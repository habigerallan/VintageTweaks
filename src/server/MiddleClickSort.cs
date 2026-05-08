using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class MiddleClickSort
{
    private const string SortChannelName = "vintagetweaks_sort";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.SortConfig _config;

    public MiddleClickSort(ICoreServerAPI sapi, VintageTweaksSystem.SortConfig config)
    {
        _sapi = sapi;
        _config = config;

        sapi.Network.RegisterChannel(SortChannelName)
            .RegisterMessageType<shared.MiddleClickSortPayload>()
            .SetMessageHandler<shared.MiddleClickSortPayload>(OnSortRequest);
    }

    private int GetFirstSortableSlot(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.backpackInvClassName ? _config.BackpackSlots : 0;
    }

    private static bool IsSortableSlot(ItemSlot slot)
    {
        return slot != null
            && slot is not ItemSlotOutput
            && slot is not ItemSlotOffhand;
    }

    private static void AddSlotToStorageGroup(List<List<ItemSlot>> slotGroups, ItemSlot slot)
    {
        foreach (List<ItemSlot> slotGroup in slotGroups)
        {
            if (slotGroup[0].StorageType == slot.StorageType)
            {
                slotGroup.Add(slot);
                return;
            }
        }

        slotGroups.Add([slot]);
    }

    private void MergeStacks(List<ItemStack> itemsToBeSorted)
    {
        List<ItemStack> mergedStacks = [];

        foreach (ItemStack stack in itemsToBeSorted)
        {
            foreach (ItemStack mergedStack in mergedStacks)
            {
                if (stack.StackSize <= 0)
                {
                    break;
                }

                ItemStackMergeOperation mergeOperation = new(
                    _sapi.World,
                    EnumMouseButton.Left,
                    (EnumModifierKey)0,
                    EnumMergePriority.AutoMerge,
                    stack.StackSize
                )
                {
                    SourceSlot = new DummySlot(stack),
                    SinkSlot = new DummySlot(mergedStack)
                };

                mergedStack.Collectible.TryMergeStacks(mergeOperation);
            }

            if (stack.StackSize > 0)
            {
                mergedStacks.Add(stack);
            }
        }

        itemsToBeSorted.Clear();
        itemsToBeSorted.AddRange(mergedStacks);
    }

    private static void RestoreSlots(List<ItemSlot> slots, List<ItemStack> originalStacks)
    {
        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            slots[slotIndex].Itemstack = originalStacks[slotIndex]?.Clone();
            slots[slotIndex].MarkDirty();
        }
    }

    private bool TryPlaceSortedStacks(List<ItemStack> sortedStacks, List<ItemSlot> targetSlots)
    {
        foreach (ItemStack stack in sortedStacks)
        {
            DummySlot sourceSlot = new(stack);
            foreach (ItemSlot targetSlot in targetSlots)
            {
                if (sourceSlot.Empty)
                {
                    break;
                }

                ItemMoveRules.TryPutInto(_sapi.World, sourceSlot, targetSlot, sourceSlot.StackSize);
            }

            if (!sourceSlot.Empty)
            {
                return false;
            }
        }

        return true;
    }

    private void SortSlotGroup(List<ItemSlot> sortableSlots)
    {
        List<ItemStack> itemsToBeSorted = [];
        List<ItemStack> originalStacks = [];

        foreach (ItemSlot slot in sortableSlots)
        {
            originalStacks.Add(slot.Empty ? null : slot.Itemstack.Clone());
            if (!slot.Empty)
            {
                itemsToBeSorted.Add(slot.TakeOutWhole());
            }
        }

        if (itemsToBeSorted.Count == 0)
        {
            return;
        }

        MergeStacks(itemsToBeSorted);

        itemsToBeSorted.Sort((left, right) =>
        {
            int collectibleComparison = left.Collectible.Id - right.Collectible.Id;
            return collectibleComparison != 0 ? collectibleComparison : right.StackSize - left.StackSize;
        });

        if (!TryPlaceSortedStacks(itemsToBeSorted, sortableSlots))
        {
            RestoreSlots(sortableSlots, originalStacks);
        }
    }

    private void OnSortRequest(IServerPlayer player, shared.MiddleClickSortPayload payload)
    {
        IInventory inventory = player.InventoryManager.GetInventory(payload.InventoryId);
        if (inventory == null)
        {
            return;
        }

        List<List<ItemSlot>> slotGroups = [];

        int firstSortableSlot = GetFirstSortableSlot(inventory);
        for (int slotIndex = firstSortableSlot; slotIndex < inventory.Count; slotIndex++)
        {
            ItemSlot slot = inventory[slotIndex];
            if (!IsSortableSlot(slot) || (!slot.Empty && !slot.CanTake()))
            {
                continue;
            }

            AddSlotToStorageGroup(slotGroups, slot);
        }

        foreach (List<ItemSlot> slotGroup in slotGroups)
        {
            SortSlotGroup(slotGroup);
        }
    }
}
