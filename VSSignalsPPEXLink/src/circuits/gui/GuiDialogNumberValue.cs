using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using VSSignalsPPEXLink.packets;

namespace VSSignalsPPEXLink.assets.vssignalsppexlink.gui;

 public class GuiDialogNumberValueInput : GuiDialogGeneric
{
    double textinputFixedY;
    public Action<string> OnTextChanged;
    public Action OnCloseCancel;

    protected bool didSave;
    public Action<float> OnSave;


    public GuiDialogNumberValueInput(string DialogTitle, string text, ICoreClientAPI capi, float oldValue) : base(DialogTitle, capi)
    {
        ElementBounds textinputBounds = ElementBounds.Fixed(0, 0, 164, 32);
        textinputFixedY = textinputBounds.fixedY;

        // Clipping bounds for textinput
        ElementBounds clippingBounds = textinputBounds.ForkBoundingParent().WithFixedPosition(0, 30);

        ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(textinputBounds.fixedWidth + 3).WithFixedWidth(20);

        ElementBounds cancelButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 5).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8, 2).WithFixedAlignmentOffset(-1, 0);
        ElementBounds saveButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 5).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(8, 2);
        ElementBounds bgBounds = ElementBounds.FixedSize(160 + 32, 60).WithFixedPadding(GuiStyle.ElementToDialogPadding);

        // 3. Finally Dialog
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

        var font = CairoFont.TextInput().WithFontSize(20f).WithFont(GuiStyle.StandardFontName);
        font.LineHeightMultiplier = 0.9;


        SingleComposer = capi.Gui
            .CreateCompo("blockentitynumbervaluedialog", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
                .BeginClip(clippingBounds)
                .AddTextInput(textinputBounds, OnTextInputChanged, font, "text")
                .EndClip()
                .EndIf()
                .AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, cancelButtonBounds)
                .AddSmallButton(Lang.Get("Save"), OnButtonSave, saveButtonBounds)
            .EndChildElements()
            .Compose()
        ;

        SingleComposer.GetTextInput("text").SetMaxHeight((int)(32 * RuntimeEnv.GUIScale));

        if (text != null && text.Length > 0)
        {
            SingleComposer.GetTextInput("text").SetValue(text);
        }

    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        SingleComposer.FocusElement(SingleComposer.GetTextInput("text").TabIndex);
    }

    private void OnTextInputChanged(string value)
    {
        GuiElementTextInput textInput = SingleComposer.GetTextInput("text");

        OnTextChanged?.Invoke(textInput.GetText());
    }
    
    private void OnTitleBarClose()
    {
        OnButtonCancel();
    }

    private bool OnButtonSave()
    {
        GuiElementTextInput textInput = SingleComposer.GetTextInput("text");
        string text = textInput.GetText();

        OnSave(float.Parse(text));

        didSave = true;
        TryClose();
        return true;
    }

    private bool OnButtonCancel()
    {
        TryClose();
        return true;
    }

    public override void OnGuiClosed()
    {
        if (!didSave) OnCloseCancel?.Invoke();
        base.OnGuiClosed();
    }
}



public class GuiDialogBlockEntityNumberValueInput : GuiDialogNumberValueInput
{
    BlockPos blockEntityPos;
    public GuiDialogBlockEntityNumberValueInput(string DialogTitle, BlockPos blockEntityPos, string text, ICoreClientAPI capi, float value) : base(DialogTitle, text, capi, value)
    {
        this.blockEntityPos = blockEntityPos;
        this.OnSave = save;
    }

    public void save(float value)
    {
        byte[] data = SerializerUtil.Serialize(new SetDeviceSingleValuePacket()
        {
            value = value,
        });
        capi.Network.SendBlockEntityPacket(blockEntityPos, 1000, data);
    }
}