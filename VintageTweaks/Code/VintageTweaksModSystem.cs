using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageTweaks.Code
{
    public sealed class VintageTweaksModSystem : ModSystem
    {
        public static Config.Root Config { get; private set; } = new();

        private const string _configFile = "VintageTweaksConfig.json";
        private Setup.Client _clientSetup;

        public override void Start(ICoreAPI api)
        {
            Config = api.LoadModConfig<Config.Root>(_configFile) ?? new Config.Root();

            Config.Crate ??= new Config.Crate();
            Config.Sort ??= new Config.Sort();
            Config.Zoom ??= new Config.Zoom();

            api.StoreModConfig(Config, _configFile);
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _clientSetup = new Setup.Client(capi, Config);
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            _ = new Setup.Server(sapi, Config);
        }

        public override void Dispose()
        {
            _clientSetup?.Dispose();
        }
    }
}
