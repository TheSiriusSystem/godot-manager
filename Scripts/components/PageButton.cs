using Godot;
using Godot.Sharp.Extras;

[Tool]
public class PageButton : ColorRect
{
    [NodePath("Label")]
    private Label _label = null;

    [NodePath("Icon")]
    private TextureRect _icon = null;

    [NodePath("Active")]
    private ColorRect _active = null;

    [Signal]
    delegate void Clicked(PageButton button);

    private string sLabel;
    private StreamTexture sIcon;

    private bool bActive;

    [Export]
    public string Label {
        get {
            return sLabel;
        }
        set {
            sLabel = value;
            if (_label != null) {
                _label.Text = sLabel;
            }
        }
    }

    [Export]
    public StreamTexture Icon {
        get {
            return sIcon;
        }

        set {
            sIcon = value;
            if (_icon != null) {
                _icon.Texture = sIcon;
            }
        }
    }

    [Export]
    public bool Active {
        get {
            return bActive;
        }
        set {
            bActive = value;
        }
    }

    public bool IsActive {
        get {
            return bActive;
        }
    }

    public override void _Ready()
    {
        this.OnReady();

        Label = sLabel;
        Icon = sIcon;
        UpdateElements();
    }

    [SignalHandler("gui_input")]
    void OnPageButton_GuiInput(InputEvent inputEvent) {
        if (!(inputEvent is InputEventMouseButton))
            return;
        
        var iemb = inputEvent as InputEventMouseButton;
        if (!iemb.Pressed && iemb.ButtonIndex != (int)ButtonList.Left)
            return;

        Activate();
        EmitSignal("Clicked",this);
    }

    [SignalHandler("mouse_entered")]
    void OnPageButton_MouseEntered() {
        Color = new Color("4b5163");
    }

    [SignalHandler("mouse_exited")]
    void OnPageButton_MouseExited() {
        Color = new Color("383d4a");
    }

    private void UpdateElements() {
        _active.Visible = IsActive;
        Label = sLabel;
        Icon = sIcon;
    }

    private void ClearActiveState() {
        foreach (var pb in GetTree().GetNodesInGroup("page_buttons")) {
            if (pb is PageButton) {
                var pbutt = pb as PageButton;
                if (pbutt.IsActive)
                    pbutt.Deactivate();
            }
        }
    }

    public void Activate() {
        ClearActiveState();
        Active = true;
        UpdateElements();
    }

    public void Deactivate() {
        Active = false;
        UpdateElements();
    }

}
