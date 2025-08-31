using Vintagestory.API.Client;

namespace VintageTweaks.Code.Config
{
    public sealed class Root
    {
        public Crate Crate = new();
        public Sort Sort = new();
        public Zoom Zoom = new();
    }

    public sealed class Crate
    {
        public bool AllowCratePush = true;
        public bool AllowCratePull = true;
        public int CrateDelayMs = 300;
        public int BackpackSlots = 4;
    }
    public sealed class Sort
    {
        public bool AllowMiddleClickSort = true;
        public int BackpackSlots = 4;
    }
    public sealed class Zoom
    {
        public bool AllowZoom = true;
        public int ZoomPercent = 80;
        public float ZoomSpeed = 0.2F;
        public GlKeys ZoomKey = GlKeys.Z;
    }
}
