using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class MiddleClickSortPayload
{
    [ProtoMember(1)]
    public string InventoryId = string.Empty;

    public MiddleClickSortPayload()
    {
    }

    public MiddleClickSortPayload(string inventoryId)
    {
        InventoryId = inventoryId ?? string.Empty;
    }
}
