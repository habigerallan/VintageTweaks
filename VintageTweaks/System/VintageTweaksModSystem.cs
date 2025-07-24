using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VintageTweaks.Client;
using VintageTweaks.Config;
using VintageTweaks.Server;

namespace VintageTweaks.System
{
    public sealed class VintageTweaksModSystem : ModSystem
    {
        public static VintageTweaksConfig config { get; private set; } = new();

        private const string configFile = "VintageTweaksConfig.json";

        private VintageTweaksClient _clientHelper;
        private VintageTweaksServer _serverHelper;

        public override void Start(ICoreAPI api)
        {
            config = api.LoadModConfig<VintageTweaksConfig>(configFile) ?? new VintageTweaksConfig();
            api.StoreModConfig(config, configFile);
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _clientHelper = new VintageTweaksClient(capi, config);
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            _serverHelper = new VintageTweaksServer(sapi, config);
        }

        public override void Dispose()
        {
            _clientHelper?.Dispose();
        }
    }
}
