using Vintagestory.API.Client;
using VintageTweaks.Config;
using VintageTweaks.Crate;
using VintageTweaks.Sort;
using VintageTweaks.Zoom;

namespace VintageTweaks.Client
{
    internal sealed partial class VintageTweaksClient
    {
        private readonly VintageTweaksSort _sortFeature;
        private readonly VintageTweaksZoom _zoomFeature;
        private readonly VintageTweaksCrate _crateFeature;

        public VintageTweaksClient(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _sortFeature = new VintageTweaksSort(capi, cfg);
            _zoomFeature = new VintageTweaksZoom(capi, cfg);
            _crateFeature = new VintageTweaksCrate(capi, cfg);
        }

        public void Dispose()
        {
            _sortFeature?.Dispose();
            _zoomFeature?.Dispose();
            _crateFeature?.Dispose();
        }
    }
}


