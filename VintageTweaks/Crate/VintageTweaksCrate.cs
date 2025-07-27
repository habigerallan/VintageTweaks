using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;
using VintageTweaks.Classes;
using VintageTweaks.Config;

namespace VintageTweaks.Crate
{
    internal class VintageTweaksCrate : IDisposable
    {
        private readonly string _channelName = "vintagetweakscrate";

        private readonly ICoreClientAPI _capi;
        private readonly ICoreServerAPI _sapi;
        private readonly VintageTweaksConfig _cfg;

        private CrateClickObject _pushObject;

        public static void ResetClick(CrateClickObject clickObject)
        {
            clickObject.ClickTime = 0;
            clickObject.ClickPos = null;
            clickObject.ClickItem = null;
            clickObject.ClickComplete = false;
        }

        public bool IsPushSuccessful(CrateClickObject pushObject, BlockPos clickPos, long clickTime, CollectibleObject clickItem)
        {
            return pushObject.ClickPos == clickPos
                && pushObject.ClickItem == clickItem
                && pushObject.ClickTime - clickTime <= _cfg.CratePushDelayMs
                && pushObject.ClickComplete;
        }

        public bool IsKeyDown(GlKeys glkey)
        {
            bool[] keyboard = _capi.Input.KeyboardKeyState;
            return keyboard[(int)glkey]; ;
        }

        public BlockPos GetHoveredBlockPos()
        {
            BlockSelection hovered = _capi.World.Player.CurrentBlockSelection;
            if (hovered != null)
            {
                return hovered.Position;
            }

            return null;   
        }

        public InventoryBase InventoryFromBlockPos(BlockPos blockPos)
        {
            var block = _capi != null
                ? _capi.World.BlockAccessor.GetBlockEntity(blockPos)
                : _sapi.World.BlockAccessor.GetBlockEntity(blockPos);

            if (block != null)
            {
                var inventory = block.GetType().GetProperty("Inventory");

                if (block != null)
                {
                    return (InventoryBase)inventory.GetValue(block);
                }
            }

            return null;
        }

        public bool PlayerContainsCrateItem(CollectibleObject collectibleObject)
        {
            string[] inventoryLayers = { "hotbar", "backpack" };
            var invManager = _capi.World.Player.InventoryManager;

            foreach (var layer in inventoryLayers)
            {
                var inventory = invManager.GetOwnInventory(layer);
                if (inventory == null) continue;

                foreach (var slot in inventory)
                {
                    if (slot.Empty) continue;
                    if (slot.Itemstack.Collectible == collectibleObject) return true;
                }
            }

            return false;
        }

        public void PushItemsToCrate(IServerPlayer player, CollectibleObject item, InventoryBase crateInv)
        {
            string[] inventoryLayers = { "hotbar", "backpack" };
            var invManager = player.InventoryManager;
            int totalCrateSlots = crateInv.Count;

            foreach (var layer in inventoryLayers)
            {
                var inventory = invManager.GetOwnInventory(layer);
                if (inventory == null) continue;

                int start = 0;
                if (layer == "backpack")
                {
                    start = _cfg.BackpackSlots;
                }

                for (int slotId = start; slotId < inventory.Count; slotId++)
                {
                    var slot = inventory[slotId];
                    if (slot.Empty) continue;

                    var stack = slot.Itemstack;
                    if (stack.Collectible != item) continue;

                    int toMove = slot.StackSize;
                    for (int ci = 0; ci < totalCrateSlots && toMove > 0; ci++)
                    {
                        var cslot = crateInv[ci];
                        if (cslot.Empty || cslot.Itemstack.Collectible == stack.Collectible)
                        {
                            toMove -= slot.TryPutInto(_sapi.World, cslot, toMove);
                            slot.MarkDirty();
                            cslot.MarkDirty();
                        }
                    }
                }
            }
        }

        public VintageTweaksCrate(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _capi = capi;
            _cfg = cfg;
            _capi.Event.MouseDown += CratePushDown;
            _capi.Event.MouseUp += CratePushUp;
            _capi.Network.RegisterChannel(_channelName)
                .RegisterMessageType<CrateRequest>();

            _pushObject = new CrateClickObject();
        }

        public VintageTweaksCrate(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            _sapi = sapi;
            _cfg = cfg;
            _sapi.Network.RegisterChannel(_channelName)
                .RegisterMessageType<CrateRequest>()
                .SetMessageHandler<CrateRequest>(OnCratePushRequest);
        }

        private void CratePushDown(MouseEvent e)
        {
            if (!_cfg.AllowCratePush) return;
            if (e.Button != EnumMouseButton.Right) return;

            if (!IsKeyDown(GlKeys.ShiftLeft)) 
            {
                ResetClick(_pushObject);
                return;
            }

            BlockPos currentPos = GetHoveredBlockPos();
            if (currentPos == null)
            {
                ResetClick(_pushObject);
                return;
            }

            InventoryBase crateInv = InventoryFromBlockPos(currentPos);
            if (crateInv == null 
                || crateInv.ClassName != "crate"
                || crateInv.Count == 0 
                || crateInv.Empty)
            {
                ResetClick(_pushObject);
                return;
            }

            CollectibleObject currentItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            if (!PlayerContainsCrateItem(currentItem))
            {
                ResetClick(_pushObject);
                return;
            }

            // fuck you bitch ass code, delay between clicks not working
            long currentClickTime = _capi.World.ElapsedMilliseconds;
            if (IsPushSuccessful(_pushObject, currentPos, currentClickTime, currentItem))
            {
                _capi.Network.GetChannel(_channelName)
                    .SendPacket(new CrateRequest { X = currentPos.X, Y = currentPos.Y, Z = currentPos.Z, ItemCode = currentItem.Code });
                
                ResetClick(_pushObject);
            }
            else
            {
                _pushObject.ClickTime = currentClickTime;
                _pushObject.ClickItem = currentItem;
                _pushObject.ClickPos = currentPos;
            }
        }

        private void CratePushUp(MouseEvent e)
        {
            if (!_cfg.AllowCratePush) return;
            if (e.Button != EnumMouseButton.Right) return;

            if (!IsKeyDown(GlKeys.ShiftLeft))
            {
                ResetClick(_pushObject);
                return;
            }

            _pushObject.ClickComplete = true;
        }

        private void OnCratePushRequest(IServerPlayer player, CrateRequest msg)
        {
            if (!_cfg.AllowCratePush) return;

            var blockPos = new BlockPos(msg.X, msg.Y, msg.Z);
            InventoryBase crateInv = InventoryFromBlockPos(blockPos);

            if (crateInv == null || crateInv.ClassName != "crate") return;
 
            CollectibleObject serverSideItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            string serverItemCode = serverSideItem.Code;
            if (serverItemCode != msg.ItemCode) return;

            PushItemsToCrate(player, serverSideItem, crateInv);
        }

        public void Dispose()
        {
            _capi.Event.MouseDown -= CratePushDown;
            _capi.Event.MouseUp -= CratePushUp;
        }
    }
}
