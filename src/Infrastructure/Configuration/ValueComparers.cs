using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Configuration;

internal static class ValueComparers
{
    public static ValueComparer<List<T>> GetListComparer<T>() where T : IEquatable<T>
    {
        return new ValueComparer<List<T>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
    }

    public static ValueComparer<List<T>> GetEnumListComparer<T>() where T : Enum
    {
        return new ValueComparer<List<T>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
    }

    public static ValueComparer<List<DateOnly>> GetDateOnlyListComparer()
    {
        return new ValueComparer<List<DateOnly>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
    }
}
