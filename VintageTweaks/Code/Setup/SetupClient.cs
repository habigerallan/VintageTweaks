using Vintagestory.API.Client;

namespace VintageTweaks.Code.Setup
{
    internal sealed partial class Client(ICoreClientAPI capi, Config.Root cfg)
    {
        private readonly Features.Client.Sort _sortFeature = new(capi, cfg.Sort);
        private readonly Features.Client.Zoom _zoomFeature = new(capi, cfg.Zoom);
        private readonly Features.Client.Crate _crateFeature = new(capi, cfg.Crate);

        public void Dispose()
        {
            _sortFeature?.Dispose();
            _zoomFeature?.Dispose();
            _crateFeature?.Dispose();
        }
    }
}


