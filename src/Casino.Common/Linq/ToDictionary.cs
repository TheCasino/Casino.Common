using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Casino.Common.Linq
{
    public static partial class Extenions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary dict)
        {
            var toReturn = new Dictionary<TKey, TValue>();
            var keys = toReturn.Keys.Cast<TKey>();

            foreach (var key in keys)
            {
                if(!toReturn.ContainsKey(key))
                    toReturn.Add(key, (TValue)dict[key]);
            }

            return toReturn;
        }
    }
}
