using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class ClearHandsPayload
{
    [ProtoMember(1)]
    public int SourceSlotId;

    [ProtoMember(2)]
    public byte[] ItemStackData = [];

    public ClearHandsPayload()
    {
    }

    public ClearHandsPayload(int sourceSlotId, byte[] itemStackData)
    {
        SourceSlotId = sourceSlotId;
        ItemStackData = itemStackData ?? [];
    }
}
