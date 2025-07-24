using ProtoBuf;

namespace VintageTweaks
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
}
