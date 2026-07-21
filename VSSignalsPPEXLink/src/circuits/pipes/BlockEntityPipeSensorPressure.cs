using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Helpers;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using VSSignalsPPEXLink.assets.vssignalsppexlink.gui;
using VSSignalsPPEXLink.packets;

namespace VSSignalsPPEXLink.circuits.pipes;

/// <summary>
/// A manually-toggled in-line valve. Open, it is a normal pipe node and the run flows straight
/// through it as one network; closed, it severs the run at its own cell
/// (<see cref="IsConnectionBroken"/>), splitting it in two. Toggling re-walks the graph. The
/// state is shown by holding the shape's <c>open</c> animation pose.
/// </summary>
[BlockEntityRegister]
public class BlockEntityPipeSensorPressure : BlockEntityPipe, IBlockEntityKnobInteractable
{
  
  private BlockEntityPipeSensorPressureRenderer? _renderer;
  public MultiTextureMeshRef? BaseMeshRef;
  public MultiTextureMeshRef? GlowyPartMeshRef;
  
  public const float MinSetPressure = 0f;
  public float MaxSetPressure =>
    Block is BlockPipe v ? v.BurstPressure : 0f;
  
  private float _setPressure = 1f;

  private GuiDialogBlockEntityNumberValueInput? editDialog;
  private BEBehaviorSignalNodeProvider? _nodeProvider;
  
  private NodePos? _alarmSourceNode;
  private NodePos? _levelSourceNode;
  


  /// <summary>Whether the valve is currently open (letting the run flow through it).</summary>
  
  /// <summary>A closed valve severs the run at its cell; open, it is a normal in-line node.</summary>
  
  #region Lifecycle

  public void OnKnobInteraction(ICoreAPI api, int index) {
    if (api.Side == EnumAppSide.Client) {
      switch (index) {
        case 0: {
          editDialog = new GuiDialogBlockEntityNumberValueInput(Lang.Get("vssignalsppexlink:edit-switchover-pressure"), Pos, _setPressure.ToString(), Api as ICoreClientAPI, _setPressure);
          //editDialog.OnTextChanged = DidChangeValueClientSide;
          editDialog.TryOpen();
          break;
        }
      }
    }
  }

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    
    if (api is ICoreClientAPI capi)
    {
      _renderer = new BlockEntityPipeSensorPressureRenderer(capi, this);
      UpdateMeshRefs();
    }
    _alarmSourceNode = new NodePos(Pos, 0);
    _levelSourceNode = new NodePos(Pos, 1);
    _nodeProvider = GetBehavior<BEBehaviorSignalNodeProvider>();
  }
  
  void UpdateMeshRefs()
  {
    if (Api is not ICoreClientAPI capi) return;

    var texSource = capi.Tesselator.GetTextureSource(Block);
    //BaseMeshRef = capi.TesselatorManager.GetDefaultBlockMeshRef(Block);
    float rotX = this.Block.Shape.rotateX;
    float rotY = this.Block.Shape.rotateY;
    float rotZ = this.Block.Shape.rotateZ;
    BaseMeshRef?.Dispose();
    GlowyPartMeshRef?.Dispose();
    var rotation = new Vec3f(rotX, rotY, rotZ);
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-sensor-pressure-base.json"), out MeshData meshdata, texSource, rotation);
      BaseMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-sensor-pressure-glowy.json"), out MeshData meshdata, texSource, rotation);
      GlowyPartMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
  }
  
  /// <summary>
  /// Drops any cached/persisted network pool. Closing severs the cell without a broadcast, so the
  /// pressurised state it cached while open would otherwise serialise and be restored into the
  /// isolated cell on reload, bursting it. Clearing keeps closed-valve saves empty.
  /// </summary>
  private void DiscardNetworkPool()
  {
    _savedNetworkState = null;
    _networkState = null;
  }

  private void ApplyValvePose()
  {

  }

  public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data) {
    if (packetid == 1000) {
      SetDeviceSingleValuePacket editSignPacket = SerializerUtil.Deserialize<SetDeviceSingleValuePacket>(data);
      _setPressure = float.Clamp(editSignPacket.value, MinSetPressure, MaxSetPressure);
      
      MarkDirty(true);

      // Tell server to save this chunk to disk again
      Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
    }
    base.OnReceivedClientPacket(fromPlayer, packetid, data);
  }

  public override void OnExchanged(Block block) {
    base.OnExchanged(block);

    if (Api is ICoreClientAPI capi) {
      UpdateMeshRefs();
      //var b = block as BlockPipeWithConnection;
      //var wireMod = Api.ModLoader.GetModSystem<HangingWiresMod>();
      //var pos = b.GetPlacedBlockInfo();
      //HashSet<NodePos> anchors = new HashSet<NodePos>(b.GetWireAnchors(Api.World, Pos));
      //foreach (var connection in wireMod.data.connections) {
      //  if (anchors.Contains(connection.pos1) || anchors.Contains(connection.pos2)) {
      //    
      //  }
      //}
      //foreach (var anchor in anchors) {
      //    
      //}
    }
  }
  

  #endregion

  
  #region HUD


  public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
  {
    dsc.AppendLine(
      Lang.Get(
        "vssignalsppexlink:gaspressuresensor-info-rating",
        ExMeasure.PressureRange(_setPressure, MaxSetPressure)
      )
    );
  }

  #endregion

  public override void OnBlockRemoved()
  {
    base.OnBlockRemoved();
        
    // Clean up memory to avoid leaks
    _renderer?.Dispose();

    // BaseMeshRef is the engine default mesh, we don't need to dispose it
    GlowyPartMeshRef?.Dispose();
    BaseMeshRef?.Dispose();
  }

  public override void OnBlockUnloaded() {
    base.OnBlockUnloaded();
    
    // Clean up memory to avoid leaks
    _renderer?.Dispose();

    // BaseMeshRef is the engine default mesh, we don't need to dispose it
    GlowyPartMeshRef?.Dispose();
    BaseMeshRef?.Dispose();
  }

  public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
    return true;
  }

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
    base.ToTreeAttributes(tree);
    tree.SetFloat("pressureSetting", _setPressure);
  }

  public override void FromTreeAttributes(
    ITreeAttribute tree,
    IWorldAccessor worldForResolving
  )
  {
    base.FromTreeAttributes(tree, worldForResolving);
    // Pre-existing valves saved before the gate was configurable default to 1 atm.
    _setPressure = tree.GetFloat("pressureSetting", 1f);
    UpdateSignals();
  }

  public float GetPressure() {
    return Pressure;
  }

  private void UpdateSignals() {
    _nodeProvider?.UpdateSource(_levelSourceNode,  (byte)(Pressure / MaxSetPressure * 15f));
    _nodeProvider?.UpdateSource(_alarmSourceNode, (byte)(Pressure > _setPressure?15:0));
  }

  public override void OnNetworkUpdate(object? state) {
    base.OnNetworkUpdate(state);
    UpdateSignals();
  }

  public float GetMaxPressure() {
    return MaxSetPressure;
  }
}
