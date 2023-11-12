namespace RiptideRendering.Direct3D12;

internal readonly unsafe struct DeviceDependentConstants {
    public readonly uint ResourceViewDescIncrementSize;
    public readonly uint SamplerDescIncrementSize;
    public readonly uint RtvDescIncrementSize;
    public readonly uint DsvDescIncrementSize;

    public readonly D3DRootSignatureVersion HighestRootSignatureVersion;
    public readonly D3DShaderModel HighestShaderModelVersion;
    public readonly MeshShaderTier MeshShaderTier;

    public DeviceDependentConstants(ID3D12Device* pDevice) {
        ResourceViewDescIncrementSize = pDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        SamplerDescIncrementSize = pDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
        RtvDescIncrementSize = pDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        DsvDescIncrementSize = pDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.Dsv);

        // Highest root sig version
        {
            ReadOnlySpan<D3DRootSignatureVersion> versions = [
                D3DRootSignatureVersion.Version11,
                D3DRootSignatureVersion.Version10,
            ];

            foreach (var version in versions) {
                FeatureDataRootSignature data = new() {
                    HighestVersion = version,
                };

                if (pDevice->CheckFeatureSupport(D3D12Feature.RootSignature, &data, (uint)sizeof(FeatureDataRootSignature)) >= 0) {
                    HighestRootSignatureVersion = version;
                    break;
                }
            }

            Debug.Assert(HighestRootSignatureVersion != 0, "Unknown highest root signature version.");
        }

        // Highest shader model version
        {
            ReadOnlySpan<D3DShaderModel> models = [
                D3DShaderModel.ShaderModel67,
                D3DShaderModel.ShaderModel66,
                D3DShaderModel.ShaderModel65,
                D3DShaderModel.ShaderModel64,
                D3DShaderModel.ShaderModel63,
                D3DShaderModel.ShaderModel62,
                D3DShaderModel.ShaderModel61,
                D3DShaderModel.ShaderModel60,
                D3DShaderModel.ShaderModel51,
            ];

            foreach (var version in models) {
                FeatureDataShaderModel data = new() {
                    HighestShaderModel = version,
                };

                if (pDevice->CheckFeatureSupport(D3D12Feature.ShaderModel, &data, (uint)sizeof(FeatureDataShaderModel)) >= 0) {
                    HighestShaderModelVersion = version;
                    break;
                }
            }

            Debug.Assert(HighestShaderModelVersion != 0, "Unknown highest shader model version.");
        }

        // Option 7
        {
            FeatureDataD3D12Options7 data;
            
            int hr = pDevice->CheckFeatureSupport(D3D12Feature.D3D12Options7, &data, (uint)sizeof(FeatureDataD3D12Options7));
            Debug.Assert(hr >= 0);

            MeshShaderTier = data.MeshShaderTier;
        }
    }
}