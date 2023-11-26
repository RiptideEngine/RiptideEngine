namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void CopyResource(GpuResource source, GpuResource destination) {
        if (source == destination) return;
        
        EnsureNotClosed();
        FlushResourceBarriers();

        Debug.Assert(source is D3D12GpuBuffer or D3D12GpuTexture, "source is D3D12GpuBuffer or D3D12GpuTexture");
        Debug.Assert(destination is D3D12GpuBuffer or D3D12GpuTexture, "destination is D3D12GpuBuffer or D3D12GpuTexture");

        pCommandList.CopyResource((ID3D12Resource*)destination.NativeResourceHandle, (ID3D12Resource*)source.NativeResourceHandle);
    }

    public override void CopyBufferRegion(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong numBytes) {
        EnsureNotClosed();
        FlushResourceBarriers();
        
        Debug.Assert(source is D3D12GpuBuffer, "source is D3D12GpuBuffer");
        Debug.Assert(destination is D3D12GpuBuffer, "destination is D3D12GpuBuffer");

        pCommandList.CopyBufferRegion((ID3D12Resource*)destination.NativeResourceHandle, destinationOffset, (ID3D12Resource*)source.NativeResourceHandle, sourceOffset, numBytes);
    }

    public override void CopyTextureRegion(GpuTexture source, Bound3DUInt sourceBox, GpuTexture destination, uint destinationX, uint destinationY, uint destinationZ) {
        EnsureNotClosed();
        FlushResourceBarriers();
        
        Debug.Assert(source is D3D12GpuTexture, "source is D3D12GpuTexture");
        Debug.Assert(destination is D3D12GpuTexture, "destination is D3D12GpuTexture");
        
        ResourceDesc rdesc = ((ID3D12Resource*)source.NativeResourceHandle)->GetDesc();
        
        PlacedSubresourceFootprint placedFootprint;
        _context.Device->GetCopyableFootprints(&rdesc, 0, 1, 0, &placedFootprint, null, null, null);
        
        TextureCopyLocation dest = new() {
            Type = TextureCopyType.SubresourceIndex,
            PResource = (ID3D12Resource*)destination.NativeResourceHandle,
            SubresourceIndex = 0,
        };
        
        TextureCopyLocation src = new() {
            Type = TextureCopyType.PlacedFootprint,
            PResource = (ID3D12Resource*)source.NativeResourceHandle,
            PlacedFootprint = placedFootprint,
        };
        
        pCommandList.CopyTextureRegion(&dest, destinationX, destinationY, destinationZ, &src, new Box() {
            Left = sourceBox.Min.X,
            Right = sourceBox.Max.X,
            Top = sourceBox.Min.Y,
            Bottom = sourceBox.Max.Y,
            Front = sourceBox.Min.Z,
            Back = sourceBox.Max.Z,
        });
    }

    public override void UpdateResource(GpuResource resource, ReadOnlySpan<byte> data) {
        if (data.IsEmpty) return;

        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");
        
        EnsureNotClosed();
        FlushResourceBarriers();

        ResourceDesc rdesc = ((ID3D12Resource*)resource.NativeResourceHandle)->GetDesc();

        PlacedSubresourceFootprint placedSubresource;
        uint numRows;
        ulong rowSize;
        ulong totalSize;
        _context.Device->GetCopyableFootprints(&rdesc, 0, 1, 0, &placedSubresource, &numRows, &rowSize, &totalSize);

        fixed (byte* pSource = data) {
            if (rdesc.Dimension == D3D12ResourceDimension.Buffer) {
                var region = _uploadBuffer.Allocate(totalSize);

                Unsafe.CopyBlock(region.CpuAddress, pSource, uint.Min((uint)data.Length, (uint)rowSize));
                pCommandList.CopyBufferRegion((ID3D12Resource*)resource.NativeResourceHandle, 0, region.Resource, region.Offset, totalSize);
            } else {
                var region = _uploadBuffer.Allocate(totalSize, D3D12.TextureDataPlacementAlignment);
                placedSubresource.Offset = region.Offset;

                var pitch = placedSubresource.Footprint.RowPitch;

                if (numRows * rowSize <= (ulong)data.Length) {
                    for (uint r = 0; r < numRows; r++) {
                        Unsafe.CopyBlock(region.CpuAddress + pitch * r, pSource + rowSize * r, (uint)rowSize);
                    }
                } else {
                    uint copyRows = (uint)((ulong)data.Length / rowSize);

                    for (uint r = 0; r < copyRows; r++) {
                        Unsafe.CopyBlock(region.CpuAddress + pitch * r, pSource + rowSize * r, (uint)rowSize);
                    }

                    uint excessAmount = (uint)((ulong)data.Length % rowSize);
                    if (excessAmount != 0) {
                        Unsafe.CopyBlock(region.CpuAddress + pitch * copyRows, pSource + rowSize * copyRows, excessAmount);
                        Unsafe.InitBlock(region.CpuAddress + pitch * copyRows + excessAmount, 0, (uint)rowSize - excessAmount);
                    }

                    for (uint r = copyRows + 1; r < numRows; r++) {
                        Unsafe.InitBlock(region.CpuAddress + pitch * r, 0x00, (uint)rowSize);
                    }
                }

                TextureCopyLocation dest = new() {
                    Type = TextureCopyType.SubresourceIndex,
                    PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                    SubresourceIndex = 0,
                };
                TextureCopyLocation src = new() {
                    Type = TextureCopyType.PlacedFootprint,
                    PResource = region.Resource,
                    PlacedFootprint = placedSubresource,
                };

                pCommandList.CopyTextureRegion(&dest, 0, 0, 0, &src, (Box*)null);
            }
        }
    }

    public override void UpdateResource<T>(GpuResource resource, ResourceWriter<T> writer, T state) {
        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");
        
        EnsureNotClosed();
        FlushResourceBarriers();
        
        ResourceDesc rdesc = ((ID3D12Resource*)resource.NativeResourceHandle)->GetDesc();
        
        PlacedSubresourceFootprint placedSubresource;
        uint numRows;
        ulong rowSize;
        ulong totalSize;
        _context.Device->GetCopyableFootprints(&rdesc, 0, 1, 0, &placedSubresource, &numRows, &rowSize, &totalSize);
        
        if (rdesc.Dimension == D3D12ResourceDimension.Buffer) {
            var region = _uploadBuffer.Allocate(totalSize);

            writer(new(region.CpuAddress, (int)rowSize), 0, state);
        
            pCommandList.CopyBufferRegion((ID3D12Resource*)resource.NativeResourceHandle, 0, region.Resource, region.Offset, totalSize);
        } else {
            var region = _uploadBuffer.Allocate(totalSize, D3D12.TextureDataPlacementAlignment);
            placedSubresource.Offset = region.Offset;
        
            var pitch = placedSubresource.Footprint.RowPitch;

            for (uint r = 0; r < numRows; r++) {
                writer(new(region.CpuAddress + pitch * r, (int)rowSize), r, state);
            }
        
            TextureCopyLocation dest = new() {
                Type = TextureCopyType.SubresourceIndex,
                PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                SubresourceIndex = 0,
            };
            TextureCopyLocation src = new() {
                Type = TextureCopyType.PlacedFootprint,
                PResource = region.Resource,
                PlacedFootprint = placedSubresource,
            };
        
            pCommandList.CopyTextureRegion(&dest, 0, 0, 0, &src, (Box*)null);
        }
    }

    public override void UpdateBufferRegion(GpuBuffer buffer, uint offset, ReadOnlySpan<byte> source) {
        Debug.Assert(buffer is D3D12GpuBuffer, "buffer is D3D12GpuBuffer");
        
        EnsureNotClosed();
        FlushResourceBarriers();
        
        ResourceDesc rdesc = ((ID3D12Resource*)buffer.NativeResourceHandle)->GetDesc();
    
        PlacedSubresourceFootprint placedSubresource;
        uint numRows;
        ulong rowSize;
        ulong totalSize;
    
        _context.Device->GetCopyableFootprints(&rdesc, 0, 1, 0, &placedSubresource, &numRows, &rowSize, &totalSize);

        var region = _uploadBuffer.Allocate(totalSize);

        uint size = uint.Min((uint)source.Length, (uint)rowSize);

        fixed (byte* pSource = source) {
            Unsafe.CopyBlock(region.CpuAddress, pSource, size);
        }

        pCommandList.CopyBufferRegion((ID3D12Resource*)buffer.NativeResourceHandle, offset, region.Resource, region.Offset, size);
    }
}