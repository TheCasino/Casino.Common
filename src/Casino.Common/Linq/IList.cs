using System;
using System.Collections.Generic;
using System.Linq;

namespace Casino.Linq
{
    public static partial class Extenions
    {
        public static T[] ToArray<T>(this IList<T> list, Func<T, bool> predicate)
            => list.Where(predicate).ToArray();

        public static List<T> ToList<T>(this IList<T> list, Func<T, bool> predicate)
            => list.Where(predicate).ToList();
    }
}
