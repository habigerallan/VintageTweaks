using Vintagestory.API.Server;

namespace VintageTweaks.Code.Setup
{
    internal sealed class Server
    {
        public Server(ICoreServerAPI sapi, Config.Root cfg)
        {
            _ = new Features.Server.Sort(sapi, cfg.Sort);
            _ = new Features.Server.Crate(sapi, cfg.Crate);
        }
    }
}
