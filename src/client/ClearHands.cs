using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageTweaks.src.client;

internal sealed class ClearHands : IDisposable
{
    private const string ChannelName = "vintagetweaks_clear_hands";
    private const string HotkeyCode = "vintagetweaks_clear_hands";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.ClearHandsConfig _config;

    public ClearHands(ICoreClientAPI capi, VintageTweaksSystem.ClearHandsConfig config)
    {
        _capi = capi;
        _config = config;

        if (!_config.AllowClearHands)
        {
            return;
        }

        _capi.Input.RegisterHotKey(
            HotkeyCode,
            "Clear hands (VintageTweaks)",
            _config.ClearHandsKey,
            HotkeyType.CharacterControls,
            shiftPressed: true
        );

        _capi.Input.SetHotKeyHandler(HotkeyCode, OnClearHands);

        _capi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.ClearHandsPayload>();
    }

    private bool OnClearHands(KeyCombination combination)
    {
        if (!_config.AllowClearHands || !ClientInventoryInput.IsShiftDown(_capi))
        {
            return false;
        }

        ItemSlot activeSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        if (activeSlot == null || activeSlot.Empty || !activeSlot.CanTake())
        {
            return false;
        }

        int sourceSlotId = activeSlot.Inventory?.GetSlotId(activeSlot) ?? -1;
        if (sourceSlotId < 0)
        {
            return false;
        }

        _capi.Network.GetChannel(ChannelName)
            .SendPacket(new shared.ClearHandsPayload(sourceSlotId, activeSlot.Itemstack.ToBytes()));

        return true;
    }

    public void Dispose()
    {
    }
}
