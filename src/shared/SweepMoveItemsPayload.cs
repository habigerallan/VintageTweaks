using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class SweepMoveItemsPayload
{
    [ProtoMember(1)]
    public string SourceInventoryId = string.Empty;

    [ProtoMember(2)]
    public int SourceSlotId;

    [ProtoMember(3)]
    public byte[] ItemStackData = [];

    public SweepMoveItemsPayload()
    {
    }

    public SweepMoveItemsPayload(string sourceInventoryId, int sourceSlotId, byte[] itemStackData)
    {
        SourceInventoryId = sourceInventoryId ?? string.Empty;
        SourceSlotId = sourceSlotId;
        ItemStackData = itemStackData ?? [];
    }
}
