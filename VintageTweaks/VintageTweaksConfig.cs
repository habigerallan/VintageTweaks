using Vintagestory.API.Client;

namespace VintageTweaks
{
    public sealed class VintageTweaksConfig
    {
        public bool AllowMiddleClickSort = true;
        public int BackpackSlots = 4;

        public bool AllowZoom = true;
        public int ZoomPercent = 80;
        public float ZoomSpeed = 0.2F;
        public GlKeys ZoomKey = GlKeys.Z;
    }
}
