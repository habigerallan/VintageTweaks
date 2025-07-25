using ProtoBuf;
using Vintagestory.API.Common;

namespace VintageTweaks.Classes
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class MiddleClickRequest
    {
        public string InvId;
        public string InvClass;

        public MiddleClickRequest() { }

        public MiddleClickRequest(string invId, string invClass)
        {
            InvId = invId;
            InvClass = invClass;
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CratePushRequest
    {
        public int X;
        public int Y;
        public int Z;
        public string ItemCode;
    }
}
