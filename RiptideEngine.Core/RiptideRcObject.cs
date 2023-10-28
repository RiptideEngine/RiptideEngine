using RPCSToolkit;

namespace RiptideEngine.Core;

/// <summary>
/// Base class of every reference-counted <see cref="RiptideObject"/>.
/// </summary>
public abstract class RiptideRcObject : RiptideObject, IReferenceCount {
    protected ulong _refcount = 1;

    public ulong IncrementReference() {
        if (Interlocked.Read(ref _refcount) == 0) return 0;

        return Interlocked.Increment(ref _refcount);
    }

    public ulong DecrementReference() {
        switch (Interlocked.Read(ref _refcount)) {
            case 0: return 0;
            case 1:
                Dispose();
                Interlocked.Exchange(ref _refcount, 0);
                return 0;
            default: return Interlocked.Decrement(ref _refcount);
        }
    }

    public ulong GetReferenceCount() => Interlocked.Read(ref _refcount);

    protected abstract void Dispose();
}