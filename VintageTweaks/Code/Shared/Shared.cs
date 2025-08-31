using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageTweaks.Code.Shared
{
    public sealed class Functions
    {
        public static InventoryBase InventoryFromBlockPos(ICoreAPI api, BlockPos blockPos)
        {
            BlockEntity block = api.World.BlockAccessor.GetBlockEntity(blockPos);
            if (block == null) return null;

            PropertyInfo inventoryProp = block.GetType().GetProperty("Inventory");
            if (inventoryProp == null) return null;

            return (InventoryBase)inventoryProp.GetValue(block);
        }
    }
    public sealed class CrateClickObject
    {
        public BlockPos ClickPos;
        public long ClickTime;
        public CollectibleObject ClickItem;
        public bool ClickComplete;

        public CrateClickObject()
        {
            ClickPos = null;
            ClickTime = 0;
            ClickItem = null;
            ClickComplete = false;
        }

        public CrateClickObject(BlockPos clickPos, long clickTime, CollectibleObject clickItem, bool clickComplete)
        {
            ClickPos = clickPos;
            ClickTime = clickTime;
            ClickItem = clickItem;
            ClickComplete = clickComplete;
        }
    }
}
