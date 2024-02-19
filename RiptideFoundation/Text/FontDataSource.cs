namespace RiptideFoundation.Text;

internal readonly struct FontDataSource {
    public readonly SourceType Type;

    private readonly SourceData _srcData;

    public string FilePath {
        get {
            Debug.Assert(Type == SourceType.File);
            return _srcData.FilePath;
        }
    }

    public byte[] Memory {
        get {
            Debug.Assert(Type == SourceType.Memory);
            return _srcData.Memory;
        }
    }

    public FontDataSource(string filePath) {
        Type = SourceType.File;
        _srcData = new(filePath);
    }
    
    public FontDataSource(byte[] memory) {
        Type = SourceType.File;
        _srcData = new(memory);
    }
    
    public enum SourceType {
        Unknown = 0,
        
        File,
        Memory,
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SourceData {
        [FieldOffset(0)] public readonly string FilePath = null!;
        [FieldOffset(0)] public readonly byte[] Memory = null!;

        public SourceData(string filePath) {
            FilePath = filePath;
        }

        public SourceData(byte[] memory) {
            Memory = memory;
        }
    }
}