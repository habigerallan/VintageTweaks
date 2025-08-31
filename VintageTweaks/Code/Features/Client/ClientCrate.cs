using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageTweaks.Code.Features.Client
{
    internal sealed class Crate : IDisposable
    {
        private const string _pushChannelName = "vintagetweaks_crate_push";
        private const string _pullChannelName = "vintagetweaks_crate_pull";

        private readonly ICoreClientAPI _capi;
        private readonly Config.Crate _cfg;

        private readonly Shared.CrateClickObject _pushObject;
        private readonly Shared.CrateClickObject _pullObject;

        public Crate(ICoreClientAPI capi, Config.Crate cfg)
        {
            _capi = capi;
            _cfg = cfg;

            _pushObject = new Shared.CrateClickObject();
            _pullObject = new Shared.CrateClickObject();

            _capi.Event.MouseDown += CratePushDown;
            _capi.Event.MouseUp += CratePushUp;
            _capi.Event.MouseDown += CratePullDown;
            _capi.Event.MouseUp += CratePullUp;

            _capi.Network.RegisterChannel(_pushChannelName)
                .RegisterMessageType<Network.CrateRequest>();

            _capi.Network.RegisterChannel(_pullChannelName)
                .RegisterMessageType<Network.CrateRequest>();
        }

        public static void ResetClick(Shared.CrateClickObject clickObject)
        {
            clickObject.ClickTime = 0;
            clickObject.ClickPos = null;
            clickObject.ClickItem = null;
            clickObject.ClickComplete = false;
        }

        private bool IsSuccessful(Shared.CrateClickObject crateObject, BlockPos clickPos, long clickTime, CollectibleObject clickItem)
        {
            return clickPos.Equals(crateObject.ClickPos)
                && clickItem == crateObject.ClickItem
                && clickTime - crateObject.ClickTime <= _cfg.CrateDelayMs
                && crateObject.ClickComplete;
        }

        private bool IsKeyDown(GlKeys glkey)
        {
            bool[] keyboard = _capi.Input.KeyboardKeyState;
            return keyboard[(int)glkey];
        }

        private BlockPos GetHoveredBlockPos()
        {
            BlockSelection hovered = _capi.World.Player.CurrentBlockSelection;
            return hovered?.Position;
        }

        private bool PlayerContainsCrateItem(CollectibleObject collectibleObject)
        {
            string[] inventoryLayers = { "hotbar", "backpack" };
            IPlayerInventoryManager invManager = _capi.World.Player.InventoryManager;

            foreach (string layer in inventoryLayers)
            {
                IInventory inventory = invManager.GetOwnInventory(layer);
                if (inventory == null) continue;

                foreach (ItemSlot slot in inventory)
                {
                    if (slot.Empty) continue;
                    if (slot.Itemstack.Collectible.Code == collectibleObject.Code) return true;
                }
            }
            return false;
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

            InventoryBase crateInv = Shared.Functions.InventoryFromBlockPos(_capi, currentPos);
            if (crateInv == null || crateInv.ClassName != "crate" || crateInv.Count == 0 || crateInv.Empty)
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

            long currentClickTime = _capi.World.ElapsedMilliseconds;
            if (IsSuccessful(_pushObject, currentPos, currentClickTime, currentItem))
            {
                _capi.Network.GetChannel(_pushChannelName)
                    .SendPacket(new Network.CrateRequest(currentPos.X, currentPos.Y, currentPos.Z, currentItem.Code));

                ResetClick(_pushObject);
            }
            else
            {
                _pushObject.ClickPos = currentPos;
                _pushObject.ClickTime = currentClickTime;
                _pushObject.ClickItem = currentItem;
                _pushObject.ClickComplete = false;
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

        private void CratePullDown(MouseEvent e)
        {
            if (!_cfg.AllowCratePull) return;
            if (e.Button != EnumMouseButton.Right) return;

            if (!IsKeyDown(GlKeys.ControlLeft))
            {
                ResetClick(_pullObject);
                return;
            }

            BlockPos currentPos = GetHoveredBlockPos();
            if (currentPos == null)
            {
                ResetClick(_pullObject);
                return;
            }

            InventoryBase crateInv = Shared.Functions.InventoryFromBlockPos(_capi, currentPos);
            if (crateInv == null || crateInv.ClassName != "crate" || crateInv.Empty)
            {
                ResetClick(_pullObject);
                return;
            }

            CollectibleObject currentItem = crateInv.FirstNonEmptySlot.Itemstack.Collectible;
            long currentClickTime = _capi.World.ElapsedMilliseconds;
            if (IsSuccessful(_pullObject, currentPos, currentClickTime, currentItem))
            {
                _capi.Network.GetChannel(_pullChannelName)
                    .SendPacket(new Network.CrateRequest(currentPos.X, currentPos.Y, currentPos.Z, currentItem.Code));

                ResetClick(_pullObject);
            }
            else
            {
                _pullObject.ClickPos = currentPos;
                _pullObject.ClickTime = currentClickTime;
                _pullObject.ClickItem = currentItem;
                _pullObject.ClickComplete = false;
            }
        }

        private void CratePullUp(MouseEvent e)
        {
            if (!_cfg.AllowCratePull) return;
            if (e.Button != EnumMouseButton.Right) return;

            if (!IsKeyDown(GlKeys.ControlLeft))
            {
                ResetClick(_pullObject);
                return;
            }

            _pullObject.ClickComplete = true;
        }

        public void Dispose()
        {
            _capi.Event.MouseDown -= CratePushDown;
            _capi.Event.MouseUp -= CratePushUp;
            _capi.Event.MouseDown -= CratePullDown;
            _capi.Event.MouseUp -= CratePullUp;
        }
    }
}
