using System.Threading.Tasks;
using Uri = System.Uri;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using DateTimeOffset = System.DateTimeOffset;

namespace Github {
	public class Github : Node {
		[Signal]
		public delegate void chunk_received(int size);

		[Signal]
		public delegate void request_completed();

#region Singleton Instance
		private static Github _instance;

		public static Github Instance {
			get {
				if (_instance == null)
					_instance = new Github();
				return _instance;
			}
		}
#endregion

		GDCSHTTPClient client = null;

		private RateLimit lastLimit;

		public RateLimit Limit {
			get {
				return lastLimit;
			}
		}

		private Github() {
			client = new GDCSHTTPClient();
			lastLimit = new RateLimit();
		}

		private void UpdateLimit(HTTPResponse response) {
			Limit.Limit = (response.Headers["X-RateLimit-Limit"] as string).ToInt();
			Limit.Remaining = (response.Headers["X-RateLimit-Remaining"] as string).ToInt();
			Limit.Reset = DateTimeOffset.FromUnixTimeSeconds((response.Headers["X-RateLimit-Reset"] as string).ToInt()).DateTime;
			Limit.Used = (response.Headers["X-RateLimit-Used"] as string).ToInt();
		}

		public async Task<Release> GetLatestRelease() {
			Release ret = null;
			Uri uri = new Uri("https://api.github.com/repos/godotengine/godot/releases/latest");
			if (CentralStore.Settings.UseProxy)
				client.SetProxy(CentralStore.Settings.ProxyHost, CentralStore.Settings.ProxyPort, uri.Scheme == "https");
			else
				client.ClearProxy();
			Task<HTTPClient.Status> cres = client.StartClient(uri.Host, uri.Port, true);

			while (!cres.IsCompleted) {
				await this.IdleFrame();
			}

			if (!client.SuccessConnect(cres.Result))
				return ret;
			
			string path = uri.AbsolutePath;
			var tresult = client.MakeRequest(path);
			while (!tresult.IsCompleted) {
				await this.IdleFrame();
			}

			Mutex mutex = new Mutex();
			mutex.Lock();
			HTTPResponse result = tresult.Result;
			
			client.Close();
			
			UpdateLimit(result);

			if (result.ResponseCode != 200)
				return null;
			else
				ret = JsonConvert.DeserializeObject<Release>(result.Body, DefaultSettings.defaultJsonSettings);

			mutex.Unlock();

			return ret;			
		}

		public async Task<Release> GetLatestManagerRelease() {
			Release ret = null;
			Uri uri = new Uri("https://api.github.com/thesiriussystem/godot-manager/releases/latest");
			if (CentralStore.Settings.UseProxy)
				client.SetProxy(CentralStore.Settings.ProxyHost, CentralStore.Settings.ProxyPort, uri.Scheme == "https");
			else
				client.ClearProxy();
			Task<HTTPClient.Status> cres = client.StartClient(uri.Host, uri.Port, true);

			while (!cres.IsCompleted) {
				await this.IdleFrame();
			}

			if (!client.SuccessConnect(cres.Result))
				return ret;
			
			string path = uri.AbsolutePath;
			var tresult = client.MakeRequest(path);
			while (!tresult.IsCompleted) {
				await this.IdleFrame();
			}

			Mutex mutex = new Mutex();
			mutex.Lock();
			HTTPResponse result = tresult.Result;

			client.Close();

			UpdateLimit(result);

			if (result.ResponseCode != 200)
				return null;
			else
				ret = JsonConvert.DeserializeObject<Release>(result.Body, DefaultSettings.defaultJsonSettings);
			
			mutex.Unlock();

			return ret;
		}

		public async Task<Array<Release>> GetReleases(int per_page=0, int page=1) {
			Array<Release> ret = new Array<Release>();
			Uri uri = new Uri("https://api.github.com/repos/godotengine/godot/releases");
			if (CentralStore.Settings.UseProxy)
				client.SetProxy(CentralStore.Settings.ProxyHost, CentralStore.Settings.ProxyPort, uri.Scheme == "https");
			else
				client.ClearProxy();
			Task<HTTPClient.Status> cres = client.StartClient(uri.Host, uri.Port ,true);
			
			while (!cres.IsCompleted) {
				await this.IdleFrame();
			}

			if (!client.SuccessConnect(cres.Result))
				return ret;

			string path = "/repos/godotengine/godot/releases";
			if (per_page > 0)
				path += $"?per_page={per_page}";
			if (page > 1 && per_page > 0)
				path += $"&page={page}";
			else if (page > 1)
				path += $"?page={page}";
			
			var tresult = client.MakeRequest(path);
			while (!tresult.IsCompleted) {
				await this.IdleFrame();
			}

			Mutex mutex = new Mutex();
			mutex.Lock();
			HTTPResponse result = tresult.Result;
			client.Close();

			UpdateLimit(result);

			// Check for Errors:
			if (result.ResponseCode != 200)
				return null;
			else
				ret = JsonConvert.DeserializeObject<Array<Release>>(result.Body, DefaultSettings.defaultJsonSettings);
			
			mutex.Unlock();

			return ret;
		}

		public async Task<Array<Release>> GetAllReleases() {
			Array<Release> releases = new Array<Release>();
			Mutex mutex = new Mutex();
			bool stop = false;
			int page = 1;
			while (stop == false) {
				var tres = GetReleases(30,page);
				while (!tres.IsCompleted)
					await this.IdleFrame();
				
				mutex.Lock();
				if (tres.Result == null) {
					OS.Alert(Tr("Failed to get Release information from Github"), Tr("Github Connection Error"));
					stop = true;
					continue;
				}
				Array<Release> relRes = tres.Result;
				if (relRes.Count > 0) {
					foreach(Release release in relRes) {
						releases.Add(release);
					}
				} else
					stop = true;
				page++;
				mutex.Unlock();
			}

			return releases;
		}
	}
}