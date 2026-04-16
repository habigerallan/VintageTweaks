using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class SweepMoveItems
{
    private const string ChannelName = "vintagetweaks_sweepmove_items";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.SweepMoveConfig _config;

    public SweepMoveItems(ICoreServerAPI sapi, VintageTweaksSystem.SweepMoveConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.SweepMoveItemsPayload>()
            .SetMessageHandler<shared.SweepMoveItemsPayload>(OnSweepMoveRequest);
    }

    private bool IsProtectedSlot(IInventory inventory, int slotId)
    {
        return inventory.ClassName == GlobalConstants.mousecursorInvClassName
            || (inventory.ClassName == GlobalConstants.backpackInvClassName && slotId < _config.BackpackSlots);
    }

    private static bool IsMouseSlotEmpty(IServerPlayer player)
    {
        ItemSlot mouseSlot = player.InventoryManager.MouseItemSlot;
        return mouseSlot == null || mouseSlot.Empty;
    }

    private bool MatchesRequestedStack(ItemStack candidate, ItemStack requestedStack)
    {
        return candidate != null
            && requestedStack != null
            && candidate.Equals(_sapi.World, requestedStack, GlobalConstants.IgnoredStackAttributes);
    }

    private ItemStack GetRequestedStack(shared.SweepMoveItemsPayload payload)
    {
        if (payload.ItemStackData == null || payload.ItemStackData.Length == 0)
        {
            return null;
        }

        try
        {
            ItemStack requestedStack = new(payload.ItemStackData);
            return requestedStack.ResolveBlockOrItem(_sapi.World) ? requestedStack : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private bool ValidateSourceSlot(
        IServerPlayer player,
        shared.SweepMoveItemsPayload payload,
        ItemStack requestedStack,
        out ItemSlot sourceSlot
    )
    {
        sourceSlot = null;
        if (string.IsNullOrWhiteSpace(payload.SourceInventoryId))
        {
            return false;
        }

        if (!player.InventoryManager.GetInventory(payload.SourceInventoryId, out InventoryBase sourceInventory))
        {
            return false;
        }

        if (sourceInventory == null
            || !player.InventoryManager.HasInventory(sourceInventory)
            || payload.SourceSlotId < 0
            || payload.SourceSlotId >= sourceInventory.Count
            || IsProtectedSlot(sourceInventory, payload.SourceSlotId))
        {
            return false;
        }

        sourceSlot = sourceInventory[payload.SourceSlotId];
        return sourceSlot != null
            && !sourceSlot.Empty
            && MatchesRequestedStack(sourceSlot.Itemstack, requestedStack);
    }

    private void TryTransferAway(IServerPlayer player, ItemSlot sourceSlot, ItemStack requestedStack)
    {
        while (!sourceSlot.Empty && MatchesRequestedStack(sourceSlot.Itemstack, requestedStack))
        {
            int previousStackSize = sourceSlot.StackSize;
            ItemStackMoveOperation moveOperation = new(
                _sapi.World,
                EnumMouseButton.Left,
                EnumModifierKey.SHIFT,
                EnumMergePriority.AutoMerge,
                previousStackSize
            )
            {
                ActingPlayer = player
            };

            player.InventoryManager.TryTransferAway(sourceSlot, ref moveOperation, false);

            if (moveOperation.MovedQuantity <= 0 || (!sourceSlot.Empty && sourceSlot.StackSize >= previousStackSize))
            {
                return;
            }
        }
    }

    private void OnSweepMoveRequest(IServerPlayer player, shared.SweepMoveItemsPayload payload)
    {
        if (!_config.AllowSweepMoveItems || !IsMouseSlotEmpty(player))
        {
            return;
        }

        ItemStack requestedStack = GetRequestedStack(payload);
        if (requestedStack == null)
        {
            return;
        }

        if (ValidateSourceSlot(player, payload, requestedStack, out ItemSlot sourceSlot))
        {
            TryTransferAway(player, sourceSlot, requestedStack);
        }
    }
}
