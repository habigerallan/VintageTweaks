using ProtoBuf;

namespace VintageTweaks.src.shared;

[ProtoContract]
public sealed class ClickedCratePayload
{
    [ProtoMember(1)]
    public int X;

    [ProtoMember(2)]
    public int Y;

    [ProtoMember(3)]
    public int Z;

    [ProtoMember(4)]
    public string ItemCode = string.Empty;

    public ClickedCratePayload()
    {
    }

    public ClickedCratePayload(int x, int y, int z, string itemCode)
    {
        X = x;
        Y = y;
        Z = z;
        ItemCode = itemCode ?? string.Empty;
    }
}
