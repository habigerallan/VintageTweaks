using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using VintageTweaks.Config;
using VintageTweaks.Crate;
using VintageTweaks.Sort;

namespace VintageTweaks.Server
{
    internal sealed class VintageTweaksServer
    {
        private readonly VintageTweaksSort _sortFeature;
        private readonly VintageTweaksCrate _crateFeature;

        public VintageTweaksServer(ICoreServerAPI sapi, VintageTweaksConfig cfg)
        {
            _sortFeature = new VintageTweaksSort(sapi, cfg);
            _crateFeature = new VintageTweaksCrate(sapi, cfg);
        }
    }
}
