using Vintagestory.API.Common;

namespace VintageTweaks.src.server;

internal static class ItemMoveRules
{
    public static bool CanMoveInto(ItemSlot sourceSlot, ItemSlot targetSlot)
    {
        if (sourceSlot == null
            || targetSlot == null
            || sourceSlot == targetSlot
            || sourceSlot.Empty
            || !sourceSlot.CanTake()
            || !targetSlot.CanTakeFrom(sourceSlot, EnumMergePriority.AutoMerge))
        {
            return false;
        }

        InventoryBase targetInventory = targetSlot.Inventory;
        return targetInventory == null || targetInventory.CanContain(targetSlot, sourceSlot);
    }

    public static int TryPutInto(IWorldAccessor world, ItemSlot sourceSlot, ItemSlot targetSlot, int quantity)
    {
        return quantity > 0 && CanMoveInto(sourceSlot, targetSlot)
            ? sourceSlot.TryPutInto(world, targetSlot, quantity)
            : 0;
    }
}
