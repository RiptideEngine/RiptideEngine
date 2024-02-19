namespace RiptideEditorV2.UI;

[Flags]
public enum InvalidationFlags {
    None = 0,
    
    Layout = 1 << 0,
    Graphics = 1 << 1,
    
    All = Layout | Graphics,
}