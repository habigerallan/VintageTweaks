using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageTweaks.src.shared;

internal static class InventoryFromPosition
{
    public static InventoryBase InventoryFromBlockPos(ICoreAPI api, BlockPos blockPos)
    {
        BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(blockPos);
        if (blockEntity == null)
        {
            return null;
        }

        PropertyInfo inventoryProperty = blockEntity.GetType().GetProperty("Inventory");
        if (inventoryProperty == null)
        {
            return null;
        }

        return inventoryProperty.GetValue(blockEntity) as InventoryBase;
    }
}
