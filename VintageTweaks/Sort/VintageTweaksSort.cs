using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VintageTweaks.Classes;
using VintageTweaks.Config;

namespace VintageTweaks.Sort
{
    internal class VintageTweaksSort : IDisposable
    {
        private readonly string _sortChannelName = "vintagetweaks_sort";

        private readonly ICoreClientAPI _capi;
        private readonly VintageTweaksConfig _cfg;

        private readonly List<ItemStack> _buffer = new();

        public VintageTweaksSort(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _capi = capi;
            _cfg = cfg;

            _capi.Event.MouseDown += OnMouseDown;

            capi.Network.RegisterChannel(_sortChannelName)
                .RegisterMessageType<MiddleClickRequest>();
        }

        public VintageTweaksSort(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            _cfg = cfg;

            sapi.Network.RegisterChannel(_sortChannelName)
                .RegisterMessageType<MiddleClickRequest>()
                .SetMessageHandler<MiddleClickRequest>(HandlePacket);
        }

        public void HandlePacket(IServerPlayer player, MiddleClickRequest msg)
        {
            IInventory inv = player.InventoryManager.GetInventory(msg.InvId);
            if (inv == null) return;

            _buffer.Clear();
            int firstSlot = msg.InvClass == "backpack" ? _cfg.BackpackSlots : 0;

            for (int i = firstSlot; i < inv.Count; i++)
            {
                ItemSlot slot = inv[i];
                if (slot.Empty) continue;

                _buffer.Add(slot.TakeOutWhole());
            }
            if (_buffer.Count == 0) return;

            _buffer.Sort((a, b) =>
            {
                int cmp = a.Collectible.Id - b.Collectible.Id;
                return cmp != 0 ? cmp : b.StackSize - a.StackSize;
            });

            int written = 0;
            for (int i = firstSlot; i < inv.Count && written < _buffer.Count; i++)
            {
                if (!inv[i].Empty) continue;

                inv[i].Itemstack = _buffer[written++];
                inv[i].MarkDirty();
            }
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Middle ||
                !_cfg.AllowMiddleClickSort)
            {
                return;
            }

            ItemSlot slot = _capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (slot == null) return;

            if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && !slot.Empty)
                return;

            IInventory inv = slot.Inventory;
            if (inv.ClassName != "chest" && inv.ClassName != "backpack") return;

            int idx = inv.GetSlotId(slot);
            if (inv.ClassName == "backpack" && idx < _cfg.BackpackSlots) return;

            int firstSlot = inv.ClassName == "backpack" ? _cfg.BackpackSlots : 0;

            for (int i = firstSlot; i < inv.Count; i++)
            {
                inv[i].MarkDirty();
            }

            _capi.Network.GetChannel("vintagetweaks")
                 .SendPacket(new MiddleClickRequest(inv.InventoryID, inv.ClassName));
        }

        public void Dispose()
        {
            _capi.Event.MouseDown -= OnMouseDown;
        }
    }
}