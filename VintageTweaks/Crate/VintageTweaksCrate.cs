using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using VintageTweaks.Config;
using VintageTweaks.Classes;

namespace VintageTweaks.Crate
{
    internal class VintageTweaksCrate : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private readonly ICoreServerAPI _sapi;
        private readonly VintageTweaksConfig _cfg;

        private BlockPos _lastPos;
        private long _lastClickTime;
        private CollectibleObject _lastItem;

        private BlockPos _currentPos;
        private long _currentClickTime;
        private CollectibleObject _currentItem;

        private bool _clicked;

        public VintageTweaksCrate(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _capi = capi;
            _cfg = cfg;
            _capi.Event.MouseDown += OnMouseDown;
            _capi.Event.MouseUp += OnMouseUp;
            _capi.Network.RegisterChannel("vintagetweakscrate")
                .RegisterMessageType<CratePushRequest>();

            _lastPos = null;
            _lastClickTime = 0;
            _lastItem = null;

            _currentPos = null;
            _currentClickTime = 0;
            _currentItem = null;

            _clicked = false;
        }

        public VintageTweaksCrate(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            _sapi = sapi;
            _cfg = cfg;
            _sapi.Network.RegisterChannel("vintagetweakscrate")
                .RegisterMessageType<CratePushRequest>()
                .SetMessageHandler<CratePushRequest>(OnCratePushRequest);
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (!_cfg.AllowCratePush) return;
            if (e.Button != EnumMouseButton.Right) return;

            var kb = _capi.Input.KeyboardKeyState;
            bool shift = kb[(int)GlKeys.ShiftLeft] || kb[(int)GlKeys.ShiftRight];
            if (!shift) 
            {
                _lastPos = null;
                _lastItem = null;
                _lastClickTime = 0;
                _clicked = false;
                return;
            }

            var sel = _capi.World.Player.CurrentBlockSelection;
            if (sel == null) return;

            _currentPos = sel.Position;
            var be = _capi.World.BlockAccessor.GetBlockEntity(_currentPos);
            if (be == null) return;

            var invProp = be.GetType().GetProperty("Inventory");
            if (invProp?.GetValue(be) is not InventoryBase crateInv) return;
            if (crateInv.Count == 0 || crateInv.Empty) return;

            bool hasMatch = false;
            _currentItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            foreach (var layer in new[] { "hotbar", "backpack" })
            {
                var inv = _capi.World.Player.InventoryManager.GetOwnInventory(layer);
                if (inv == null) continue;
                foreach (var slot in inv)
                {
                    if (!slot.Empty && slot.Itemstack.Collectible == _currentItem)
                    {
                        hasMatch = true;
                        break;
                    }
                }
                if (hasMatch) break;
            }
            if (!hasMatch) return;

            _currentClickTime = _capi.World.ElapsedMilliseconds;
            if (_lastPos != null 
                && _lastPos.Equals(_currentPos) 
                && _lastItem == _currentItem
                && _currentClickTime - _lastClickTime < _cfg.CratePushDelayMs
                && _clicked)
            {
                _capi.Network.GetChannel("vintagetweakscrate")
                    .SendPacket(new CratePushRequest { X = _currentPos.X, Y = _currentPos.Y, Z = _currentPos.Z, ItemCode = _currentItem.Code });

                _lastPos = null;
                _lastItem = null;
                _lastClickTime = 0;
                _clicked = false;
            }
            else
            {
                _lastPos = _currentPos;
                _lastItem = _currentItem;
                _lastClickTime = _currentClickTime;
                _clicked = false;
            }
        }

        private void OnMouseUp(MouseEvent e)
        {
            if (!_cfg.AllowCratePush) return;
            if (e.Button != EnumMouseButton.Right) return;

            var kb = _capi.Input.KeyboardKeyState;
            bool shift = kb[(int)GlKeys.ShiftLeft] || kb[(int)GlKeys.ShiftRight];
            if (!shift) return;

            _clicked = true;
        }


        private void OnCratePushRequest(IServerPlayer player, CratePushRequest msg)
        {
            if (!_cfg.AllowCratePush) return;

            var pos = new BlockPos(msg.X, msg.Y, msg.Z);
            var be = _sapi.World.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return;

            var invProp = be.GetType().GetProperty("Inventory");
            if (invProp?.GetValue(be) is not InventoryBase crateInv) return;

            CollectibleObject serverSideItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            string serverItemCode = serverSideItem.Code;
            if (serverItemCode != msg.ItemCode) return;

            IInventory hotbar = player.InventoryManager.GetOwnInventory("hotbar");
            IInventory backpack = player.InventoryManager.GetOwnInventory("backpack");

            int totalCrateSlots = crateInv.Count;
            CollectibleObject crateType = crateInv.Empty
                ? null
                : crateInv.FirstNonEmptySlot.Itemstack.Collectible;

            foreach (var playerInv in new[] { hotbar, backpack })
            {
                if (playerInv == null) continue;

                int firstSlot = playerInv.ClassName == "backpack"
                    ? _cfg.BackpackSlots
                    : 0;

                for (int slotId = firstSlot; slotId < playerInv.Count; slotId++)
                {
                    var slot = playerInv[slotId];
                    if (slot.Empty) continue;

                    var stack = slot.Itemstack;

                    if (crateType != null && stack.Collectible != crateType) continue;

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

            be.MarkDirty();
        }

        public void Dispose()
        {
            _capi.Event.MouseDown -= OnMouseDown;
            _capi.Event.MouseUp -= OnMouseUp;
        }
    }
}
