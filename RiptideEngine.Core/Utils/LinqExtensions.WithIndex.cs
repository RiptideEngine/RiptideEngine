using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RiptideEngine.Core.Utils;

partial class LinqExtensions {
    public static IEnumerable<(T Element, int Index)> WithIndex<T>(this IEnumerable<T> enumerable) {
        int index = 0;
        foreach (var element in enumerable) {
            yield return (element, index++);
        }
    }

    public static IEnumerable<(T Element, int Index)> WithIndex<T>(this ImmutableArray<T> array) {
        return array.IsDefault ? Enumerable.Empty<(T, int)>() : ImmutableCollectionsMarshal.AsArray(array)!.WithIndex();
    }
}