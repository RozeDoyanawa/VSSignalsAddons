using signals.src;
using signals.src.signalNetwork;

namespace VSSignalsAddons.circuits.signalNetwork;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

public class BEBehaviorSignalContactor : BEBehaviorSignalConnector
{
    Connection con_no;
    Connection con_nc;
    SignalNetworkMod signalMod;

    private const byte CONTACTOR_ATT_ON = 0;
    private const byte CONTACTOR_ATT_OFF = 15;

    public BEBehaviorSignalContactor(BlockEntity blockentity) : base(blockentity)
    {
    }

    public void commute(bool state){
        if (signalMod.Api.Side == EnumAppSide.Client) {
            return;
        }
        if (state) {
            signalMod.netManager.UpdateConnection(con_nc, CONTACTOR_ATT_OFF, CONTACTOR_ATT_OFF);
            signalMod.netManager.UpdateConnection(con_no, CONTACTOR_ATT_ON, CONTACTOR_ATT_ON);
        } else {
            signalMod.netManager.UpdateConnection(con_no, CONTACTOR_ATT_OFF, CONTACTOR_ATT_OFF);
            signalMod.netManager.UpdateConnection(con_nc, CONTACTOR_ATT_ON, CONTACTOR_ATT_ON);
        }
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
        base.Initialize(api, properties);

        NodePos coil = new NodePos(this.Pos, 0);
        NodePos no_a = new NodePos(this.Pos, 1);
        NodePos no_b = new NodePos(this.Pos, 2);
        NodePos nc_a = new NodePos(this.Pos, 3);
        NodePos nc_b = new NodePos(this.Pos, 4);

        ISignalNode node_coil = GetNodeAt(coil);
        ISignalNode node_no_a = GetNodeAt(no_a);
        ISignalNode node_no_b = GetNodeAt(no_b);
        ISignalNode node_nc_a = GetNodeAt(nc_a);
        ISignalNode node_nc_b = GetNodeAt(nc_b);
        
        api.Logger.VerboseDebug("coil node="+node_coil + ", node_no_a="+node_no_a + ", node_no_b="+node_no_b + ", node_nc_a="+node_nc_a+", node_nc_b="+node_nc_b);

        if (node_coil == null || node_no_a == null || node_no_b == null || node_nc_a == null || node_nc_b == null) {
            return;
        }
        if (signalMod.Api.Side == EnumAppSide.Client) {
            return;
        }
        BEContactor be = this.Blockentity as BEContactor;
        api.Logger.VerboseDebug("BEContactor = " + be);
        if(be == null) return;
        con_no = new Connection(node_no_a, node_no_b, be.state? (byte)0: (byte)15);
        con_nc = new Connection(node_nc_a, node_nc_b, be.state? (byte)15: (byte)0);
        signalMod.netManager.AddConnection(con_no);
        signalMod.netManager.AddConnection(con_nc);
    }
}