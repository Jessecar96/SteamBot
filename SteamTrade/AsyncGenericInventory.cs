using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;
using System;

namespace SteamTrade
{
    public class AsyncGenericInventory : GenericInventory
    {
        public event EventHandler OnLoadCompleted;
        Task<GenericInventory> InventoryLoader;

        void Loaded()
        {
            Task LoadCompleted = InventoryLoader.ContinueWith((inventory) =>
            {
                base.Items = inventory.Result.Items;
                base.Descriptions = inventory.Result.Descriptions;
                base.Errors = inventory.Result.Errors;
                base.IsLoaded = true;

                EventHandler tmp = OnLoadCompleted;

                if (OnLoadCompleted != null)
                    OnLoadCompleted(null, EventArgs.Empty);
            });
        }

        public new Dictionary<string, GenericInventory.Item> Items
        {
            get
            {
                if (!InventoryLoader.IsCompleted)
                    InventoryLoader.Wait();

                return base.Items;
            }
        }

        public new Dictionary<string, GenericInventory.ItemDescription> Descriptions
        {
            get
            {
                if (!InventoryLoader.IsCompleted)
                    InventoryLoader.Wait();

                return base.Descriptions;
            }
        }

        public new void Load(SteamID steamId, int appId, IEnumerable<int> contextId)
        {
            InventoryLoader = Task<GenericInventory>.Factory.StartNew(() =>
            {
                return GenericInventory.Load(steamId, appId, contextId);
            });

            Loaded();
        }

        public new void Load(SteamID steamId, int appId, int contextId = 2)
        {
            InventoryLoader = Task<GenericInventory>.Factory.StartNew(() =>
            {
                return GenericInventory.Load(steamId, appId, contextId);
            });

            Loaded();
        }

        public void WaitToLoad()
        {
            if (!InventoryLoader.IsCompleted)
                InventoryLoader.Wait();
        }
    }
}
