using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.Code.Features.Client
{
    internal class Sort : IDisposable
    {
        private const string _sortChannelName = "vintagetweaks_sort";

        private readonly ICoreClientAPI _capi;
        private readonly Config.Sort _cfg;

        public Sort(ICoreClientAPI capi, Config.Sort cfg)
        {
            _capi = capi;
            _cfg = cfg;

            _capi.Event.MouseDown += SortDown;

            _capi.Network.RegisterChannel(_sortChannelName)
                .RegisterMessageType<Network.MiddleClickRequest>();
        }

        private void SortDown(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Middle || !_cfg.AllowMiddleClickSort) return;

            ItemSlot slot = _capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (slot == null) return;

            if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && !slot.Empty) return;

            InventoryBase inv = slot.Inventory;

            int idx = inv.GetSlotId(slot);
            if (inv.ClassName == "backpack" && idx < _cfg.BackpackSlots) return;

            int firstSlot = inv.ClassName == "backpack" ? _cfg.BackpackSlots : 0;

            for (int i = firstSlot; i < inv.Count; i++)
            {
                inv[i].MarkDirty();
            }

            _capi.Network.GetChannel(_sortChannelName)
                .SendPacket(new Network.MiddleClickRequest(inv.InventoryID, inv.ClassName));
        }

        public void Dispose()
        {
            _capi.Event.MouseDown -= SortDown;
        }
    }
}
