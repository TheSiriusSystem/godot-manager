using Godot;
using Godot.Sharp.Extras;

public class SplashScreen : Control
{
	[NodePath] private Label VersionInfo = null;
	[NodePath] private Label ErrorText = null;
	private Thread _thread;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();
		VersionInfo.Text = $"Version {VERSION.GodotManager}";
		CallDeferred("GDThread_Init");
	}

	void GDThread_Init()
	{
		_thread = new Thread();
		_thread.Start(this, "GDThread_Loader");
	}

	void GDThread_Loader()
	{
		ResourceInteractiveLoader loader = ResourceLoader.LoadInteractive("res://Scenes/SceneManager.tscn");
		if (loader == null)
		{
			ErrorText.Text = "An error occurred while loading the scene manager.";
			return;
		}

		do
		{
			Error err = loader.Poll();
			if (err == Error.FileEof)
			{
				CallDeferred("ThreadDone", (PackedScene)loader.GetResource());
				break;
			} else if (err != Error.Ok)
			{
				ErrorText.Text = "An error occurred.\nError Code: " + err.ToString();
				break;
			}
		} while (true);
	}

	void ThreadDone(PackedScene res)
	{
		_thread.WaitToFinish();

		SceneManager inst = res.Instance<SceneManager>();
		GetTree().CurrentScene.QueueFree();
		GetTree().CurrentScene = null;
		GetTree().Root.AddChild(inst);
		GetTree().CurrentScene = inst;
	}
}
