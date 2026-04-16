using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class SweepLeaveItemPayload
{
    [ProtoMember(1)]
    public string TargetInventoryId = string.Empty;

    [ProtoMember(2)]
    public int TargetSlotId;

    public SweepLeaveItemPayload()
    {
    }

    public SweepLeaveItemPayload(string targetInventoryId, int targetSlotId)
    {
        TargetInventoryId = targetInventoryId ?? string.Empty;
        TargetSlotId = targetSlotId;
    }
}
