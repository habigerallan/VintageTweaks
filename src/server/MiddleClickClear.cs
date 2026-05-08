using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageTweaks.src.server;

internal sealed class MiddleClickClear
{
    private static readonly string[] PlayerInventoryLayers =
    [
        GlobalConstants.backpackInvClassName,
        GlobalConstants.hotBarInvClassName
    ];

    private const string ClearChannelName = "vintagetweaks_clear";

    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.MiddleClickClearConfig _config;

    public MiddleClickClear(ICoreServerAPI sapi, VintageTweaksSystem.MiddleClickClearConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.Network.RegisterChannel(ClearChannelName)
            .RegisterMessageType<shared.MiddleClickClearPayload>()
            .SetMessageHandler<shared.MiddleClickClearPayload>(OnClearRequest);
    }

    private static bool IsCraftingGrid(IInventory inventory)
    {
        return inventory?.ClassName == GlobalConstants.craftingInvClassName;
    }

    private int GetFirstTargetSlot(IInventory inventory)
    {
        return inventory.ClassName == GlobalConstants.backpackInvClassName ? _config.BackpackSlots : 0;
    }

    private bool ValidateCraftingInventory(
        IServerPlayer player,
        shared.MiddleClickClearPayload payload,
        out InventoryBase craftingInventory
    )
    {
        craftingInventory = null;
        if (string.IsNullOrWhiteSpace(payload.InventoryId))
        {
            return false;
        }

        if (!player.InventoryManager.GetInventory(payload.InventoryId, out craftingInventory))
        {
            return false;
        }

        return craftingInventory != null
            && player.InventoryManager.HasInventory(craftingInventory)
            && IsCraftingGrid(craftingInventory)
            && payload.SlotId >= 0
            && payload.SlotId < craftingInventory.Count
            && craftingInventory[payload.SlotId] is ItemSlotOutput;
    }

    private void TryTransferSlotToInventory(ItemSlot sourceSlot, IInventory targetInventory)
    {
        TryTransferSlotToTargetSlots(sourceSlot, targetInventory, false);
        TryTransferSlotToTargetSlots(sourceSlot, targetInventory, true);
    }

    private void TryTransferSlotToTargetSlots(ItemSlot sourceSlot, IInventory targetInventory, bool allowEmptyTargets)
    {
        if (targetInventory == null)
        {
            return;
        }

        int firstTargetSlot = GetFirstTargetSlot(targetInventory);
        for (int slotIndex = firstTargetSlot; slotIndex < targetInventory.Count && !sourceSlot.Empty; slotIndex++)
        {
            ItemSlot targetSlot = targetInventory[slotIndex];
            if (targetSlot == null || targetSlot.Empty != allowEmptyTargets)
            {
                continue;
            }

            ItemMoveRules.TryPutInto(_sapi.World, sourceSlot, targetSlot, sourceSlot.StackSize);
        }
    }

    private void TryTransferAway(IServerPlayer player, ItemSlot sourceSlot)
    {
        foreach (string inventoryLayer in PlayerInventoryLayers)
        {
            if (sourceSlot.Empty)
            {
                return;
            }

            TryTransferSlotToInventory(
                sourceSlot,
                player.InventoryManager.GetOwnInventory(inventoryLayer)
            );
        }

        if (!sourceSlot.Empty)
        {
            sourceSlot.MarkDirty();
        }
    }

    private void OnClearRequest(IServerPlayer player, shared.MiddleClickClearPayload payload)
    {
        if (!_config.AllowMiddleClickClear)
        {
            return;
        }

        if (!ValidateCraftingInventory(player, payload, out InventoryBase craftingInventory))
        {
            return;
        }

        for (int slotIndex = 0; slotIndex < craftingInventory.Count; slotIndex++)
        {
            ItemSlot slot = craftingInventory[slotIndex];
            if (slot == null || slot.Empty || slot is ItemSlotOutput || !slot.CanTake())
            {
                continue;
            }

            TryTransferAway(player, slot);
        }
    }
}
