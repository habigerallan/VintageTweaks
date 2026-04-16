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

    private void OnSortRequest(IServerPlayer player, shared.MiddleClickSortPayload payload)
    {
        IInventory inventory = player.InventoryManager.GetInventory(payload.InventoryId);
        if (inventory == null)
        {
            return;
        }

        List<ItemStack> itemsToBeSorted = [];

        int firstSortableSlot = GetFirstSortableSlot(inventory);
        for (int slotIndex = firstSortableSlot; slotIndex < inventory.Count; slotIndex++)
        {
            ItemSlot slot = inventory[slotIndex];
            if (slot.Empty)
            {
                continue;
            }

            itemsToBeSorted.Add(slot.TakeOutWhole());
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

        int nextSortedStack = 0;
        for (int slotIndex = firstSortableSlot; slotIndex < inventory.Count && nextSortedStack < itemsToBeSorted.Count; slotIndex++)
        {
            inventory[slotIndex].Itemstack = itemsToBeSorted[nextSortedStack++];
            inventory[slotIndex].MarkDirty();
        }
    }
}
