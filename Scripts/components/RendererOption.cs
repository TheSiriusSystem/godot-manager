using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;

public class RendererOption : CheckBox
{
    [Export]
    public Array<Dictionary> metadata = new Array<Dictionary>();

    public Dictionary currentMeta;

    [NodePath("../../../../GodotVersion")]
	OptionButton _godotVersion = null;

    [NodePath("../../Description")]
    Label _description = null;

	public override void _Ready() {
		this.OnReady();
	}

    [SignalHandler("toggled")]
    void OnToggled(bool buttonPressed) {
        if (buttonPressed) {
            foreach (Dictionary meta in metadata) {
                if ((_godotVersion.GetSelectedMetadata() as GodotVersion).GetMajorVersion() == (int)meta["version"]) {
                    currentMeta = meta.Duplicate();
                    _description.Text = Tr(@"" + (string)meta["description"]);
                    break;
                }
            }
        }
    }
}
