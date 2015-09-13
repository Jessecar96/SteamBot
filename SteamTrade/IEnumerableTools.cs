using System.Linq;
using System.Collections.Generic;
using System;

namespace SteamTrade
{
    public static class IEnumerableTools
    {
        public static int GetHashCode<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            int hash = enumerable.GetHashCode();
            foreach (T item in enumerable)
                hash *= item.GetHashCode();
            return hash;
        }

        public static bool Equals<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            else if (other == null)
                return false;
            foreach (T item in enumerable)
            {
                if (!other.Contains(item))
                    return false;
            }
            return true;
        }
    }
}
