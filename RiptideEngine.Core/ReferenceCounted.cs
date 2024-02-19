namespace RiptideEngine.Core;

/// <summary>
/// Base class of every reference-counted objects.
/// </summary>
public abstract class ReferenceCounted : IReferenceCount {
    protected ulong _refcount = 1;

    // TODO: Atomic increment and decrement with Interlocked, somehow...
    
    public ulong IncrementReference() {
        return Interlocked.Read(ref _refcount) == 0 ? 0 : ++_refcount;
    }

    public ulong DecrementReference() {
        switch (Interlocked.Read(ref _refcount)) {
            case 0: return 0;
            case 1:
                Dispose();
                _refcount = 0;
                return 0;
            default: return --_refcount;
        }
    }

    public ulong GetReferenceCount() => Interlocked.Read(ref _refcount);

    protected abstract void Dispose();
}