using System.Collections;

namespace RiptideEngine.Core.Utils;

public static class LinqExtensions {
    public static bool AllComparableEquals<T>(this IEnumerable<T> enumerable, T? value) where T : IComparable<T> {
        foreach (var item in enumerable) {
            if (item.CompareTo(value) != 0) return false;
        }

        return true;
    }

    public static bool AllEquals<T>(this IEnumerable<T> enumerable, T? value) where T : IEquatable<T> {
        foreach (var item in enumerable) {
            if (!item.Equals(value)) return false;
        }

        return true;
    }

    public static bool AllStructuredEquals<T>(this IEnumerable<T> enumerable, T? value, IEqualityComparer comparer) where T : IStructuralEquatable {
        foreach (var item in enumerable) {
            if (!item.Equals(value, comparer)) return false;
        }

        return true;
    }

    /// <summary>
    /// Filters a sequence so that enumerable only contains non-null values.
    /// </summary>
    /// <typeparam name="T">Nullable class type.</typeparam>
    /// <param name="enumerable">Sequence to filter null values.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that only contains non-null values.</returns>
    public static IEnumerable<T> FilterNull<T>(this IEnumerable<T?> enumerable) where T : class {
        return (IEnumerable<T>)enumerable.Where(IsNotNull);

        static bool IsNotNull(T? value) => value != null;
    }
}