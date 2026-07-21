using System;
using signals.src.signalNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace VSSignalsAddons.circuits.signalNetwork;

class BEContactor : BlockEntity, IBESignalReceptor
{
    public bool state = false;

    public override void Initialize(ICoreAPI api)
    {
        Block block = this.Block as Block;
        state = block.LastCodePart() == "on";
        base.Initialize(api);
    }
    
    private void SwapBlock(bool is_on){
        AssetLocation newCode;
        Block block = this.Block;
        try {
            if(is_on){
                newCode = block.CodeWithVariant("powered", "on");
            }else{
                newCode = block.CodeWithVariant("powered", "off");
            }
            Block newBlock = this.Api.World.BlockAccessor.GetBlock(newCode);
            this.Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, this.Pos);
            this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos);
        }
        catch (Exception){
            this.Api.Logger.Debug("Can't swap actuator block");
        };
    }
    
    public void OnValueChanged(NodePos pos, byte value)
    {
        if(pos.index != 0) return;
        state = value >= 1;
        SwapBlock(state);
        BEBehaviorSignalContactor contactor = GetBehavior<BEBehaviorSignalContactor>();
        contactor?.commute(state);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        state = tree.GetBool("state");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetBool("state", state);
    }
}