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

    private bool MatchesHeldStack(ItemStack candidate, ItemStack heldStack)
    {
        return candidate != null
            && heldStack != null
            && candidate.Equals(_sapi.World, heldStack, GlobalConstants.IgnoredStackAttributes);
    }

    private bool CanLeaveIntoSlot(ItemSlot sourceSlot, ItemSlot targetSlot)
    {
        return targetSlot != null
            && (targetSlot.Empty || MatchesHeldStack(targetSlot.Itemstack, sourceSlot.Itemstack))
            && ItemMoveRules.CanMoveInto(sourceSlot, targetSlot);
    }

    private bool ValidateTargetSlot(
        IServerPlayer player,
        shared.SweepLeaveItemPayload payload,
        ItemSlot sourceSlot,
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
        return CanLeaveIntoSlot(sourceSlot, targetSlot);
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

        if (!ValidateTargetSlot(player, payload, mouseSlot, out ItemSlot targetSlot))
        {
            return;
        }

        ItemMoveRules.TryPutInto(_sapi.World, mouseSlot, targetSlot, 1);
    }
}
