using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using System.Linq;
using Uri = System.Uri;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

public class DownloadAddon : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void download_completed(AssetLib.Asset asset, AssetProject ap, AssetPlugin apl);

	[Signal]
	public delegate void download_failed(string errDesc);
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MC/TitleBarBG/HB/Title")]
	Label _Title = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/GridContainer/FileName")]
	Label _FileName = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/GridContainer/FileSize")]
	Label _FileSize = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/GridContainer/Speed")]
	Label _Speed = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/GridContainer/Eta")]
	Label _Eta = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/ProgressBar")]
	ProgressBar _ProgressBar = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _CancelButton = null;

	[NodePath("DownloadSpeedTimer")]
	Timer _DownloadSpeedTimer = null;

	[NodePath("IndeterminateProgress")]
	Tween _IndeterminateProgress = null;
#endregion

#region Private Variables
	int iTotalBytes;
	int iLastByteCount;
	int iFileSize;
	DateTime dtStartTime;
	GDCSHTTPClient client;
	Uri dlUri;
	bool bDownloading = false;
	Array<double> adSpeedStack;

	private Array<string> Templates = new Array<string> {"Templates", "Projects", "Demos"};
#endregion

#region Public Accessors
	public AssetLib.Asset Asset;
#endregion

	public override void _Ready()
	{
		this.OnReady();
		iTotalBytes = 0;
		iLastByteCount = 0;
		iFileSize = 0;
		adSpeedStack = new Array<double>();
	}

	[SignalHandler("pressed", nameof(_CancelButton))]
	void OnCancelPressed() {
		if (client != null)
			client.Cancel();
		_DownloadSpeedTimer.Stop();
		Visible = false;
	}

	void OnChunkReceived(int bytes) {
		iTotalBytes += bytes;
		if (iFileSize >= 0) {
			_ProgressBar.Value = iTotalBytes;
			_ProgressBar.Update();
		}
	}

	[SignalHandler("timeout", nameof(_DownloadSpeedTimer))]
	void OnDownloadSpeedTimer_Timeout() {
		Mutex mutex = new Mutex();
		mutex.Lock();
		var lbc = iLastByteCount;
		var tb = iTotalBytes;
		var speed = tb - lbc;
		adSpeedStack.Add(speed);
		var avgSpeed = adSpeedStack.Sum() / adSpeedStack.Count;
		_Speed.Text = $"{Util.FormatSize(avgSpeed)}/s";
		_FileSize.Text = Util.FormatSize(tb);
		TimeSpan elapsedTime = DateTime.Now - dtStartTime;
		_Eta.Text = elapsedTime.ToString("hh':'mm':'ss");
		iLastByteCount = iTotalBytes;
		mutex.Unlock();
	}

	async Task StartIndeterminateTween() {
		_ProgressBar.RectRotation = 0;
		_ProgressBar.RectPivotOffset = new Vector2(_ProgressBar.RectSize.x/2,_ProgressBar.RectSize.y/2);
		_ProgressBar.Value = 0;
		while (bDownloading) {
			_IndeterminateProgress.InterpolateProperty(_ProgressBar, "value", 0, 100, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
			_IndeterminateProgress.Start();
			while (_IndeterminateProgress.IsActive() && bDownloading)
				await this.IdleFrame();
			_ProgressBar.RectRotation = 180;
			_IndeterminateProgress.InterpolateProperty(_ProgressBar, "value", 100, 0, 0.5f, Tween.TransitionType.Linear, Tween.EaseType.InOut);
			_IndeterminateProgress.Start();
			while (_IndeterminateProgress.IsActive() && bDownloading)
				await this.IdleFrame();
			_ProgressBar.RectRotation = 0;
		}
		if (_IndeterminateProgress.IsActive())
			_IndeterminateProgress.StopAll();
		_ProgressBar.RectRotation = 0;
		_ProgressBar.Value = 0;
	}

	public async Task<bool> StartNetwork()
	{
		bDownloading = true;
		adSpeedStack.Clear();
		InitClient();

		Task<HTTPClient.Status> cres = client.StartClient(dlUri.Host, dlUri.Port, (dlUri.Scheme == "https"));

		while (!cres.IsCompleted)
			await this.IdleFrame();

		if (!client.SuccessConnect(cres.Result))
		{
			EmitSignal("download_failed", "Unable to connect to Godot Asset Library.");
			CleanupClient();
			return false;
		}

		// When using MakeRequest(), it will get the body before we get the headers back from the
		// function call.  We need to see about using HEAD to get the filesize, before the actual
		// request, this will also allow us to use HEAD to get redirect, without a body, I believe.
		var tresult = client.HeadRequest(dlUri.PathAndQuery);
		while (!tresult.IsCompleted)
			await this.IdleFrame();
		client.Close();

		HTTPResponse result = tresult.Result;
		if (result == null || result.Cancelled) {
			EmitSignal("download_failed", "");
			CleanupClient();
			return false;
		}

		Array<int> redirect_codes = new Array<int> { 301, 302, 303, 307, 308 };

		if (redirect_codes.IndexOf(result.ResponseCode) >= 0)
		{
			dlUri = new Uri(result.Headers["Location"] as string);
			CleanupClient();
			Task<bool> recurse = StartNetwork();
			await recurse;
			return recurse.Result;
		}

		if (result.ResponseCode != 200)
		{
			EmitSignal("download_failed", "Unable to connect to Godot Asset Library.");
			CleanupClient();
			return false;
		}

		UpdateFields(result);

		cres = client.StartClient(dlUri.Host, dlUri.Port, (dlUri.Scheme == "https"));
		while (!cres.IsCompleted)
			await this.IdleFrame();

		if (!client.SuccessConnect(cres.Result))
		{
			EmitSignal("download_failed", "Unable to connect to Godot Asset Library.");
			CleanupClient();
			return false;
		}

		// Begin Actual Network download of addon/project/demo....
		dtStartTime = DateTime.Now;
		_DownloadSpeedTimer.Start();
		tresult = client.MakeRequest(dlUri.PathAndQuery, true);

		while (!tresult.IsCompleted)
		{
			await this.IdleFrame();
			if (tresult.IsCanceled)
				break;
		}

		client.Close();
		bDownloading = false;
		if (tresult.IsCanceled)
		{
			EmitSignal("download_failed", "");
			CleanupClient();
			return false;
		}

		result = tresult.Result;

		if (result == null || result.Cancelled)
		{
			EmitSignal("download_failed", "");
			CleanupClient();
			return false;
		}

		string zipName = $"{Asset.Type}-{Asset.AssetId}-{Asset.Title}.zip".NormalizeFileName();
		string sPath = $"{CentralStore.Settings.CachePath}/downloads/{zipName}";

		File fh = new File();
		Error err = fh.Open(sPath, File.ModeFlags.Write);
		if (err != Error.Ok) {
			EmitSignal("download_failed", $"Failed to write to file \"{sPath.GetFile()}\".");
			CleanupClient();
			return false;
		}

		fh.StoreBuffer(result.BodyRaw);
		fh.Close();

		Visible = false;
		CleanupClient();

		AssetProject ap = null;
		AssetPlugin apl = null;
		
		if (Templates.IndexOf(Asset.Category) != -1) {
			ap = new AssetProject();
			ap.Asset = Asset;
			ap.Location = sPath;
			if (CentralStore.Instance.HasTemplateId(Asset.AssetId)) {
				CentralStore.Templates.Remove(CentralStore.Instance.GetTemplateId(Asset.AssetId));
			}
			CentralStore.Templates.Add(ap);
		} else {
			apl = new AssetPlugin();
			apl.Asset = Asset;
			apl.Location = sPath;
			if (CentralStore.Instance.HasPluginId(Asset.AssetId)) {
				CentralStore.Plugins.Remove(CentralStore.Instance.GetPluginId(Asset.AssetId));
			}
			CentralStore.Plugins.Add(apl);
		}
		CentralStore.Instance.SaveDatabase();

		EmitSignal("download_completed", Asset, ap, apl);
		_DownloadSpeedTimer.Stop();

		return true;
	}

	private void UpdateFields(HTTPResponse result)
	{
		if (result.Headers.Contains("Content-Length"))
		{
			if (int.TryParse(result.Headers["Content-Length"] as string, out iFileSize))
			{
				_FileSize.Text = Util.FormatSize(iFileSize);
				_ProgressBar.MaxValue = iFileSize;
				_ProgressBar.Value = 0;
			}
			else
			{
				iFileSize = -1;
				_FileSize.Text = "0.00 B";
				_Eta.Text = "00:00:00";
				_ProgressBar.Value = 0;
				_ProgressBar.MaxValue = 100;
				Task task = StartIndeterminateTween();

			}
		}
		else
		{
			iFileSize = -1;
			_ProgressBar.Value = 0;
			_ProgressBar.MaxValue = 100;
			_FileSize.Text = "0.00 B";
			_Eta.Text = "00:00:00";
			Task task = StartIndeterminateTween();
		}
	}

	private void InitClient()
	{
		if (client != null)
			CleanupClient();
		client = new GDCSHTTPClient();
		client.Connect("chunk_received", this, "OnChunkReceived");
	}

	private void CleanupClient()
	{
		client.Disconnect("chunk_received", this, "OnChunkReceived");
		client.QueueFree();
		client = null;
	}

	public void LoadInformation() {
		_FileName.Text = Asset.Title;
		_FileSize.Text = "0.00 B";
		_Speed.Text = "0.00 B/s";
		_Eta.Text = "00:00:00";
		_ProgressBar.Value = 0;
		_ProgressBar.MaxValue = 100;
		iTotalBytes = 0;
		iLastByteCount = 0;
		iFileSize = 0;
	}

	public async Task StartDownload() {
		if (Asset == null)
			return;
		Visible = true;
		LoadInformation();
		dlUri = new Uri(Asset.DownloadUrl);
		await StartNetwork();
	}
}
