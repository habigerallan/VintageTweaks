using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class QuickStackCrate
{
    private static readonly string[] InventoryLayers =
    [
        GlobalConstants.hotBarInvClassName,
        GlobalConstants.backpackInvClassName
    ];

    private const string CrateInventoryClassName = "crate";
    private const string PushChannelName = "vintagetweaks_crate_push";
    private const string PullChannelName = "vintagetweaks_crate_pull";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.CrateConfig _config;

    public QuickStackCrate(ICoreServerAPI sapi, VintageTweaksSystem.CrateConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(PushChannelName)
            .RegisterMessageType<shared.ClickedCratePayload>()
            .SetMessageHandler<shared.ClickedCratePayload>(OnPushRequest);

        _sapi.Network.RegisterChannel(PullChannelName)
            .RegisterMessageType<shared.ClickedCratePayload>()
            .SetMessageHandler<shared.ClickedCratePayload>(OnPullRequest);
    }

    private InventoryBase GetCrateInventory(shared.ClickedCratePayload payload)
    {
        BlockPos blockPosition = new(payload.X, payload.Y, payload.Z);
        InventoryBase crateInventory = shared.InventoryFromPosition.InventoryFromBlockPos(_sapi, blockPosition);
        return crateInventory != null && crateInventory.ClassName == CrateInventoryClassName ? crateInventory : null;
    }

    private static CollectibleObject GetValidatedItem(InventoryBase crateInventory, shared.ClickedCratePayload payload)
    {
        if (crateInventory.Empty)
        {
            return null;
        }

        CollectibleObject serverSideItem = crateInventory.FirstNonEmptySlot.Itemstack.Collectible;
        return string.Equals(serverSideItem.Code.ToString(), payload.ItemCode, StringComparison.Ordinal)
            ? serverSideItem
            : null;
    }

    private void PushItemsToCrate(IServerPlayer player, CollectibleObject itemToMatch, InventoryBase crateInventory)
    {
        IPlayerInventoryManager inventoryManager = player.InventoryManager;

        foreach (string inventoryLayer in InventoryLayers)
        {
            IInventory playerInventory = inventoryManager.GetOwnInventory(inventoryLayer);
            if (playerInventory == null)
            {
                continue;
            }

            int firstAvailableSlot = inventoryLayer == GlobalConstants.backpackInvClassName ? _config.BackpackSlots : 0;
            for (int slotIndex = firstAvailableSlot; slotIndex < playerInventory.Count; slotIndex++)
            {
                ItemSlot playerSlot = playerInventory[slotIndex];
                if (playerSlot.Empty || playerSlot.Itemstack.Collectible != itemToMatch)
                {
                    continue;
                }

                int itemsToMove = playerSlot.StackSize;
                for (int crateSlotIndex = 0; crateSlotIndex < crateInventory.Count && itemsToMove > 0; crateSlotIndex++)
                {
                    ItemSlot crateSlot = crateInventory[crateSlotIndex];
                    if (!crateSlot.Empty && crateSlot.Itemstack.Collectible != itemToMatch)
                    {
                        continue;
                    }

                    itemsToMove -= playerSlot.TryPutInto(_sapi.World, crateSlot, itemsToMove);
                }
            }
        }
    }

    private void PullItemsFromCrate(IServerPlayer player, InventoryBase crateInventory)
    {
        IPlayerInventoryManager inventoryManager = player.InventoryManager;

        foreach (string inventoryLayer in InventoryLayers)
        {
            IInventory playerInventory = inventoryManager.GetOwnInventory(inventoryLayer);
            if (playerInventory == null)
            {
                continue;
            }

            int firstAvailableSlot = inventoryLayer == GlobalConstants.backpackInvClassName ? _config.BackpackSlots : 0;
            for (int crateSlotIndex = 0; crateSlotIndex < crateInventory.Count; crateSlotIndex++)
            {
                ItemSlot crateSlot = crateInventory[crateSlotIndex];
                if (crateSlot.Empty)
                {
                    continue;
                }

                int itemsToMove = crateSlot.Itemstack.StackSize;
                for (int slotIndex = firstAvailableSlot; slotIndex < playerInventory.Count && itemsToMove > 0; slotIndex++)
                {
                    ItemSlot playerSlot = playerInventory[slotIndex];
                    if (!playerSlot.Empty && playerSlot.Itemstack.Collectible != crateSlot.Itemstack.Collectible)
                    {
                        continue;
                    }

                    itemsToMove -= crateSlot.TryPutInto(_sapi.World, playerSlot, itemsToMove);
                }
            }
        }
    }

    private void OnPushRequest(IServerPlayer player, shared.ClickedCratePayload payload)
    {
        if (!_config.AllowCratePush)
        {
            return;
        }

        InventoryBase crateInventory = GetCrateInventory(payload);
        if (crateInventory == null)
        {
            return;
        }

        CollectibleObject serverSideItem = GetValidatedItem(crateInventory, payload);
        if (serverSideItem == null)
        {
            return;
        }

        PushItemsToCrate(player, serverSideItem, crateInventory);
    }

    private void OnPullRequest(IServerPlayer player, shared.ClickedCratePayload payload)
    {
        if (!_config.AllowCratePull)
        {
            return;
        }

        InventoryBase crateInventory = GetCrateInventory(payload);
        if (crateInventory == null || GetValidatedItem(crateInventory, payload) == null)
        {
            return;
        }

        PullItemsFromCrate(player, crateInventory);
    }
}
