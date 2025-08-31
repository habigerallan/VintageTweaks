using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VintageTweaks.Code.Features.Server
{
    internal sealed class Crate
    {
        private readonly string[] _inventoryLayers = ["hotbar", "backpack"];
        private readonly string _pushChannelName = "vintagetweaks_crate_push";
        private readonly string _pullChannelName = "vintagetweaks_crate_pull";

        private readonly ICoreServerAPI _sapi;
        private readonly Config.Crate _cfg;

        public Crate(ICoreServerAPI sapi, Config.Crate cfg)
        {
            _sapi = sapi;
            _cfg = cfg;

            _sapi.Network.RegisterChannel(_pushChannelName)
                .RegisterMessageType<Network.CrateRequest>()
                .SetMessageHandler<Network.CrateRequest>(OnCratePushRequest);

            _sapi.Network.RegisterChannel(_pullChannelName)
                .RegisterMessageType<Network.CrateRequest>()
                .SetMessageHandler<Network.CrateRequest>(OnCratePullRequest);
        }

        private void PushItemsToCrate(IServerPlayer player, CollectibleObject item, InventoryBase crateInv)
        {
            IPlayerInventoryManager invManager = player.InventoryManager;
            int totalCrateSlots = crateInv.Count;

            foreach (string layer in _inventoryLayers)
            {
                IInventory inventory = invManager.GetOwnInventory(layer);
                if (inventory == null) continue;

                int start = layer == "backpack" ? _cfg.BackpackSlots : 0;

                for (int slotId = start; slotId < inventory.Count; slotId++)
                {
                    ItemSlot pslot = inventory[slotId];
                    if (pslot.Empty) continue;

                    ItemStack stack = pslot.Itemstack;
                    if (stack.Collectible != item) continue;

                    int toMove = pslot.StackSize;
                    for (int ci = 0; ci < totalCrateSlots && toMove > 0; ci++)
                    {
                        ItemSlot cslot = crateInv[ci];
                        if (cslot.Empty || cslot.Itemstack.Collectible == stack.Collectible)
                        {
                            toMove -= pslot.TryPutInto(_sapi.World, cslot, toMove);
                            pslot.MarkDirty();
                            cslot.MarkDirty();
                        }
                    }
                }
            }
        }

        private void PullItemsFromCrate(IServerPlayer player, InventoryBase crateInv)
        {
            IPlayerInventoryManager invManager = player.InventoryManager;
            int totalCrateSlots = crateInv.Count;

            foreach (string layer in _inventoryLayers)
            {
                IInventory inventory = invManager.GetOwnInventory(layer);
                if (inventory == null) continue;

                int start = layer == "backpack" ? _cfg.BackpackSlots : 0;

                for (int ci = 0; ci < totalCrateSlots; ci++)
                {
                    ItemSlot cslot = crateInv[ci];
                    if (cslot.Empty) continue;

                    int toMove = cslot.Itemstack.StackSize;
                    for (int slotId = start; slotId < inventory.Count && toMove > 0; slotId++)
                    {
                        ItemSlot pslot = inventory[slotId];
                        if (pslot.Empty || pslot.Itemstack.Collectible == cslot.Itemstack.Collectible)
                        {
                            toMove -= cslot.TryPutInto(_sapi.World, pslot, toMove);
                            cslot.MarkDirty();
                            pslot.MarkDirty();
                        }
                    }
                }
            }
        }

        private static CollectibleObject GetItem(InventoryBase crateInv, Network.CrateRequest msg)
        {
            CollectibleObject serverSideItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            string serverItemCode = serverSideItem.Code;
            if (serverItemCode != msg.ItemCode) return null;

            return serverSideItem;
        }

        private InventoryBase GetInventory(IServerPlayer player, Network.CrateRequest msg)
        {
            BlockPos blockPos = new(msg.X, msg.Y, msg.Z);
            InventoryBase crateInv = Shared.Functions.InventoryFromBlockPos(_sapi, blockPos);
            if (crateInv == null || crateInv.ClassName != "crate") return null;

            return crateInv;
        }

        private void OnCratePushRequest(IServerPlayer player, Network.CrateRequest msg)
        {
            if (!_cfg.AllowCratePush) return;

            InventoryBase crateInv = GetInventory(player, msg);
            if (crateInv == null) return;

            CollectibleObject serverSideItem = GetItem(crateInv, msg);
            if (serverSideItem == null) return;

            PushItemsToCrate(player, serverSideItem, crateInv);
        }

        private void OnCratePullRequest(IServerPlayer player, Network.CrateRequest msg)
        {
            if (!_cfg.AllowCratePull) return;

            InventoryBase crateInv = GetInventory(player, msg);
            if (crateInv == null) return;

            if (GetItem(crateInv, msg) == null) return;

            PullItemsFromCrate(player, crateInv);
        }
    }
}
