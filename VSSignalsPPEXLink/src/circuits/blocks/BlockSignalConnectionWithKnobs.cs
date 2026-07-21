using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Registries.Entities;
using HarmonyLib;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using VSSignalsPPEXLink.circuits.pipes;

namespace VSSignalsPPEXLink.circuits.blocks;

[BlockRegister]
public class BlockSignalConnectionWithKnobs : Block, IHangingWireAnchor {
    protected SelectionKnob[] selectors;
    protected WireAnchor[] wireAnchors;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        
        wireAnchors = [];
        JsonObject[] jsonObj = Attributes?["signalNodes"]?.AsArray();
        if (jsonObj != null)
        {
            try
            {
                wireAnchors = new WireAnchor[jsonObj.Length];
                for (int i = 0; i < jsonObj.Length; i++)
                {
                    wireAnchors[i] = jsonObj[i].AsObject<WireAnchor>();
                }
            }
            catch (Exception e)
            {
                api.World.Logger.Error("Failed loading SignalNodes for item/block {0}. Will ignore. Exception: {1}", Code, e);
                wireAnchors = [];
            }
        }
        selectors = [];
        JsonObject[]? jsonObjKnobs = Attributes?["selectionKnobs"]?.AsArray();
        if (jsonObjKnobs != null)
        {
            try
            {
                selectors = new SelectionKnob[jsonObjKnobs.Length];
                for (int i = 0; i < jsonObjKnobs.Length; i++) {
                    selectors[i] = jsonObjKnobs[i].AsObject<SelectionKnob>() ?? throw new  Exception("Can't read knob");
                }
            }
            catch (Exception e)
            {
                api.World.Logger.Error("Failed loading Selection Knob for item/block {0}. Will ignore. Exception: {1}", Code, e);
                selectors = [];
            }
        }
    }


    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
    {
        List<Cuboidf> boxes = new List<Cuboidf>();
        foreach (WireAnchor nb in wireAnchors)
        {
            boxes.Add(nb.RotatedCopy());
        }

        foreach (SelectionKnob nb in selectors) {
            boxes.Add(nb.RotatedCopy());
        }
        boxes.AddRange(base.GetSelectionBoxes(world, pos));
        return boxes.ToArray();
    }

    public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
    {
        return true;
    }
    
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        PlacingWiresMod mod = api.ModLoader.GetModSystem<PlacingWiresMod>();
        if (mod == null)
        {
            api.Logger.Error("PlacingWiresMod mod system not found");
        }
        else
        {
            NodePos pos = GetNodePosForWire(world, blockSel, mod.GetPendingNode());
            if (pos != null)
            {
                if (CanAttachWire(world, pos, mod.GetPendingNode()))
                {
                    mod.ConnectWire(pos, byPlayer, this);
                    return false;
                }
            }
        }

        if (byPlayer.Entity.RightHandItemSlot.Empty) {
            foreach (SelectionKnob nb in selectors) {
                if (nb.Index + wireAnchors.Length == blockSel.SelectionBoxIndex) {
                    BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);

                    if (be is IBlockEntityKnobInteractable beki)
                    {
                        beki.OnKnobInteraction(api, nb.Index);
                        return true; 
                    }
                }
            }
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
    
    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        string info = base.GetPlacedBlockInfo(world, pos, forPlayer);
        BlockSelection sel = forPlayer.Entity.BlockSelection;
        //NodePos nodepos = this.GetNodePosForWire(world, sel);
        //if (!(nodepos == null)){info += nodepos?.ToString() + "\r\n";}
        if (sel != null) {
            string? name = GetAnchorName(world, sel);
            if (name != null) {
                info += "\r\n" + Lang.Get("vssignalsppexlink:connection-name-" + name) + "\r\n";
            }
        }
        return info;
    }
    
    #region Wire anchor
    public Vec3f GetAnchorPosInBlock(NodePos pos)
    {
        foreach (WireAnchor box in wireAnchors)
        {
            Cuboidf cube =  box.RotatedCopy();
            Vec3f position = new Vec3f(cube.MidX, cube.MidY, cube.MidZ);
            if (box.Index == pos.index) return position;
        }
        return new Vec3f(0f, 0f, 0f);
    }

    public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
    {
        base.OnBlockRemoved(world, pos);
        HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
        mod.RemoveAllNodesAtBlockPos(pos);
    }
    
    public string? GetAnchorName(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null)
    {
        foreach (WireAnchor box in wireAnchors)
        {
            if (box.Index == blockSel.SelectionBoxIndex) return box.Name ?? "con-unamed";
        }
        return null;
    }

    public NodePos GetNodePosForWire(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null)
    {
        foreach (WireAnchor box in wireAnchors)
        {
            if (box.Index == blockSel.SelectionBoxIndex) return new NodePos(blockSel.Position, blockSel.SelectionBoxIndex);
        }
        return null;
    }

    public bool CanAttachWire(IWorldAccessor world, NodePos pos, NodePos posInit = null)
    {
        return true;
    }

    public NodePos[] GetWireAnchors(IWorldAccessor world, BlockPos pos)
    {
        NodePos[] nodes = new NodePos[wireAnchors.Length];
        for(int i=0;i<wireAnchors.Length;i++)
        {
            nodes[i] = new NodePos(pos, wireAnchors[i].Index);
        }
        return nodes;
    }
    
    #endregion
    
    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection selection,
        IPlayer forPlayer
    ){
        var q = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        var p = world.BlockAccessor.GetBlockEntity<BlockEntityPipeSensor>(selection.Position);
        if (p != null) {
            q.AddRangeToArray(p.GetEntityInteractionHelp(world, selection, forPlayer));
        }

        return q.ToArray();
    }
    
}