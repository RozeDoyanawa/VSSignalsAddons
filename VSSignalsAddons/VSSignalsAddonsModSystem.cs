using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using VSSignalsAddons.circuits.signalNetwork;

[assembly: ModInfo("VS Signals Addons",
    "vssignalsaddons",
    Description = "Additional components for the signals mod",
    Website = "",
    Version = "0.1.0",
    Authors = new[] { "Roze" })
]

namespace VSSignalsAddons;

public class VSSignalsAddonsModSystem : ModSystem {
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    ICoreAPI api;

    IServerNetworkChannel serverChannel;
    IClientNetworkChannel clientChannel;
    
    public override void Start(ICoreAPI api) {
        Mod.Logger.Notification("Hello from template mod: " + api.Side);
        api.RegisterBlockEntityClass("BlockEntityContactor", typeof(BEContactor));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalContactor", typeof(BEBehaviorSignalContactor));
    }

    public override void StartServerSide(ICoreServerAPI api) {
        Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("vssignalsaddons:hello"));
    }

}