using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SteamTrade.InventorySys
{
    internal struct ItemEnumerator :
        IEnumerator<Item>,
        IEnumerator<ulong>
    {
        private readonly IEnumerable<Item> items;

        private int currentIndex;

        Item IEnumerator<Item>.Current => items.ElementAt(currentIndex);

        ulong IEnumerator<ulong>.Current => (ulong)items.ElementAt(currentIndex);

        object IEnumerator.Current => items.ElementAt(currentIndex);

        internal ItemEnumerator(IEnumerable<Item> enumeratableItems)
        {
            items = enumeratableItems;
            currentIndex = 0;
        }

        public bool MoveNext()
        {
            if (currentIndex == items.Count() + 1)
                return false;
            currentIndex++;
            return true;
        }

        public void Reset() => currentIndex = 0;

        public void Dispose() { throw new NotImplementedException(); }
    }
}
