namespace RiptideEditorV2.UI;

public sealed class RootElement : InterfaceElement {
    internal RootElement(InterfaceDocument document) : base() {
        Parent = null;
        Document = document;
    }
}