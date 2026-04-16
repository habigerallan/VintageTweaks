using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace VintageTweaks.src.client;

internal sealed class QuickStackCrate : IDisposable
{
    private static readonly string[] InventoryLayers =
    [
        GlobalConstants.hotBarInvClassName,
        GlobalConstants.backpackInvClassName
    ];

    private const string CrateInventoryClassName = "crate";
    private const string PushChannelName = "vintagetweaks_crate_push";
    private const string PullChannelName = "vintagetweaks_crate_pull";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.CrateConfig _config;
    private readonly shared.CrateClickState _pushClickState = new();
    private readonly shared.CrateClickState _pullClickState = new();

    public QuickStackCrate(ICoreClientAPI capi, VintageTweaksSystem.CrateConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnPushMouseDown;
        _capi.Event.MouseUp += OnPushMouseUp;
        _capi.Event.MouseDown += OnPullMouseDown;
        _capi.Event.MouseUp += OnPullMouseUp;

        _capi.Network.RegisterChannel(PushChannelName)
            .RegisterMessageType<shared.ClickedCratePayload>();

        _capi.Network.RegisterChannel(PullChannelName)
            .RegisterMessageType<shared.ClickedCratePayload>();
    }

    private BlockPos GetHoveredBlockPos()
    {
        BlockSelection hoveredBlock = _capi.World.Player.CurrentBlockSelection;
        return hoveredBlock?.Position?.Copy();
    }

    private bool IsKeyDown(GlKeys key)
    {
        return _capi.Input.KeyboardKeyState[(int)key];
    }

    private bool IsSuccessful(shared.CrateClickState clickState, BlockPos hoveredPosition, long clickTimeMs, CollectibleObject clickedItem)
    {
        return clickState.ClickComplete
            && clickState.ClickPos != null
            && hoveredPosition.Equals(clickState.ClickPos)
            && clickedItem == clickState.ClickItem
            && clickTimeMs - clickState.ClickTimeMs <= _config.CrateDelayMs;
    }

    private bool PlayerContainsItem(CollectibleObject collectible)
    {
        IPlayerInventoryManager inventoryManager = _capi.World.Player.InventoryManager;

        foreach (string inventoryLayer in InventoryLayers)
        {
            IInventory inventory = inventoryManager.GetOwnInventory(inventoryLayer);
            if (inventory == null)
            {
                continue;
            }

            foreach (ItemSlot slot in inventory)
            {
                if (slot.Empty)
                {
                    continue;
                }

                if (slot.Itemstack.Collectible.Code == collectible.Code)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsCrateInventoryWithItem(InventoryBase inventory)
    {
        return inventory != null && inventory.ClassName == CrateInventoryClassName && !inventory.Empty;
    }

    private void OnPushMouseDown(MouseEvent mouseEvent)
    {
        if (!_config.AllowCratePush || mouseEvent.Button != EnumMouseButton.Right)
        {
            return;
        }

        if (!IsKeyDown(GlKeys.ShiftLeft))
        {
            _pushClickState.Reset();
            return;
        }

        BlockPos hoveredPosition = GetHoveredBlockPos();
        if (hoveredPosition == null)
        {
            _pushClickState.Reset();
            return;
        }

        InventoryBase crateInventory = shared.InventoryFromPosition.InventoryFromBlockPos(_capi, hoveredPosition);
        if (!IsCrateInventoryWithItem(crateInventory))
        {
            _pushClickState.Reset();
            return;
        }

        CollectibleObject clickedItem = crateInventory.FirstNonEmptySlot.Itemstack.Collectible;
        if (!PlayerContainsItem(clickedItem))
        {
            _pushClickState.Reset();
            return;
        }

        long clickTimeMs = _capi.World.ElapsedMilliseconds;
        if (IsSuccessful(_pushClickState, hoveredPosition, clickTimeMs, clickedItem))
        {
            _capi.Network.GetChannel(PushChannelName)
                .SendPacket(new shared.ClickedCratePayload(
                    hoveredPosition.X,
                    hoveredPosition.Y,
                    hoveredPosition.Z,
                    clickedItem.Code.ToString()
                ));

            _pushClickState.Reset();
            return;
        }

        _pushClickState.ClickPos = hoveredPosition;
        _pushClickState.ClickTimeMs = clickTimeMs;
        _pushClickState.ClickItem = clickedItem;
        _pushClickState.ClickComplete = false;
    }

    private void OnPushMouseUp(MouseEvent mouseEvent)
    {
        if (!_config.AllowCratePush || mouseEvent.Button != EnumMouseButton.Right)
        {
            return;
        }

        if (!IsKeyDown(GlKeys.ShiftLeft))
        {
            _pushClickState.Reset();
            return;
        }

        _pushClickState.ClickComplete = true;
    }

    private void OnPullMouseDown(MouseEvent mouseEvent)
    {
        if (!_config.AllowCratePull || mouseEvent.Button != EnumMouseButton.Right)
        {
            return;
        }

        if (!IsKeyDown(GlKeys.ControlLeft))
        {
            _pullClickState.Reset();
            return;
        }

        BlockPos hoveredPosition = GetHoveredBlockPos();
        if (hoveredPosition == null)
        {
            _pullClickState.Reset();
            return;
        }

        InventoryBase crateInventory = shared.InventoryFromPosition.InventoryFromBlockPos(_capi, hoveredPosition);
        if (!IsCrateInventoryWithItem(crateInventory))
        {
            _pullClickState.Reset();
            return;
        }

        CollectibleObject clickedItem = crateInventory.FirstNonEmptySlot.Itemstack.Collectible;
        long clickTimeMs = _capi.World.ElapsedMilliseconds;
        if (IsSuccessful(_pullClickState, hoveredPosition, clickTimeMs, clickedItem))
        {
            _capi.Network.GetChannel(PullChannelName)
                .SendPacket(new shared.ClickedCratePayload(
                    hoveredPosition.X,
                    hoveredPosition.Y,
                    hoveredPosition.Z,
                    clickedItem.Code.ToString()
                ));

            _pullClickState.Reset();
            return;
        }

        _pullClickState.ClickPos = hoveredPosition;
        _pullClickState.ClickTimeMs = clickTimeMs;
        _pullClickState.ClickItem = clickedItem;
        _pullClickState.ClickComplete = false;
    }

    private void OnPullMouseUp(MouseEvent mouseEvent)
    {
        if (!_config.AllowCratePull || mouseEvent.Button != EnumMouseButton.Right)
        {
            return;
        }

        if (!IsKeyDown(GlKeys.ControlLeft))
        {
            _pullClickState.Reset();
            return;
        }

        _pullClickState.ClickComplete = true;
    }

    public void Dispose()
    {
        _capi.Event.MouseDown -= OnPushMouseDown;
        _capi.Event.MouseUp -= OnPushMouseUp;
        _capi.Event.MouseDown -= OnPullMouseDown;
        _capi.Event.MouseUp -= OnPullMouseUp;
    }
}
