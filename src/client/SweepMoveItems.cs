using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class SweepMoveItems : IDisposable
{
    private const string ChannelName = "vintagetweaks_sweepmove_items";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.SweepMoveConfig _config;
    private readonly HashSet<string> _sweptSlots = [];
    private bool _isSweeping;
    private string _lastHoveredSlotKey = string.Empty;

    public SweepMoveItems(ICoreClientAPI capi, VintageTweaksSystem.SweepMoveConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnMouseDown;
        _capi.Event.MouseMove += OnMouseMove;
        _capi.Event.MouseUp += OnMouseUp;

        _capi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.SweepMoveItemsPayload>();
    }

    private bool TryGetHoveredSlot(out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId)
    {
        return ClientInventoryInput.TryGetHoveredSlot(_capi, _config.BackpackSlots, out hoveredSlot, out inventory, out slotId);
    }

    private bool TryGetHoveredMoveSlot(out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId)
    {
        return TryGetHoveredSlot(out hoveredSlot, out inventory, out slotId)
            && !hoveredSlot.Empty;
    }

    private void StopSweep()
    {
        _isSweeping = false;
        _lastHoveredSlotKey = string.Empty;
        _sweptSlots.Clear();
    }

    private void SweepHoveredSlot()
    {
        if (!TryGetHoveredMoveSlot(out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId))
        {
            return;
        }

        string slotKey = ClientInventoryInput.GetSlotKey(inventory, slotId);
        if (slotKey == _lastHoveredSlotKey)
        {
            return;
        }

        _lastHoveredSlotKey = slotKey;
        if (!_sweptSlots.Add(slotKey))
        {
            return;
        }

        _capi.Network.GetChannel(ChannelName)
            .SendPacket(new shared.SweepMoveItemsPayload(
                inventory.InventoryID,
                slotId,
                hoveredSlot.Itemstack.ToBytes()
            ));
    }

    private void OnMouseDown(MouseEvent mouseEvent)
    {
        if (!_config.AllowSweepMoveItems
            || mouseEvent.Button != EnumMouseButton.Left
            || !ClientInventoryInput.IsShiftDown(_capi)
            || !ClientInventoryInput.IsMouseSlotEmpty(_capi))
        {
            StopSweep();
            return;
        }

        if (!TryGetHoveredSlot(out _, out InventoryBase inventory, out int slotId))
        {
            StopSweep();
            return;
        }

        _isSweeping = true;
        _sweptSlots.Clear();
        _lastHoveredSlotKey = ClientInventoryInput.GetSlotKey(inventory, slotId);
        _sweptSlots.Add(_lastHoveredSlotKey);
    }

    private void OnMouseMove(MouseEvent mouseEvent)
    {
        if (!_isSweeping)
        {
            return;
        }

        if (!_config.AllowSweepMoveItems
            || !ClientInventoryInput.IsShiftDown(_capi)
            || !ClientInventoryInput.IsLeftMouseDown(_capi)
            || !ClientInventoryInput.IsMouseSlotEmpty(_capi))
        {
            StopSweep();
            return;
        }

        SweepHoveredSlot();
    }

    private void OnMouseUp(MouseEvent mouseEvent)
    {
        if (mouseEvent.Button == EnumMouseButton.Left)
        {
            StopSweep();
        }
    }

    public void Dispose()
    {
        _capi.Event.MouseDown -= OnMouseDown;
        _capi.Event.MouseMove -= OnMouseMove;
        _capi.Event.MouseUp -= OnMouseUp;
    }
}
