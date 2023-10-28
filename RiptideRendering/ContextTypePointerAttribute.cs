namespace RiptideRendering;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
internal class ContextTypePointerAttribute<T> : Attribute where T : BaseRenderingContext { }