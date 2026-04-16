using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageTweaks.src.shared;

internal sealed class CrateClickState
{
    public BlockPos ClickPos;
    public long ClickTimeMs;
    public CollectibleObject ClickItem;
    public bool ClickComplete;

    public void Reset()
    {
        ClickPos = null;
        ClickTimeMs = 0;
        ClickItem = null;
        ClickComplete = false;
    }
}
