using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class QuickMoveItem
{
    private static readonly string[] PlayerInventoryLayers =
    [
        GlobalConstants.hotBarInvClassName,
        GlobalConstants.backpackInvClassName
    ];

    private const string ChannelName = "vintagetweaks_quickmove_item";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.QuickMoveConfig _config;

    public QuickMoveItem(ICoreServerAPI sapi, VintageTweaksSystem.QuickMoveConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.QuickMoveItemPayload>()
            .SetMessageHandler<shared.QuickMoveItemPayload>(OnQuickMoveRequest);
    }

    private bool IsProtectedBackpackSlot(IInventory inventory, int slotId)
    {
        return inventory.ClassName == GlobalConstants.backpackInvClassName && slotId < _config.BackpackSlots;
    }

    private static bool IsProtectedInventory(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.mousecursorInvClassName;
    }

    private bool MatchesRequestedStack(ItemStack candidate, ItemStack requestedStack)
    {
        return candidate != null
            && requestedStack != null
            && candidate.Equals(_sapi.World, requestedStack, GlobalConstants.IgnoredStackAttributes);
    }

    private static bool IsPlayerStorageInventory(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.hotBarInvClassName
            || inventory.ClassName == GlobalConstants.backpackInvClassName;
    }

    private ItemStack GetRequestedStack(shared.QuickMoveItemPayload payload)
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

    private bool ValidateSourceInventory(IServerPlayer player, shared.QuickMoveItemPayload payload, out InventoryBase sourceInventory)
    {
        sourceInventory = null;
        if (string.IsNullOrWhiteSpace(payload.SourceInventoryId))
        {
            return false;
        }

        if (!player.InventoryManager.GetInventory(payload.SourceInventoryId, out sourceInventory))
        {
            return false;
        }

        return sourceInventory != null
            && player.InventoryManager.HasInventory(sourceInventory)
            && payload.SourceSlotId >= 0
            && payload.SourceSlotId < sourceInventory.Count
            && !IsProtectedInventory(sourceInventory)
            && !IsProtectedBackpackSlot(sourceInventory, payload.SourceSlotId);
    }

    private bool ValidateSourceSlot(InventoryBase sourceInventory, int slotId, ItemStack requestedStack)
    {
        ItemSlot sourceSlot = sourceInventory[slotId];
        return sourceSlot.Empty || MatchesRequestedStack(sourceSlot.Itemstack, requestedStack);
    }

    private int GetFirstUsableSlot(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.backpackInvClassName ? _config.BackpackSlots : 0;
    }

    private void TryTransferMatchingSlots(IServerPlayer player, IInventory inventory, ItemStack requestedStack)
    {
        if (inventory == null)
        {
            return;
        }

        int firstSlot = GetFirstUsableSlot(inventory);
        for (int slotIndex = firstSlot; slotIndex < inventory.Count; slotIndex++)
        {
            ItemSlot slot = inventory[slotIndex];
            if (slot.Empty || !MatchesRequestedStack(slot.Itemstack, requestedStack))
            {
                continue;
            }

            TryTransferAway(player, slot);
        }
    }

    private void TryTransferMatchingSlotsToInventory(IInventory sourceInventory, IInventory targetInventory, ItemStack requestedStack)
    {
        if (sourceInventory == null || targetInventory == null)
        {
            return;
        }

        int firstSourceSlot = GetFirstUsableSlot(sourceInventory);
        for (int slotIndex = firstSourceSlot; slotIndex < sourceInventory.Count; slotIndex++)
        {
            ItemSlot sourceSlot = sourceInventory[slotIndex];
            if (sourceSlot.Empty || !MatchesRequestedStack(sourceSlot.Itemstack, requestedStack))
            {
                continue;
            }

            TryTransferSlotToInventory(sourceSlot, targetInventory, requestedStack);
        }
    }

    private void TryTransferSlotToInventory(ItemSlot sourceSlot, IInventory targetInventory, ItemStack requestedStack)
    {
        TryTransferSlotToTargetSlots(sourceSlot, targetInventory, requestedStack, false);
        TryTransferSlotToTargetSlots(sourceSlot, targetInventory, requestedStack, true);
    }

    private void TryTransferSlotToTargetSlots(
        ItemSlot sourceSlot,
        IInventory targetInventory,
        ItemStack requestedStack,
        bool allowEmptyTargets
    )
    {
        int firstTargetSlot = GetFirstUsableSlot(targetInventory);
        for (int slotIndex = firstTargetSlot; slotIndex < targetInventory.Count && !sourceSlot.Empty; slotIndex++)
        {
            ItemSlot targetSlot = targetInventory[slotIndex];
            if (targetSlot.Empty != allowEmptyTargets)
            {
                continue;
            }

            if (!allowEmptyTargets && !MatchesRequestedStack(targetSlot.Itemstack, requestedStack))
            {
                continue;
            }

            sourceSlot.TryPutInto(_sapi.World, targetSlot, sourceSlot.StackSize);
        }
    }

    private void TryTransferPlayerStorageToOtherLayer(IServerPlayer player, IInventory sourceInventory, ItemStack requestedStack)
    {
        string targetInventoryLayer = sourceInventory.ClassName == GlobalConstants.hotBarInvClassName
            ? GlobalConstants.backpackInvClassName
            : GlobalConstants.hotBarInvClassName;

        TryTransferMatchingSlotsToInventory(
            sourceInventory,
            player.InventoryManager.GetOwnInventory(targetInventoryLayer),
            requestedStack
        );
    }

    private static bool HasExternalInventoryOpen(IServerPlayer player)
    {
        foreach (IInventory inventory in player.InventoryManager.OpenedInventories)
        {
            if (!IsPlayerInternalInventory(inventory))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPlayerInternalInventory(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.hotBarInvClassName
            || inventory.ClassName == GlobalConstants.backpackInvClassName
            || inventory.ClassName == GlobalConstants.mousecursorInvClassName
            || inventory.ClassName == GlobalConstants.characterInvClassName
            || inventory.ClassName == GlobalConstants.craftingInvClassName
            || inventory.ClassName == GlobalConstants.creativeInvClassName
            || inventory.ClassName == GlobalConstants.groundInvClassName;
    }

    private void TryTransferAway(IServerPlayer player, ItemSlot sourceSlot)
    {
        while (!sourceSlot.Empty)
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

    private void OnQuickMoveRequest(IServerPlayer player, shared.QuickMoveItemPayload payload)
    {
        if (!_config.AllowQuickMoveItem)
        {
            return;
        }

        ItemStack requestedStack = GetRequestedStack(payload);
        if (requestedStack == null)
        {
            return;
        }

        if (!ValidateSourceInventory(player, payload, out InventoryBase sourceInventory)
            || !ValidateSourceSlot(sourceInventory, payload.SourceSlotId, requestedStack))
        {
            return;
        }

        if (!IsPlayerStorageInventory(sourceInventory))
        {
            TryTransferMatchingSlots(player, sourceInventory, requestedStack);
            return;
        }

        if (!HasExternalInventoryOpen(player))
        {
            TryTransferPlayerStorageToOtherLayer(player, sourceInventory, requestedStack);
            return;
        }

        foreach (string inventoryLayer in PlayerInventoryLayers)
        {
            TryTransferMatchingSlots(player, player.InventoryManager.GetOwnInventory(inventoryLayer), requestedStack);
        }
    }
}
