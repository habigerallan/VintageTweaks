using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class SweepLeaveItem
{
    private const string ChannelName = "vintagetweaks_sweepleave_item";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.SweepLeaveConfig _config;

    public SweepLeaveItem(ICoreServerAPI sapi, VintageTweaksSystem.SweepLeaveConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.SweepLeaveItemPayload>()
            .SetMessageHandler<shared.SweepLeaveItemPayload>(OnSweepLeaveRequest);
    }

    private bool IsProtectedSlot(IInventory inventory, int slotId)
    {
        return inventory.ClassName == GlobalConstants.mousecursorInvClassName
            || (inventory.ClassName == GlobalConstants.backpackInvClassName && slotId < _config.BackpackSlots);
    }

    private bool ValidateTargetSlot(
        IServerPlayer player,
        shared.SweepLeaveItemPayload payload,
        out ItemSlot targetSlot
    )
    {
        targetSlot = null;
        if (string.IsNullOrWhiteSpace(payload.TargetInventoryId))
        {
            return false;
        }

        if (!player.InventoryManager.GetInventory(payload.TargetInventoryId, out InventoryBase targetInventory))
        {
            return false;
        }

        if (targetInventory == null
            || !player.InventoryManager.HasInventory(targetInventory)
            || payload.TargetSlotId < 0
            || payload.TargetSlotId >= targetInventory.Count
            || IsProtectedSlot(targetInventory, payload.TargetSlotId))
        {
            return false;
        }

        targetSlot = targetInventory[payload.TargetSlotId];
        return targetSlot != null && targetSlot.Empty;
    }

    private void OnSweepLeaveRequest(IServerPlayer player, shared.SweepLeaveItemPayload payload)
    {
        if (!_config.AllowSweepLeaveItem)
        {
            return;
        }

        ItemSlot mouseSlot = player.InventoryManager.MouseItemSlot;
        if (mouseSlot == null || mouseSlot.Empty)
        {
            return;
        }

        if (!ValidateTargetSlot(player, payload, out ItemSlot targetSlot))
        {
            return;
        }

        mouseSlot.TryPutInto(_sapi.World, targetSlot, 1);
    }
}
