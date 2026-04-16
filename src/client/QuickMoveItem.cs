using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class QuickMoveItem : IDisposable
{
    private const string ChannelName = "vintagetweaks_quickmove_item";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.QuickMoveConfig _config;
    private readonly QuickMoveClickState _clickState = new();

    public QuickMoveItem(ICoreClientAPI capi, VintageTweaksSystem.QuickMoveConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnMouseDown;
        _capi.Event.MouseUp += OnMouseUp;

        _capi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.QuickMoveItemPayload>();
    }

    private static bool SameSlot(QuickMoveClickState clickState, InventoryBase inventory, int slotId)
    {
        return string.Equals(clickState.InventoryId, inventory.InventoryID, StringComparison.Ordinal)
            && clickState.SlotId == slotId;
    }

    private bool IsSuccessfulDoubleClick(InventoryBase inventory, int slotId, long clickTimeMs)
    {
        return _clickState.ClickComplete
            && SameSlot(_clickState, inventory, slotId)
            && clickTimeMs - _clickState.ClickTimeMs <= _config.QuickMoveDelayMs
            && _clickState.ItemStackData.Length > 0;
    }

    private void OnMouseDown(MouseEvent mouseEvent)
    {
        if (!_config.AllowQuickMoveItem || mouseEvent.Button != EnumMouseButton.Left)
        {
            return;
        }

        if (!ClientInventoryInput.IsShiftDown(_capi))
        {
            _clickState.Reset();
            return;
        }

        if (!ClientInventoryInput.TryGetHoveredSlot(_capi, _config.BackpackSlots, out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId))
        {
            _clickState.Reset();
            return;
        }

        long clickTimeMs = _capi.World.ElapsedMilliseconds;
        if (IsSuccessfulDoubleClick(inventory, slotId, clickTimeMs))
        {
            _capi.Network.GetChannel(ChannelName)
                .SendPacket(new shared.QuickMoveItemPayload(
                    _clickState.InventoryId,
                    _clickState.SlotId,
                    _clickState.ItemStackData
                ));

            _clickState.Reset();
            return;
        }

        if (hoveredSlot.Empty)
        {
            _clickState.Reset();
            return;
        }

        _clickState.InventoryId = inventory.InventoryID;
        _clickState.SlotId = slotId;
        _clickState.ItemStackData = hoveredSlot.Itemstack.ToBytes();
        _clickState.ClickTimeMs = clickTimeMs;
        _clickState.ClickComplete = false;
    }

    private void OnMouseUp(MouseEvent mouseEvent)
    {
        if (!_config.AllowQuickMoveItem || mouseEvent.Button != EnumMouseButton.Left)
        {
            return;
        }

        if (!ClientInventoryInput.IsShiftDown(_capi))
        {
            _clickState.Reset();
            return;
        }

        _clickState.ClickComplete = true;
    }

    public void Dispose()
    {
        _capi.Event.MouseDown -= OnMouseDown;
        _capi.Event.MouseUp -= OnMouseUp;
    }

    private sealed class QuickMoveClickState
    {
        public string InventoryId = string.Empty;
        public int SlotId = -1;
        public byte[] ItemStackData = [];
        public long ClickTimeMs;
        public bool ClickComplete;

        public void Reset()
        {
            InventoryId = string.Empty;
            SlotId = -1;
            ItemStackData = [];
            ClickTimeMs = 0;
            ClickComplete = false;
        }
    }
}
