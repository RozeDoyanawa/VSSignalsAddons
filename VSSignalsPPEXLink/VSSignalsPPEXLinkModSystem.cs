using ExpandedLib.Registries.Commands;
using ExpandedLib.Registries.Entities;
using ProtoBuf;
using signals.src.hangingwires;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

[assembly: ModInfo("VS Signal PPEX Link Mod",
    "vssignalsppexlink",
    Description = "Additional components for the signals mod",
    Website = "",
    Version = "0.1.0",
    Authors = new[] { "Roze" })
]

namespace VSSignalsPPEXLink;

public class VSSignalsPPEXLinkModSystem : ModSystem {
    IServerNetworkChannel serverChannel;
    IClientNetworkChannel clientChannel;
    
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api) {
        Mod.Logger.Notification("Hello from template mod: " + api.Side);
        EntityRegistry.RegisterAll(api, Mod, GetType().Assembly);
        
        //if (api.World is IClientWorldAccessor)
        //{
        //    clientChannel = ((ICoreClientAPI)api).Network.RegisterChannel("vssinglasppexlinkmod")
        //        .RegisterMessageType(typeof(SetDeviceSingleValuePacket));
        //}
        //else
        //{
        //    serverChannel = ((ICoreServerAPI)api).Network.RegisterChannel("vssinglasppexlinkmod")
        //        .RegisterMessageType(typeof(SetDeviceSingleValuePacket))
        //        .SetMessageHandler<SetDeviceSingleValuePacket>(OnSetSingleValueFromClient);
        //}
    }

    public override void StartServerSide(ICoreServerAPI api) {
        Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("vssignalsPPEXlink:hello"));
        CommandRegistry.RegisterAll(api, Mod, GetType().Assembly);
    }

    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("vssignalsPPEXlink:hello"));
    }
    
    //[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    //public class SetDeviceSingleValuePacket {
    //    public BlockPos Pos;
    //    public int value;
    //    public string byPlayer;
    //}
}