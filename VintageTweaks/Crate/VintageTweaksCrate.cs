using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using VintageTweaks.Config;
using VintageTweaks.Classes;

namespace VintageTweaks.Crate
{
    public class VintageTweaksCrate : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private readonly ICoreServerAPI _sapi;
        private readonly VintageTweaksConfig _cfg;
        private BlockPos lastPos;
        private long lastClickMillis;

        public VintageTweaksCrate(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            this._capi = capi;
            this._cfg = cfg;
            capi.Event.MouseUp += OnMouseUp;
            capi.Network.RegisterChannel("vintagetweakscrate")
                .RegisterMessageType<CratePushRequest>();
        }

        public VintageTweaksCrate(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            this._sapi = sapi;
            this._cfg = cfg;
            sapi.Network.RegisterChannel("vintagetweakscrate")
                .RegisterMessageType<CratePushRequest>()
                .SetMessageHandler<CratePushRequest>(OnCratePushRequest);
        }

        private void OnMouseUp(MouseEvent e)
        {
            if (!_cfg.AllowCratePush) return;
            if (e.Button != EnumMouseButton.Right) return;
            var sel = _capi.World.Player.CurrentBlockSelection;
            if (sel == null) return;

            var pos = sel.Position;
            var kb = _capi.Input.KeyboardKeyState;
            bool shift = kb[(int)GlKeys.ShiftLeft] || kb[(int)GlKeys.ShiftRight];
            if (shift)
            {
                lastPos = new BlockPos(pos.X, pos.Y, pos.Z);
                lastClickMillis = _capi.World.ElapsedMilliseconds;
                return;
            }

            if (lastPos != null
                && lastPos.Equals(pos)
                && _capi.World.ElapsedMilliseconds - lastClickMillis < _cfg.CratePushDelayMs)
            {
                _capi.Network.GetChannel("vintagetweakscrate")
                    .SendPacket(new CratePushRequest { X = pos.X, Y = pos.Y, Z = pos.Z });
            }
        }

        private void OnCratePushRequest(IServerPlayer player, CratePushRequest msg)
        {
            if (!_cfg.AllowCratePush) return;

            var pos = new BlockPos(msg.X, msg.Y, msg.Z);
            var be = _sapi.World.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return;

            var invProp = be.GetType().GetProperty("Inventory");
            var crateInv = invProp?.GetValue(be) as IInventory;
            if (crateInv == null) return;

            IInventory hotbar = player.InventoryManager.GetOwnInventory("hotbar");
            IInventory backpack = player.InventoryManager.GetOwnInventory("backpack");

            foreach (var playerInv in new[] { hotbar, backpack })
            {
                if (playerInv == null) continue;

                foreach (var slot in playerInv)
                {
                    if (slot.Empty) continue;
                    var stack = slot.Itemstack;

                    if (crateInv.Count > 0
                        && !crateInv[0].Empty
                        && stack.Collectible != crateInv[0].Itemstack.Collectible)
                    {
                        continue;
                    }

                    int toMove = slot.StackSize;

                    foreach (var cslot in crateInv)
                    {
                        if (!cslot.Empty && cslot.Itemstack.Collectible == stack.Collectible)
                        {
                            toMove -= slot.TryPutInto(_sapi.World, cslot, toMove);
                            if (toMove <= 0) break;
                        }
                    }

                    if (toMove > 0)
                    {
                        foreach (var cslot in crateInv)
                        {
                            if (cslot.Empty)
                            {
                                toMove -= slot.TryPutInto(_sapi.World, cslot, toMove);
                                if (toMove <= 0) break;
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _capi.Event.MouseUp -= OnMouseUp;
        }
    }
}
