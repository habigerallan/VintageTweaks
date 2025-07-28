using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageTweaks.Classes
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class MiddleClickRequest
    {
        public string InvId;
        public string InvClass;

        public MiddleClickRequest()
        {
            InvId = string.Empty;
            InvClass = string.Empty;
        }

        public MiddleClickRequest(string invId, string invClass)
        {
            InvId = invId;
            InvClass = invClass;
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CrateRequest
    {
        public int X;
        public int Y;
        public int Z;
        public string ItemCode;

        public CrateRequest()
        {
            X = 0;
            Y = 0;
            Z = 0;
            ItemCode = string.Empty;
        }

        public CrateRequest(int x, int y, int z, string itemCode)
        {
            X = x;
            Y = y;
            Z = z;
            ItemCode = itemCode;
        }
    }
    public class CrateClickObject
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
