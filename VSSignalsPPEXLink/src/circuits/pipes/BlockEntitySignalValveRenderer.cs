using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using VSSignalsPPEXLink.circuits.pipes;

public class BlockEntitySignalValveRenderer : IRenderer
{
    private ICoreClientAPI capi;
    private BlockPos pos;
    private BlockEntitySignalValve beValve;
    public Matrixf ModelMat = new();


    // IRenderer requirement: Determines the frequency of your DrawOrder
    public double RenderOrder => 0.5; 
    
    // IRenderer requirement: The render pass you want to hook into
    public int RenderRange => 24; 

    public BlockEntitySignalValveRenderer(ICoreClientAPI capi, BlockEntitySignalValve valve)
    {
        this.capi = capi;
        this.beValve = valve;
        
        capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {

        IRenderAPI rpi = capi.Render;
        Vec3d camPos = capi.World.Player.Entity.CameraPos;

        Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(beValve.Pos.X, beValve.Pos.Y, beValve.Pos.Z);
        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(0);
        
        int extraGlow = beValve.IsOpen()?100:0;
        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true);
        
        IStandardShaderProgram prog = rpi.PreparedStandardShader(beValve.Pos.X, beValve.Pos.Y, beValve.Pos.Z);
        
        prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(beValve.Pos.X - camPos.X, beValve.Pos.Y - camPos.Y, beValve.Pos.Z - camPos.Z)
                .Values
            ;
        
        prog.RgbaLightIn = lightrgbs;
        prog.RgbaGlowIn = ColorUtil.ToRGBAVec4f(ColorUtil.HsvToRgb(43, 7, 3)); // new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);
        prog.ExtraGlow = extraGlow;
        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        //prog.TempGlowMode = stack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0;
        prog.TempGlowMode = 0;
        prog.ExtraGlow = 0;
        rpi.RenderMultiTextureMesh(beValve.BaseMeshRef, "tex");
        if (beValve.IsOpen()) {
            rpi.RenderMultiTextureMesh(beValve.HandleOpenMeshRef, "tex");
        } else {
            rpi.RenderMultiTextureMesh(beValve.HandleClosedMeshRef, "tex");
            rpi.RenderMultiTextureMesh(beValve.LidMeshRef, "tex");
        }

        prog.ExtraGlow = extraGlow;
        rpi.RenderMultiTextureMesh(beValve.GlowyPartMeshRef, "tex");
        
        prog.Stop();
    }

    public void Dispose()
    {
        capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
    }
}