using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageTweaks.Code.Features.Server
{
    internal class Sort
    {
        private const string _sortChannelName = "vintagetweaks_sort";

        private readonly Config.Sort _cfg;
        private readonly List<ItemStack> _itemsToBeSorted;

        public Sort(ICoreServerAPI sapi, Config.Sort cfg)
        {
            _cfg = cfg;

            _itemsToBeSorted = [];

            sapi.Network.RegisterChannel(_sortChannelName)
                .RegisterMessageType<Network.MiddleClickRequest>()
                .SetMessageHandler<Network.MiddleClickRequest>(OnMiddleClickRequest);
        }

        private void OnMiddleClickRequest(IServerPlayer player, Network.MiddleClickRequest msg)
        {
            IInventory inv = player.InventoryManager.GetInventory(msg.InvId);
            if (inv == null) return;

            _itemsToBeSorted.Clear();
            int firstSlot = msg.InvClass == "backpack" ? _cfg.BackpackSlots : 0;

            for (int i = firstSlot; i < inv.Count; i++)
            {
                ItemSlot slot = inv[i];
                if (slot.Empty) continue;

                _itemsToBeSorted.Add(slot.TakeOutWhole());
            }
            if (_itemsToBeSorted.Count == 0) return;

            _itemsToBeSorted.Sort((a, b) =>
            {
                int cmp = a.Collectible.Id - b.Collectible.Id;
                return cmp != 0 ? cmp : b.StackSize - a.StackSize;
            });

            int written = 0;
            for (int i = firstSlot; i < inv.Count && written < _itemsToBeSorted.Count; i++)
            {
                if (!inv[i].Empty) continue;

                inv[i].Itemstack = _itemsToBeSorted[written++];
                inv[i].MarkDirty();
            }
        }
    }
}
