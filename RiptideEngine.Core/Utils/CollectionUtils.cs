namespace RiptideEngine.Core.Utils;

public static unsafe class CollectionUtils {
#pragma warning disable CS0649
    private struct ListLayout {
        public readonly nint VirtualTable;
        public nint Items;
        public int Size;
        public int Version;
    }
#pragma warning restore CS0649

    public static void SwapElements<T>(List<T> list1, List<T> list2) {
        var pLayout1 = *(ListLayout**)Unsafe.AsPointer(ref list1);
        var pLayout2 = *(ListLayout**)Unsafe.AsPointer(ref list2);

        (pLayout1->Items, pLayout2->Items) = (pLayout2->Items, pLayout1->Items);
        (pLayout1->Size, pLayout2->Size) = (pLayout2->Size, pLayout1->Size);

        pLayout1->Version++;
        pLayout2->Version++;
    }
}