using System;
using System.Collections.Generic;
using System.Linq;

namespace Kommon.Linq
{
    public static partial class Extenions
    {
        public static T[] ToArray<T>(this IEnumerable<T> list, Func<T, bool> predicate)
            => list.Where(predicate).ToArray();

        public static List<T> ToList<T>(this IEnumerable<T> list, Func<T, bool> predicate)
            => list.Where(predicate).ToList();
    }
}
