namespace RiptideEngine.Core;

public interface IReferenceCount {
    ulong IncrementReference();
    ulong DecrementReference();
    ulong GetReferenceCount();
}