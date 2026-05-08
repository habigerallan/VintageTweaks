using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class ClearHands
{
    private const string ChannelName = "vintagetweaks_clear_hands";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.ClearHandsConfig _config;

    public ClearHands(ICoreServerAPI sapi, VintageTweaksSystem.ClearHandsConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<shared.ClearHandsPayload>()
            .SetMessageHandler<shared.ClearHandsPayload>(OnClearHandsRequest);
    }

    private bool MatchesRequestedStack(ItemStack candidate, ItemStack requestedStack)
    {
        return candidate != null
            && requestedStack != null
            && candidate.Equals(_sapi.World, requestedStack, GlobalConstants.IgnoredStackAttributes);
    }

    private ItemStack GetRequestedStack(shared.ClearHandsPayload payload)
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
        shared.ClearHandsPayload payload,
        ItemStack requestedStack,
        out ItemSlot sourceSlot
    )
    {
        sourceSlot = player.InventoryManager.ActiveHotbarSlot;
        InventoryBase sourceInventory = sourceSlot?.Inventory;

        return sourceSlot != null
            && sourceInventory != null
            && sourceInventory.ClassName == GlobalConstants.hotBarInvClassName
            && sourceInventory.GetSlotId(sourceSlot) == payload.SourceSlotId
            && !sourceSlot.Empty
            && sourceSlot.CanTake()
            && MatchesRequestedStack(sourceSlot.Itemstack, requestedStack);
    }

    private List<ItemSlot> GetProtectedBackpackSlots(IInventory targetInventory)
    {
        List<ItemSlot> skipSlots = [];
        int protectedSlots = Math.Min(_config.BackpackSlots, targetInventory.Count);

        for (int slotIndex = 0; slotIndex < protectedSlots; slotIndex++)
        {
            ItemSlot slot = targetInventory[slotIndex];
            if (slot != null)
            {
                skipSlots.Add(slot);
            }
        }

        return skipSlots;
    }

    private int TryTransferToBackpack(IServerPlayer player, ItemSlot sourceSlot)
    {
        IInventory backpackInventory = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
        if (backpackInventory == null)
        {
            return 0;
        }

        int totalMoved = 0;
        List<ItemSlot> skipSlots = GetProtectedBackpackSlots(backpackInventory);

        while (!sourceSlot.Empty && sourceSlot.CanTake())
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

            WeightedSlot targetSlot = backpackInventory.GetBestSuitedSlot(sourceSlot, moveOperation, skipSlots);
            if (targetSlot?.slot == null)
            {
                break;
            }

            int moved = ItemMoveRules.TryPutInto(_sapi.World, sourceSlot, targetSlot.slot, previousStackSize);
            if (moved <= 0)
            {
                skipSlots.Add(targetSlot.slot);
                continue;
            }

            totalMoved += moved;
        }

        return totalMoved;
    }

    private void OnClearHandsRequest(IServerPlayer player, shared.ClearHandsPayload payload)
    {
        if (!_config.AllowClearHands)
        {
            return;
        }

        ItemStack requestedStack = GetRequestedStack(payload);
        if (requestedStack == null)
        {
            return;
        }

        if (!ValidateSourceSlot(player, payload, requestedStack, out ItemSlot sourceSlot))
        {
            return;
        }

        if (TryTransferToBackpack(player, sourceSlot) > 0)
        {
            player.InventoryManager.BroadcastHotbarSlot();
        }
    }
}
