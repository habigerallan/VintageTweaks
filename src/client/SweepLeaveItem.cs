using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class SweepLeaveItem : IDisposable
{
    private const string ChannelName = "vintagetweaks_sweepleave_item";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.SweepLeaveConfig _config;
    private readonly HashSet<string> _visitedSlots = [];
    private bool _isSweeping;
    private string _lastHoveredSlotKey = string.Empty;

    public SweepLeaveItem(ICoreClientAPI capi, VintageTweaksSystem.SweepLeaveConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnMouseDown;
        _capi.Event.MouseMove += OnMouseMove;
        _capi.Event.MouseUp += OnMouseUp;

        _capi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.SweepLeaveItemPayload>();
    }

    private bool TryGetHoveredSlot(out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId)
    {
        return ClientInventoryInput.TryGetHoveredSlot(_capi, _config.BackpackSlots, out hoveredSlot, out inventory, out slotId);
    }

    private void StopSweep()
    {
        _isSweeping = false;
        _lastHoveredSlotKey = string.Empty;
        _visitedSlots.Clear();
    }

    private void TryLeaveHoveredSlot()
    {
        if (!TryGetHoveredSlot(out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId))
        {
            return;
        }

        string slotKey = ClientInventoryInput.GetSlotKey(inventory, slotId);
        if (slotKey == _lastHoveredSlotKey)
        {
            return;
        }

        _lastHoveredSlotKey = slotKey;
        if (!_visitedSlots.Add(slotKey) || !hoveredSlot.Empty)
        {
            return;
        }

        _capi.Network.GetChannel(ChannelName)
            .SendPacket(new shared.SweepLeaveItemPayload(inventory.InventoryID, slotId));
    }

    private void OnMouseDown(MouseEvent mouseEvent)
    {
        if (!_config.AllowSweepLeaveItem
            || mouseEvent.Button != EnumMouseButton.Left
            || !ClientInventoryInput.IsShiftDown(_capi)
            || ClientInventoryInput.IsMouseSlotEmpty(_capi))
        {
            StopSweep();
            return;
        }

        if (!TryGetHoveredSlot(out _, out _, out _))
        {
            StopSweep();
            return;
        }

        _isSweeping = true;
        _visitedSlots.Clear();
        _lastHoveredSlotKey = string.Empty;
        mouseEvent.Handled = true;
        TryLeaveHoveredSlot();
    }

    private void OnMouseMove(MouseEvent mouseEvent)
    {
        if (!_isSweeping)
        {
            return;
        }

        if (!_config.AllowSweepLeaveItem
            || !ClientInventoryInput.IsShiftDown(_capi)
            || !ClientInventoryInput.IsLeftMouseDown(_capi)
            || ClientInventoryInput.IsMouseSlotEmpty(_capi))
        {
            StopSweep();
            return;
        }

        TryLeaveHoveredSlot();
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
