namespace RiptideEditor.Application;

internal abstract class ApplicationState {
    public abstract void Begin();
    public abstract void Update();
    public abstract void RenderGUI();
    public abstract void End();

    public virtual void Resize(int width, int height) { }
    public virtual void Shutdown() { }
}