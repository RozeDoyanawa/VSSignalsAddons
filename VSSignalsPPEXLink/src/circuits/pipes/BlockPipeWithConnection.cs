using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VSSignalsPPEXLink.circuits.pipes;

public class BlockPipeWithConnection : BlockPipe, IHangingWireAnchor{
    protected WireAnchor[] wireAnchors;
    protected SelectionKnob[] selectors;
    
    public BlockPipeWithConnection(): base()
    {
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        wireAnchors = new WireAnchor[0];
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
        selectors = new SelectionKnob[0];
        JsonObject[] jsonObjKnobs = Attributes?["selectionKnobs"]?.AsArray();
        if (jsonObjKnobs != null)
        {
            try
            {
                selectors = new SelectionKnob[jsonObjKnobs.Length];
                for (int i = 0; i < jsonObjKnobs.Length; i++)
                {
                    selectors[i] = jsonObjKnobs[i].AsObject<SelectionKnob>();
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


    public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
    {
        base.OnBlockRemoved(world, pos);
        HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
        mod.RemoveAllNodesAtBlockPos(pos);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
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
                    BlockEntityPipe pipe = world.BlockAccessor.GetBlockEntity<BlockEntityPipe>(blockSel.Position);

                    if (pipe is IBlockEntityKnobInteractable beki)
                    {
                        beki.OnKnobInteraction(api, nb.Index);
                        return true; 
                    }
                }
            }
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
    
    Vec3i GetChunkPos(BlockPos pos)
    {
        int cx = (int)Math.Floor((double)pos.X / GlobalConstants.ChunkSize);
        int cy = (int)Math.Floor((double)pos.Y / GlobalConstants.ChunkSize);
        int cz = (int)Math.Floor((double)pos.Z / GlobalConstants.ChunkSize);
        return new Vec3i(cx, cy, cz);
    }

    public void ForceWireRenderUpdate(ICoreAPI Api, BlockPos pos)
    {
        try
        {
            HangingWiresMod mod = Api.ModLoader.GetModSystem<HangingWiresMod>();
            HashSet<Vec3i> pendingPartialChunks = new HashSet<Vec3i>();
            pendingPartialChunks.Add(GetChunkPos(pos));
            HangingWiresRenderer renderer = mod.Renderer;
            var q = renderer.GetType();
            MethodInfo? rebuildMethod = q.GetMethod("BeginMeshRebuild", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); 
            rebuildMethod?.Invoke(renderer, [mod.data, false, pendingPartialChunks, null]);


            //renderer.BeginMeshRebuild(data, false, pendingPartialChunks);
        }
        catch (Exception ex)
        {
            Api.Logger.Error($"[Signals Patch] Reflection failed to broadcast wire packet: {ex.Message}");
        }
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
    #endregion
}