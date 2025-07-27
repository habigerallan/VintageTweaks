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
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CrateRequest
    {
        public int X;
        public int Y;
        public int Z;
        public string ItemCode;
    }
    public class CrateClickObject
    {
        public BlockPos ClickPos;
        public long ClickTime;
        public CollectibleObject ClickItem;
        public bool ClickComplete;
    }
}
