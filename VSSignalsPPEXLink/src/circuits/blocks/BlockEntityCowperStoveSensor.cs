using System.Text;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.Helpers;
using signals.src.signalNetwork;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using VSSignalsPPEXLink.assets.vssignalsppexlink.gui;
using VSSignalsPPEXLink.circuits.pipes;
using VSSignalsPPEXLink.packets;

namespace VSSignalsPPEXLink.circuits.blocks;

[BlockEntityRegister]
public class BlockEntityCowperStoveSensor : BlockEntity, IBESignalReceptor, IBlockEntityKnobInteractable {
    
    
    private BlockEntityTemperatureSensorRenderer? _renderer;
    public MultiTextureMeshRef? BaseMeshRef;
    public MultiTextureMeshRef? GlowyPartMeshRef;
  
    public const float MinSetTemperature = 0f;
    public float MaxSetTemperature => 2000;
  
    private float _setTemperature = 1f;

    private float _cachedTemperature = 0;
  
    GuiDialogBlockEntityNumberValueInput? _editDialog;
    private BEBehaviorSignalNodeProvider? _nodeProvider;
  
    private NodePos? _alarmSourceNode;
    private NodePos? _levelSourceNode;
  
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
    
    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        
        if (api is ICoreClientAPI capi)
        {
            _renderer = new BlockEntityTemperatureSensorRenderer(capi, this);
            UpdateMeshRefs();
        }
        
        _alarmSourceNode = new NodePos(Pos, 0);
        _levelSourceNode = new NodePos(Pos, 1);
        _nodeProvider = GetBehavior<BEBehaviorSignalNodeProvider>();
        
        RegisterGameTickListener(OnSlowServerTick, 1000);
    }

    private void OnSlowServerTick(IWorldAccessor world, BlockPos blockPos, float arg3) {
        Block currentBlock = world.BlockAccessor.GetBlock(blockPos);
        string orientation = currentBlock.Variant["side"]; 
        
        BlockFacing facing = BlockFacing.FromCode(orientation) ?? BlockFacing.DOWN;

        // 3. Find the local "below" direction by inverting the orientation vector
        // If the block faces UP, its local "below" is DOWN. If it faces NORTH, its local "below" is SOUTH.
        BlockFacing localBelowFacing = facing.Opposite;

        // 4. Calculate the target position 2 blocks away
        BlockPos targetPos = blockPos.AddCopy(
            facing.Normali.X * 2,
            facing.Normali.Y * 2,
            facing.Normali.Z * 2
        );

        // 5. Fetch the target block
        //Block targetBlock = world.BlockAccessor.GetBlock(targetPos);
        
        BlockEntityHeatSink be = world.BlockAccessor.GetBlockEntity<BlockEntityHeatSink>(targetPos);
        if (be != null) {
            _cachedTemperature = be.Temperature;
        } else {
            _cachedTemperature = 0;
        }

        UpdateSignals();
    }

    public void OnValueChanged(NodePos pos, byte value) {
        throw new System.NotImplementedException();
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
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/block/sensor-temperature-base.json"), out MeshData meshdata, texSource, rotation);
      BaseMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
    {
      capi.Tesselator.TesselateShape("BlockEntitySignalValve", Api.Assets.Get<Shape>("vssignalsppexlink:shapes/block/sensor-temperature-glowy.json"), out MeshData meshdata, texSource, rotation);
      GlowyPartMeshRef = capi.Render.UploadMultiTextureMesh(meshdata);
    }
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
        return _cachedTemperature; 
    }

    private void UpdateSignals() {
        var temp = GetTemperature();
        _nodeProvider?.UpdateSource(_levelSourceNode,  (byte)(temp / MaxSetTemperature * 15f));
        _nodeProvider?.UpdateSource(_alarmSourceNode, (byte)(temp > _setTemperature?15:0));
    }

    public float GetMaxTemperature() {
        return MaxSetTemperature;
    }
}