using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class MiddleClickClearPayload
{
    [ProtoMember(1)]
    public string InventoryId = string.Empty;

    [ProtoMember(2)]
    public int SlotId;

    public MiddleClickClearPayload()
    {
    }

    public MiddleClickClearPayload(string inventoryId, int slotId)
    {
        InventoryId = inventoryId ?? string.Empty;
        SlotId = slotId;
    }
}
