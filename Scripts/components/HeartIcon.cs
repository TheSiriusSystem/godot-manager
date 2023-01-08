using Godot;
using Godot.Sharp.Extras;

public class HeartIcon : TextureRect
{

    [Signal]
    public delegate void clicked();
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.OnReady();
    }

    public bool IsChecked() {
        return Modulate == new Color("ffffffff");
    }

    public void SetCheck(bool check) {
        if (check)
            Modulate = new Color("ffffffff");
        else
            Modulate = new Color("d16de3db");
    }

    [SignalHandler("gui_input")]
    void OnGuiInput(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left) {
            SetCheck(!IsChecked());
            EmitSignal("clicked");
        }
    }
}
