using System.Text;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.Helpers;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using VSSignalsPPEXLink.assets.vssignalsppexlink.gui;
using VSSignalsPPEXLink.circuits.blocks;
using VSSignalsPPEXLink.packets;

namespace VSSignalsPPEXLink.circuits.pipes;

/// <summary>
/// A manually-toggled in-line valve. Open, it is a normal pipe node and the run flows straight
/// through it as one network; closed, it severs the run at its own cell
/// (<see cref="IsConnectionBroken"/>), splitting it in two. Toggling re-walks the graph. The
/// state is shown by holding the shape's <c>open</c> animation pose.
/// </summary>
[BlockEntityRegister]
public class BlockEntityPipeSensorTemperature : BlockEntityPipe, IBlockEntityKnobInteractable
{
  
  private BlockEntityPipeSensorTemperatureRenderer? renderer;
  public MultiTextureMeshRef? BaseMeshRef;
  public MultiTextureMeshRef? GlowyPartMeshRef;
  
  public const float MinSetTemperature = 0f;
  public float MaxSetTemperature => 2000;
  
  private float _setTemperature = 1f;
  
  GuiDialogBlockEntityNumberValueInput? _editDialog;
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
          _editDialog = new GuiDialogBlockEntityNumberValueInput(Lang.Get("vssignalsppexlink:edit-switchover-temperature"), Pos, _setTemperature.ToString(), Api as ICoreClientAPI, _setTemperature);
          //editDialog.OnTextChanged = DidChangeValueClientSide;
          _editDialog.TryOpen();
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
      renderer = new BlockEntityPipeSensorTemperatureRenderer(capi, this);
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
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-sensor-temperature-base.json"), out MeshData meshdata, texSource, rotation);
      BaseMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-sensor-temperature-glowy.json"), out MeshData meshdata, texSource, rotation);
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

  public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data) {
    if (packetid == 1000) {
      SetDeviceSingleValuePacket editSignPacket = SerializerUtil.Deserialize<SetDeviceSingleValuePacket>(data);
      _setTemperature = float.Clamp(editSignPacket.value, MinSetTemperature, MaxSetTemperature);
      
      MarkDirty(true);

      // Tell server to save this chunk to disk again
      Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
    }
    base.OnReceivedClientPacket(fromPlayer, packetid, data);
  }

  public override void OnExchanged(Block block) {
    base.OnExchanged(block);

    if (Api is ICoreClientAPI) {
      UpdateMeshRefs();
    }
  }
  

  #endregion

  
  #region HUD


  public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
  {
    dsc.AppendLine(
      Lang.Get(
        "vssignalsppexlink:gastemperaturesensor-info-rating",
        ExMeasure.Temperature(_setTemperature)
      )
    );
  }

  #endregion

  public override void OnBlockRemoved()
  {
    base.OnBlockRemoved();
        
    // Clean up memory to avoid leaks
    renderer?.Dispose();

    // BaseMeshRef is the engine default mesh, we don't need to dispose it
    GlowyPartMeshRef?.Dispose();
    BaseMeshRef?.Dispose();
  }

  public override void OnBlockUnloaded() {
    base.OnBlockUnloaded();
    
    // Clean up memory to avoid leaks
    renderer?.Dispose();

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
    tree.SetFloat("temperatureSetting", _setTemperature);
  }

  public override void FromTreeAttributes(
    ITreeAttribute tree,
    IWorldAccessor worldForResolving
  )
  {
    base.FromTreeAttributes(tree, worldForResolving);
    // Pre-existing valves saved before the gate was configurable default to 1 atm.
    _setTemperature = tree.GetFloat("temperatureSetting", 1f);
    UpdateSignals();
  }

  public float GetTemperature() {
    return Temperature;
  }

  private void UpdateSignals() {
    _nodeProvider?.UpdateSource(_levelSourceNode,  (byte)(Temperature / MaxSetTemperature * 15f));
    _nodeProvider?.UpdateSource(_alarmSourceNode, (byte)(Temperature > _setTemperature?15:0));
  }

  public override void OnNetworkUpdate(object? state) {
    base.OnNetworkUpdate(state);
    UpdateSignals();
  }

  public float GetMaxTemperature() {
    return MaxSetTemperature;
  }
}
