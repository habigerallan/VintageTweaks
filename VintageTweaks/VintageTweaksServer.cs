using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace VintageTweaks
{
    internal sealed class VintageTweaksServer
    {
        private readonly VintageTweaksSort _sortFeature;

        public VintageTweaksServer(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            _sortFeature = new VintageTweaksSort(sapi, cfg);
        }
    }
}
