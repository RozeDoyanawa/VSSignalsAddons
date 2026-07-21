using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VSSignalsPPEXLink.circuits.pipes;

public class BlockEntityPipeSensorTemperatureRenderer : IRenderer
{
    private readonly ICoreClientAPI _capi;
    private readonly BlockEntityPipeSensorTemperature _beSensor;
    private readonly Matrixf _modelMat = new();


    // IRenderer requirement: Determines the frequency of your DrawOrder
    public double RenderOrder => 0.5; 
    
    // IRenderer requirement: The render pass you want to hook into
    public int RenderRange => 24; 

    public BlockEntityPipeSensorTemperatureRenderer(ICoreClientAPI capi, BlockEntityPipeSensorTemperature sensor)
    {
        this._capi = capi;
        this._beSensor = sensor;
        
        capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {

        IRenderAPI rpi = _capi.Render;
        Vec3d camPos = _capi.World.Player.Entity.CameraPos;

        Vec4f lightrgbs = _capi.World.BlockAccessor.GetLightRGBs(_beSensor.Pos.X, _beSensor.Pos.Y, _beSensor.Pos.Z);
        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(0);
        
        int extraGlow = (int)(_beSensor.GetTemperature()/_beSensor.GetMaxTemperature()*100f);
        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true);
        
        IStandardShaderProgram prog = rpi.PreparedStandardShader(_beSensor.Pos.X, _beSensor.Pos.Y, _beSensor.Pos.Z);
        
        prog.ModelMatrix = _modelMat
                .Identity()
                .Translate(_beSensor.Pos.X - camPos.X, _beSensor.Pos.Y - camPos.Y, _beSensor.Pos.Z - camPos.Z)
                .Values
            ;
        
        prog.RgbaLightIn = lightrgbs;
        prog.RgbaGlowIn = ColorUtil.ToRGBAVec4f(ColorUtil.HsvToRgb(43, 7, 3)); // new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);
        prog.ExtraGlow = extraGlow;
        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        //prog.TempGlowMode = stack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0;
        prog.TempGlowMode = 0;
        prog.ExtraGlow = extraGlow;
        rpi.RenderMultiTextureMesh(_beSensor.GlowyPartMeshRef, "tex");
        prog.ExtraGlow = 0;
        rpi.RenderMultiTextureMesh(_beSensor.BaseMeshRef, "tex");
        
        prog.Stop();
    }

    public void Dispose()
    {
        _capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
    }
}