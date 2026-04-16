using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageTweaks.src;

public sealed class VintageTweaksSystem : ModSystem
{
    public sealed class ModConfig
    {
        public CrateConfig Crate = new();
        public QuickMoveConfig QuickMove = new();
        public SortConfig Sort = new();
        public SweepLeaveConfig SweepLeave = new();
        public SweepMoveConfig SweepMove = new();
        public ZoomConfig Zoom = new();
        public WaypointConfig Waypoint = new();

        public void EnsureDefaults()
        {
            Crate ??= new CrateConfig();
            QuickMove ??= new QuickMoveConfig();
            Sort ??= new SortConfig();
            SweepLeave ??= new SweepLeaveConfig();
            SweepMove ??= new SweepMoveConfig();
            Zoom ??= new ZoomConfig();
            Waypoint ??= new WaypointConfig();
        }
    }

    public sealed class CrateConfig
    {
        public bool AllowCratePush = true;
        public bool AllowCratePull = true;
        public int CrateDelayMs = 300;
        public int BackpackSlots = 4;
    }

    public sealed class QuickMoveConfig
    {
        public bool AllowQuickMoveItem = true;
        public int QuickMoveDelayMs = 300;
        public int BackpackSlots = 4;
    }

    public sealed class SortConfig
    {
        public bool AllowMiddleClickSort = true;
        public int BackpackSlots = 4;
    }

    public sealed class SweepMoveConfig
    {
        public bool AllowSweepMoveItems = true;
        public int BackpackSlots = 4;
    }

    public sealed class SweepLeaveConfig
    {
        public bool AllowSweepLeaveItem = true;
        public int BackpackSlots = 4;
    }

    public sealed class ZoomConfig
    {
        public bool AllowZoom = true;
        public int ZoomPercent = 80;
        public float ZoomSpeed = 0.2f;
        public GlKeys ZoomKey = GlKeys.Z;
    }

    public sealed class WaypointConfig
    {
        public bool AllowSharingWaypoints = true;
    }

    public static ModConfig Config { get; private set; } = new();

    private const string ConfigFileName = "VintageTweaksConfig.json";

    private client.MiddleClickSort? _middleClickSortClient = null!;
    private client.QuickMoveItem? _quickMoveItemClient = null!;
    private client.QuickStackCrate? _quickStackCrateClient = null!;
    private client.SweepLeaveItem? _sweepLeaveItemClient = null!;
    private client.SweepMoveItems? _sweepMoveItemsClient = null!;
    private client.SquintingZoom? _squintingZoomClient = null!;

    public override void Start(ICoreAPI api)
    {
        Config = api.LoadModConfig<ModConfig>(ConfigFileName) ?? new ModConfig();
        Config.EnsureDefaults();

        api.StoreModConfig(Config, ConfigFileName);
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        _middleClickSortClient = new client.MiddleClickSort(capi, Config.Sort);
        _quickMoveItemClient = new client.QuickMoveItem(capi, Config.QuickMove);
        _quickStackCrateClient = new client.QuickStackCrate(capi, Config.Crate);
        _sweepLeaveItemClient = new client.SweepLeaveItem(capi, Config.SweepLeave);
        _sweepMoveItemsClient = new client.SweepMoveItems(capi, Config.SweepMove);
        _squintingZoomClient = new client.SquintingZoom(capi, Config.Zoom);
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        _ = new server.MiddleClickSort(sapi, Config.Sort);
        _ = new server.QuickMoveItem(sapi, Config.QuickMove);
        _ = new server.QuickStackCrate(sapi, Config.Crate);
        _ = new server.SweepLeaveItem(sapi, Config.SweepLeave);
        _ = new server.SweepMoveItems(sapi, Config.SweepMove);
        _ = new server.WaypointSharing(sapi, Config.Waypoint);
    }

    public override void Dispose()
    {
        _middleClickSortClient?.Dispose();
        _quickMoveItemClient?.Dispose();
        _quickStackCrateClient?.Dispose();
        _sweepLeaveItemClient?.Dispose();
        _sweepMoveItemsClient?.Dispose();
        _squintingZoomClient?.Dispose();
    }
}
