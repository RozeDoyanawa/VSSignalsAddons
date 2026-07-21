using System.Text;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VSSignalsPPEXLink.circuits.pipes;

/// <summary>
/// A manually-toggled in-line valve. Open, it is a normal pipe node and the run flows straight
/// through it as one network; closed, it severs the run at its own cell
/// (<see cref="IsConnectionBroken"/>), splitting it in two. Toggling re-walks the graph. The
/// state is shown by holding the shape's <c>open</c> animation pose.
/// </summary>
[BlockEntityRegister]
public class BlockEntitySignalValve : BlockEntityPipe, IBESignalReceptor
{
  private bool _open;

  private BlockEntitySignalValveRenderer? renderer;
  public MultiTextureMeshRef? BaseMeshRef;
  public MultiTextureMeshRef? HandleClosedMeshRef;
  public MultiTextureMeshRef? HandleOpenMeshRef;
  public MultiTextureMeshRef? GlowyPartMeshRef;
  public MultiTextureMeshRef? LidMeshRef;
  

  /// <summary>Whether the valve is currently open (letting the run flow through it).</summary>
  public bool IsOpen() => _open;

  /// <summary>A closed valve severs the run at its cell; open, it is a normal in-line node.</summary>
  public override bool IsConnectionBroken() => !_open;

  #region Lifecycle

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    
    if (api is ICoreClientAPI capi)
    {
      renderer = new BlockEntitySignalValveRenderer(capi, this);
      updateMeshRefs();
    }
    
    if (!_open)
      Pressure = 0f;
  }
  
  void updateMeshRefs()
  {
    if (Api is not ICoreClientAPI capi) return;

    var texSource = capi.Tesselator.GetTextureSource(Block);
    //BaseMeshRef = capi.TesselatorManager.GetDefaultBlockMeshRef(Block);
    float rotX = this.Block.Shape.rotateX;
    float rotY = this.Block.Shape.rotateY;
    float rotZ = this.Block.Shape.rotateZ;
    HandleClosedMeshRef?.Dispose();
    HandleOpenMeshRef?.Dispose();
    LidMeshRef?.Dispose();
    GlowyPartMeshRef?.Dispose();
    BaseMeshRef?.Dispose();
    var rotation = new Vec3f(rotX, rotY, rotZ);
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-signalvalve-base.json"), out MeshData meshdata, texSource, rotation);
      BaseMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-signalvalve-handleoff.json"), out MeshData meshdata, texSource, rotation);
      HandleClosedMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-signalvalve-handleon.json"), out MeshData meshdata, texSource, rotation);
      HandleOpenMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-signalvalve-lid.json"), out MeshData meshdata, texSource, rotation);
      LidMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/pipes/pipe-signalvalve-glow.json"), out MeshData meshdata, texSource, rotation);
      GlowyPartMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
  }
  
  
  /// <summary>
  /// Builds the model→world matrix matching how the chunk tesselator rotates a CompositeShape:
  /// <c>T(centre) · RotateXYZ · T(-centre)</c> about (0.5, 0.5, 0.5), X→Y→Z order. Assigned to
  /// the renderer's <c>CustomTransform</c> so the animated pose lines up in every orientation.
  /// </summary>
  private static float[] BuildShapeRotationTransform(
    float rotXDeg,
    float rotYDeg,
    float rotZDeg
  )
  {
    float[] rotation = Mat4f.Create();
    Mat4f.RotateXYZ(
      rotation,
      rotXDeg * GameMath.DEG2RAD,
      rotYDeg * GameMath.DEG2RAD,
      rotZDeg * GameMath.DEG2RAD
    );

    float[] transform = Mat4f.Create();
    Mat4f.Identity(transform);
    Mat4f.Translate(transform, transform, 0.5f, 0.5f, 0.5f);
    Mat4f.Mul(transform, transform, rotation);
    Mat4f.Translate(transform, transform, -0.5f, -0.5f, -0.5f);
    return transform;
  }

  /// <summary>
  /// A wrench rotates the valve via <c>ExchangeBlock</c>, which keeps this BE alive so
  /// <see cref="Initialize"/> never re-runs and the animator stays bound to the original
  /// orientation. Re-bind the animator to the new block's rotation and restore the pose.
  /// </summary>
  public override void OnExchanged(Block block)
  {
    base.OnExchanged(block);

    if (Api is ICoreClientAPI capi)
    {
      updateMeshRefs();
      (block as BlockPipeWithConnection)?.ForceWireRenderUpdate(capi, Pos);
    }
  }
  
  /// <summary>Server-side toggle of the valve's open state. Re-walks the network so the
  /// change in connectivity (open rejoins the two sides, closed severs them) takes effect
  /// immediately.</summary>
  public void SetState(bool open)
  {
    _open = open;
    
    MarkDirty(true);

    // RemoveNode runs fracture detection (closing splits the run); AddNode re-merges both
    // sides when open, or re-isolates the cell when closed.
    if (
      Api?.Side == EnumAppSide.Server
      && NetworkSystem != null
      && Api.World?.BlockAccessor is { } ba
    )
    {
      NetworkSystem.RemoveNode(ba, Pos);
      NetworkSystem.AddNode(ba, Pos, NetworkType);
    }
    if (!_open)
    {
      Pressure = 0f;
      DiscardNetworkPool();
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

  #endregion

  #region HUD

  public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
  {
    dsc.AppendLine(
      Lang.Get(
        "ppex:valve-state",
        Lang.Get(_open ? "ppex:valve-open" : "ppex:valve-closed")
      )
    );
    // Open, the base pipe info reports what flows through; closed, it reads empty.
    base.GetBlockInfo(forPlayer, dsc);
  }

  #endregion

  #region Serialization

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
    base.ToTreeAttributes(tree);
    tree.SetBool("valveOpen", _open);
  }

  public override void FromTreeAttributes(
    ITreeAttribute tree,
    IWorldAccessor worldForResolving
  )
  {
    base.FromTreeAttributes(tree, worldForResolving);
    bool prev = _open;
    _open = tree.GetBool("valveOpen");
    // Closed at save time → isolated cell holds nothing. Drop any persisted pool before
    // Initialize captures it for restore, so a stale pressurised state can't burst it on load.
    if (!_open)
      DiscardNetworkPool();
    if (Api?.Side == EnumAppSide.Client && prev != _open)
      ApplyValvePose();
  }

  #endregion

  public void OnValueChanged(NodePos pos, byte value) {
    if(pos.index != 0) return;
    bool state = value >= 1;
    SetState(state);
  }
  
  public override void OnBlockRemoved()
  {
    base.OnBlockRemoved();
        
    // Clean up memory to avoid leaks
    renderer?.Dispose();

    // BaseMeshRef is the engine default mesh, we don't need to dispose it
    HandleClosedMeshRef?.Dispose();
    HandleOpenMeshRef?.Dispose();
    LidMeshRef?.Dispose();
    GlowyPartMeshRef?.Dispose();
  }

  public override void OnBlockUnloaded() {
    base.OnBlockUnloaded();
    
    // Clean up memory to avoid leaks
    renderer?.Dispose();

    // BaseMeshRef is the engine default mesh, we don't need to dispose it
    HandleClosedMeshRef?.Dispose();
    HandleOpenMeshRef?.Dispose();
    LidMeshRef?.Dispose();
    GlowyPartMeshRef?.Dispose();
  }

  public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
    //base.OnTesselation(mesher, tessThreadTesselator);
    return true;
  }
}
