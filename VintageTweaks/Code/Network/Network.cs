using ProtoBuf;

namespace VintageTweaks.Code.Network
{
    [ProtoContract]
    public sealed class MiddleClickRequest
    {
        [ProtoMember(1)] public string InvId = "";
        [ProtoMember(2)] public string InvClass = "";

        public MiddleClickRequest() { }

        public MiddleClickRequest(string invId, string invClass)
        {
            InvId = invId ?? "";
            InvClass = invClass ?? "";
        }
    }

    [ProtoContract]
    public sealed class CrateRequest
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;
        [ProtoMember(3)] public int Z;
        [ProtoMember(4)] public string ItemCode = "";

        public CrateRequest() { }

        public CrateRequest(int x, int y, int z, string itemCode)
        {
            X = x;
            Y = y;
            Z = z;
            ItemCode = itemCode ?? "";
        }
    }
}
