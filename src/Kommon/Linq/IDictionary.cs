using System.Collections;
using System.Collections.Generic;

namespace Kommon.Linq
{
    public static partial class Extenions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary dict)
        {
            var toReturn = new Dictionary<TKey, TValue>();

            foreach (var key in dict.Keys)
            {
                var k = (TKey)key;
                if (!toReturn.ContainsKey(k))
                    toReturn.Add(k, (TValue)dict[key]);
            }

            return toReturn;
        }
    }
}
