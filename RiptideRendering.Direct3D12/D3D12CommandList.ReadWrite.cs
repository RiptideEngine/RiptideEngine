﻿namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void UpdateBuffer(NativeBufferHandle resource, uint offset, ReadOnlySpan<byte> data) {
        if (data.IsEmpty) return;
        EnsureNotClosed();

        var d3d12resource = (ID3D12Resource*)resource.Handle;
        var rdesc = d3d12resource->GetDesc();

        if (offset >= rdesc.Width) return;

        uint allocateAmount = uint.Min((uint)data.Length, (uint)(rdesc.Width - offset));

        var allocation = _uploadBuffer.Allocate(allocateAmount);

        Debug.Assert(allocation.CpuAddress != null);

        fixed (byte* pData = data) {
            Unsafe.CopyBlock(allocation.CpuAddress, pData, allocateAmount);
        }

        pCommandList.CopyBufferRegion(d3d12resource, offset, allocation.Resource, allocation.Offset, allocateAmount);
    }

    public override void UpdateBuffer<T>(NativeBufferHandle resource, uint offset, uint length, BufferWriter<T> writer, T state) {
        EnsureNotClosed();

        ArgumentNullException.ThrowIfNull(writer, nameof(writer));

        var d3d12resource = (ID3D12Resource*)resource.Handle;
        var rdesc = d3d12resource->GetDesc();

        if (offset >= rdesc.Width) return;

        uint allocateAmount = uint.Min(length, (uint)(rdesc.Width - offset));

        var allocation = _uploadBuffer.Allocate(allocateAmount);

        Debug.Assert(allocation.CpuAddress != null);

        writer(new(allocation.CpuAddress, (int)allocateAmount), state);

        pCommandList.CopyBufferRegion(d3d12resource, offset, allocation.Resource, allocation.Offset, allocateAmount);
    }

    public override void UpdateTexture(NativeTextureHandle resource, ReadOnlySpan<byte> data) {
        if (data.IsEmpty) return;
        EnsureNotClosed();

        var d3d12resource = (ID3D12Resource*)resource.Handle;
        var rdesc = d3d12resource->GetDesc();

        PlacedSubresourceFootprint footprint;
        uint numRows;
        ulong rowSizeInByte;
        ulong uploadSize;
        _context.Device->GetCopyableFootprints(&rdesc, 0, 1, 0, &footprint, &numRows, &rowSizeInByte, &uploadSize);

        var uploadRegion = _uploadBuffer.Allocate(uploadSize, D3D12.TextureDataPlacementAlignment);
        footprint.Offset = uploadRegion.Offset;

        fixed (byte* pSource = data) {
            var depth = footprint.Footprint.Depth;
            ulong slicePitch = rowSizeInByte * numRows;

            var cvt = D3D12Convert.TryConvert(rdesc.Format, out var gfxFormat);
            Debug.Assert(cvt, $"Failed to convert {SilkHelper.GetNativeName(rdesc.Format, "Name")} into its correspond {nameof(GraphicsFormat)}.");

            var gt = gfxFormat.TryGetStride(out uint stride);
            Debug.Assert(gt, $"Failed to get stride of {gfxFormat}.");

            var dest = new MemcpyDest() {
                PData = uploadRegion.CpuAddress,
                RowPitch = footprint.Footprint.RowPitch,
                SlicePitch = footprint.Footprint.RowPitch * numRows,
            };

            if ((uint)data.Length >= (uint)rowSizeInByte * numRows * depth) {
                SubresourceData src = new() {
                    PData = pSource,
                    RowPitch = (nint)(rdesc.Width * stride),
                    SlicePitch = (nint)(rdesc.Width * stride * rdesc.Height),
                };

                D3D12Helper.MemcpySubresource(&dest, &src, rowSizeInByte, numRows, depth);
            } else {
                (uint numSliceCopy, uint sliceRemain) = uint.DivRem((uint)data.Length, (uint)slicePitch);

                SubresourceData src = new() {
                    PData = pSource,
                    RowPitch = (nint)(rdesc.Width * stride),
                    SlicePitch = (nint)(rdesc.Width * stride * rdesc.Height),
                };
                D3D12Helper.MemcpySubresource(&dest, &src, rowSizeInByte, numRows, numSliceCopy);

                if (sliceRemain > 0) {
                    (uint numRowCpy, uint remain) = uint.DivRem(sliceRemain, (uint)rowSizeInByte);

                    var pDestSlice = (byte*)dest.PData + dest.SlicePitch * numSliceCopy;
                    var pSrcSlice = (byte*)src.PData + src.SlicePitch * numSliceCopy;

                    for (uint y = 0; y < numRowCpy; y++) {
                        Unsafe.CopyBlock(pDestSlice + dest.RowPitch * y, pSrcSlice + src.RowPitch * y, (uint)rowSizeInByte);
                    }

                    Unsafe.CopyBlock(pDestSlice + dest.RowPitch * numRowCpy, pSrcSlice + src.RowPitch * numRowCpy, remain);
                }
            }
        }

        pCommandList.CopyTextureRegion(new TextureCopyLocation() {
            PResource = d3d12resource,
            Type = TextureCopyType.SubresourceIndex,
            SubresourceIndex = 0,
        }, 0, 0, 0, new TextureCopyLocation() {
            PResource = uploadRegion.Resource,
            Type = TextureCopyType.PlacedFootprint,
            PlacedFootprint = footprint,
        }, null);
    }

    public override void CopyBuffer(NativeBufferHandle source, NativeBufferHandle destination) {
        EnsureNotClosed();
        pCommandList.CopyResource((ID3D12Resource*)destination.Handle, (ID3D12Resource*)source.Handle);
    }

    public override void CopyTexture(NativeTextureHandle source, NativeTextureHandle destination) {
        EnsureNotClosed();
        pCommandList.CopyResource((ID3D12Resource*)destination.Handle, (ID3D12Resource*)source.Handle);
    }

    public override void CopyBufferRegion(NativeBufferHandle source, ulong sourceOffset, NativeBufferHandle destination, ulong destinationOffset, ulong copyLength) {
        EnsureNotClosed();
        pCommandList.CopyBufferRegion((ID3D12Resource*)destination.Handle, destinationOffset, (ID3D12Resource*)source.Handle, sourceOffset, copyLength);
    }

    public override void CopyTextureRegion(NativeTextureHandle source, Bound3D<uint> sourceBox, NativeTextureHandle destination, uint destinationX, uint destinationY, uint destinationZ) {
        EnsureNotClosed();

        var sourceResource = (ID3D12Resource*)source.Handle;
        var destResource = (ID3D12Resource*)destination.Handle;

        var srdesc = sourceResource->GetDesc();

        if (!ArgumentValidation.ValidateTextureBoundaryBox(srdesc, sourceBox)) {
            throw new ArgumentException("Invalid texture source box provided which can result in undefined behaviour due to out of bound copy.");
        }

        var drdesc = destResource->GetDesc();

        if (!ArgumentValidation.ValidateTextureBoundaryBox(drdesc, new Bound3D<uint>(destinationX, destinationY, destinationZ, destinationX + sourceBox.MaxX - sourceBox.MinX, destinationY + sourceBox.MaxY - sourceBox.MinY, destinationZ + sourceBox.MaxZ - sourceBox.MinZ))) {
            throw new ArgumentException("Invalid destination coordinate provided which can result in undefined behaviour due to out of bound copy.");
        }

        TextureCopyLocation src = new() {
            Type = TextureCopyType.SubresourceIndex,
            PResource = sourceResource,
            SubresourceIndex = 0,
        };
        TextureCopyLocation dest = new() {
            Type = TextureCopyType.SubresourceIndex,
            PResource = destResource,
            SubresourceIndex = 0,
        };

        pCommandList.CopyTextureRegion(&dest, destinationX, destinationY, destinationZ, &src, (Box*)&sourceBox);
    }

    public override void ReadTexture(NativeTextureHandle source, Bound3D<uint> sourceBox, NativeReadbackBufferHandle destination) {
        EnsureNotClosed();

        ID3D12Resource* sourceResource = (ID3D12Resource*)source.Handle;
        ID3D12Resource* destResource = (ID3D12Resource*)destination.Handle;

        ResourceDesc srdesc = sourceResource->GetDesc();
        if (!ArgumentValidation.ValidateTextureBoundaryBox(srdesc, sourceBox)) {
            throw new ArgumentException("Invalid texture source box provided which can result in undefined behaviour due to out of bound copy.");
        }

        ResourceDesc drdesc = destResource->GetDesc();

        var device = _context.Device;

        PlacedSubresourceFootprint sourceFootprint;
        ulong rowPitch;
        device->GetCopyableFootprints(&srdesc, 0, 1, 0, &sourceFootprint, null, &rowPitch, null);

        TextureCopyLocation src = new() {
            Type = TextureCopyType.SubresourceIndex,
            PResource = sourceResource,
            SubresourceIndex = 0,
        };

        TextureCopyLocation dest = new() {
            Type = TextureCopyType.PlacedFootprint,
            PResource = destResource,
            PlacedFootprint = new() {
                Footprint = new() {
                    Width = 1,
                    Height = 1,
                    Depth = 1,
                    // RowPitch = 256,
                    RowPitch = sourceFootprint.Footprint.RowPitch,
                    Format = sourceFootprint.Footprint.Format,
                },
                Offset = 0,
            },
        };

        pCommandList.CopyTextureRegion(&dest, 0, 0, 0, &src, (Box*)&sourceBox);

        //var srcdesc = ((ID3D12Resource*)source.Handle)->GetDesc();
        //var device = _context.Device;

        //ulong srcRowPitch;
        //device->GetCopyableFootprints(&srcdesc, 0, 1, 0, null, null, &srcRowPitch, null);


        //var sourceBoxSize = sourceBox.Size;
        //SubresourceFootprint readbackFootprint = new() {
        //    Width = sourceBoxSize.X,
        //    Height = sourceBoxSize.Y,
        //    Depth = sourceBoxSize.Z,
        //    Format = srcdesc.Format,
        //    RowPitch = (uint)srcRowPitch,
        //};

        //TextureCopyLocation dest = new() {
        //    Type = TextureCopyType.PlacedFootprint,
        //    PResource = d3d12readback,
        //    PlacedFootprint = new() {
        //        Offset = 0,
        //        Footprint = readbackFootprint,
        //    },
        //};

        //pCommandList.CopyTextureRegion(&dest, 0, 0, 0, &src, *(Box*)&sourceBox);
    }
}