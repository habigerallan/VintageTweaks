using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class MiddleClickSort : IDisposable
{
    private const string SortChannelName = "vintagetweaks_sort";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.SortConfig _config;

    public MiddleClickSort(ICoreClientAPI capi, VintageTweaksSystem.SortConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnMouseDown;

        _capi.Network.RegisterChannel(SortChannelName)
            .RegisterMessageType<shared.MiddleClickSortPayload>();
    }

    private void OnMouseDown(MouseEvent mouseEvent)
    {
        if (mouseEvent.Button != EnumMouseButton.Middle || !_config.AllowMiddleClickSort)
        {
            return;
        }

        if (!ClientInventoryInput.TryGetHoveredSlot(_capi, _config.BackpackSlots, out ItemSlot hoveredSlot, out InventoryBase inventory, out _))
        {
            return;
        }

        if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && !hoveredSlot.Empty)
        {
            return;
        }

        _capi.Network.GetChannel(SortChannelName)
            .SendPacket(new shared.MiddleClickSortPayload(inventory.InventoryID));
    }

    public void Dispose()
    {
        _capi.Event.MouseDown -= OnMouseDown;
    }
}
