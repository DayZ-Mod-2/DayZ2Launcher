using System.Collections.Generic;
using System.Linq;

static class EnumerableExtensions
{
	public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> range)
	{
		return range.Select((n, i) => (n, i));
	}
}
