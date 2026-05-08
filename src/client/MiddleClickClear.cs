using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class MiddleClickClear : IDisposable
{
    private const string ClearChannelName = "vintagetweaks_clear";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.MiddleClickClearConfig _config;

    public MiddleClickClear(ICoreClientAPI capi, VintageTweaksSystem.MiddleClickClearConfig config)
    {
        _capi = capi;
        _config = config;

        _capi.Event.MouseDown += OnMouseDown;

        _capi.Network.RegisterChannel(ClearChannelName)
            .RegisterMessageType<shared.MiddleClickClearPayload>();
    }

    private void OnMouseDown(MouseEvent mouseEvent)
    {
        if (mouseEvent.Handled || mouseEvent.Button != EnumMouseButton.Middle || !_config.AllowMiddleClickClear)
        {
            return;
        }

        if (!ClientInventoryInput.TryGetHoveredSlot(_capi, 0, out ItemSlot hoveredSlot, out InventoryBase inventory, out int slotId))
        {
            return;
        }

        if (!ClientInventoryInput.IsCraftingOutputSlot(hoveredSlot, inventory))
        {
            return;
        }

        mouseEvent.Handled = true;

        _capi.Network.GetChannel(ClearChannelName)
            .SendPacket(new shared.MiddleClickClearPayload(inventory.InventoryID, slotId));
    }

    public void Dispose()
    {
        _capi.Event.MouseDown -= OnMouseDown;
    }
}
