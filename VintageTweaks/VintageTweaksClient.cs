using Vintagestory.API.Client;

namespace VintageTweaks
{
    internal sealed partial class VintageTweaksClient
    {
        private readonly VintageTweaksSort _sortFeature;
        private readonly VintageTweaksZoom _zoomFeature;

        public VintageTweaksClient(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _sortFeature = new VintageTweaksSort(capi, cfg);
            _zoomFeature = new VintageTweaksZoom(capi, cfg);
        }

        public void Dispose()
        {
            _sortFeature?.Dispose();
            _zoomFeature?.Dispose();
        }
    }
}


