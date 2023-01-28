using Uri = System.Uri;
using SFile = System.IO.File;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;

public class NewsPanel : Panel
{
	const string GODOT_URL = "https://godotengine.org";
	const string BLOG_URL = GODOT_URL + "/blog/";

	[NodePath] private VBoxContainer NewsList = null;
	[NodePath] private TextureRect RefreshIcon = null;
	[NodePath] private PaginationNav PageCount = null;

	private GDCSHTTPClient _client = null;
	private DownloadQueue _queue = null;
	private int _page = 1;

	public override void _Ready()
	{
		_queue = new DownloadQueue();
		_queue.Name = "DownloadQueue";
		AddChild(_queue);
		this.OnReady();
		GetParent<TabContainer>().Connect("tab_changed", this, "OnPageChanged");
	}

	private void OnPageChanged(int page)
	{
		if (GetParent<TabContainer>().GetCurrentTabControl() == this && NewsList.GetChildCount() == 0)
		{
			RefreshNews();
		}
	}

	[SignalHandler("gui_input", nameof(RefreshIcon))]
    private void OnGuiInput_RefreshIcon(InputEvent @event)
    {
        if (!(@event is InputEventMouseButton iemb))
            return;

        if (iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left)
		{
			CentralStore.TotalNewsPages = 0;
            RefreshNews();
		}
    }

	[SignalHandler("page_changed", nameof(PageCount))]
	private void OnNewsPageChanged(int page)
	{
		_page = page + 1;
		RefreshNews();
	}

	private async Task<HTTPResponse> ConnectToBlogPage(int page = 1)
	{
		InitClient();

		Uri blogUri = new Uri(BLOG_URL + (page <= 1 ? "" : page.ToString() + "/"));
		Task<HTTPClient.Status> cres = _client.StartClient(blogUri.Host, blogUri.Port, true);

		while (!cres.IsCompleted)
			await this.IdleFrame();

		if (!_client.SuccessConnect(cres.Result))
		{
			return null;
		}

		var tres = _client.MakeRequest(blogUri.PathAndQuery);
		while (!tres.IsCompleted)
			await this.IdleFrame();
		_client.Close();

		if (tres.Result.ResponseCode != 200)
		{
			CleanupClient();
			return null;
		}

		return tres.Result;
	}

	private async void RefreshNews()
	{
		Task<HTTPResponse> task = null;

		NewsList.QueueFreeChildren();
		AppDialogs.BusyDialog.ShowDialog();
		AppDialogs.BusyDialog.UpdateHeader(Tr("Fetching News"));
		if (CentralStore.TotalNewsPages <= 0)
		{
			PageCount.Visible = false;
			AppDialogs.BusyDialog.UpdateByline(Tr("Getting pages from Godot Blog..."));
			for (int i = 1; i <= 48; i++)
			{
				task = ConnectToBlogPage(i);
				while (!task.IsCompleted)
					await this.IdleFrame();
				if (task.Result != null)
				{
					++CentralStore.TotalNewsPages;
				}
				else
				{
					break;
				}
			}
			CentralStore.Instance.SaveDatabase();
		}
		AppDialogs.BusyDialog.UpdateByline(Tr("Fetching article information from Godot Blog..."));
		_page = Mathf.Clamp(_page, 1, CentralStore.TotalNewsPages);
		task = ConnectToBlogPage(_page);
		while (!task.IsCompleted)
			await this.IdleFrame();
		PageCount.UpdateConfig(CentralStore.TotalNewsPages);
		PageCount.SetPage(_page - 1);

		if (task.Result == null)
		{
			AppDialogs.BusyDialog.HideDialog();
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("Failed to connect to Godot Blog."));
			return;
		}

		var feed = ParseNews(task.Result.Body);
		foreach (Dictionary<string,string> item in feed)
		{
			var newsItem = MainWindow._plScenes["NewsItem"].Instance<NewsItem>();
			newsItem.Headline = "    " + item["title"];
			newsItem.Byline = $"    {item["author"]}{item["date"].Replace("&nbsp;", " ")}";
			newsItem.Url = item["link"];
			newsItem.Blerb = item["contents"];

			Uri uri = new Uri(item["image"]);
			string imgPath = $"{CentralStore.Settings.CachePath}/images/{uri.AbsolutePath.GetFile()}";
			if (!SFile.Exists(imgPath.GetOSDir().NormalizePath()))
			{
				ImageDownloader dld = new ImageDownloader(item["image"], imgPath);
				_queue.Push(dld);
				newsItem.SetMeta("imgPath", imgPath);
				newsItem.SetMeta("dld", dld);
			}
			else
			{
				newsItem.Image = imgPath.GetOSDir().NormalizePath();
			}

			uri = new Uri(item["avatar"]);
			imgPath = $"{CentralStore.Settings.CachePath}/images/{uri.AbsolutePath.GetFile()}";
			if (!SFile.Exists(imgPath.GetOSDir().NormalizePath()))
			{
				ImageDownloader dld = new ImageDownloader(item["avatar"], imgPath);
				_queue.Push(dld);
				newsItem.SetMeta("avatarPath", imgPath);
				newsItem.SetMeta("avatarDld", dld);
			}
			else
			{
				newsItem.Avatar = imgPath.GetOSDir().NormalizePath();
			}

			NewsList.AddChild(newsItem);
		}
		_queue.StartDownload();
		AppDialogs.BusyDialog.HideDialog();
	}

	[SignalHandler("download_completed", nameof(_queue))]
	void OnImageDownloaded(ImageDownloader dld)
	{
		foreach (NewsItem item in NewsList.GetChildren())
		{
			if (item.HasMeta("dld"))
			{
				if ((item.GetMeta("dld") as ImageDownloader) == dld)
				{
					item.RemoveMeta("dld");
					string imgPath = item.GetMeta("imgPath") as string;
					if (SFile.Exists(imgPath.GetOSDir().NormalizePath()))
					{
						item.Image = imgPath.GetOSDir().NormalizePath();
					}
					else
					{
						// Need Generic Image to use instead.
					}

					break;
				}
			}

			if (item.HasMeta("avatarDld"))
			{
				if ((item.GetMeta("avatarDld") as ImageDownloader) == dld)
				{
					item.RemoveMeta("avatarDld");
					var avatarPath = item.GetMeta("avatarPath") as string;
					if (SFile.Exists(avatarPath.GetOSDir().NormalizePath()))
					{
						item.Avatar = avatarPath.GetOSDir().NormalizePath();
					}
					else
					{
						// Need Generic Image to use instead.
					}

					break;
				}
			}
		}
	}

	private void InitClient()
	{
		if (_client != null)
			CleanupClient();
		_client = new GDCSHTTPClient();
	}

	private void CleanupClient()
	{
		_client.QueueFree();
		_client = null;
	}

	private Array<Dictionary<string,string>> ParseNews(string buffer)
	{
		var parsed_news = new Array<Dictionary<string,string>>();

		var xml = new XMLParser();
        var error = xml.OpenBuffer(buffer.ToUTF8());
        if (error != Error.Ok) return parsed_news;
        while (true)
        {
            var err = xml.Read();
			if (err != Error.Ok)
			{
				if (err != Error.FileEof)
				{
					GD.PrintErr($"Failed to parse XML contents of \"{BLOG_URL}\". Error Code: {err}");
				}
				break;
			}

            if (xml.GetNodeType() != XMLParser.NodeType.Element || xml.GetNodeName() != "article") continue;
            var tag_open_offset = xml.GetNodeOffset();
            xml.SkipSection();
            xml.Read();
            var tag_close_offset = xml.GetNodeOffset();
            parsed_news.Add(ParseNewsItem(buffer, tag_open_offset, tag_close_offset));
        }

		return parsed_news;
	}

	private Dictionary<string, string> ParseNewsItem(string buffer, ulong begin_ofs, ulong end_ofs)
    {
        var parsed_item = new Dictionary<string, string>();
        var xml = new XMLParser();
        var error = xml.OpenBuffer(buffer.ToUTF8());
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to open XML raw buffer \"{buffer}\". Error Code: {error}");
            return null;
        }

        xml.Seek(begin_ofs);

        while (xml.GetNodeOffset() != end_ofs)
        {
            if (xml.GetNodeType() == XMLParser.NodeType.Element)
            {
                switch (xml.GetNodeName())
                {
                    case "div":
                        // <div class="thumbnail" style="background-image: url('https://godotengine.org/storage/app/uploads/....');" href="https://godotengine.org/article/.....">
                        if (xml.GetNamedAttributeValueSafe("class") == "thumbnail")
                        {
                            var image_style = xml.GetNamedAttributeValueSafe("style");
                            var url_start = image_style.Find("'") + 1;
                            var url_end = image_style.FindLast("'");
                            var image_url = image_style.Substr(url_start, url_end - url_start);

                            parsed_item["image"] = GODOT_URL + image_url;
                            parsed_item["link"] = GODOT_URL + xml.GetNamedAttributeValueSafe("href");
                        }

                        break;
                    
                    case "h3":
                        // <h3>Article Title</h3>
                        xml.Read();
                        parsed_item["title"] = xml.GetNodeType() == XMLParser.NodeType.Text
                            ? xml.GetNodeData().StripEdges()
                            : "";

                        break;
                    case "span":
                        // <span class="date">&nbsp;-&nbsp;dd Month year</span>
                        if (xml.GetNamedAttributeValueSafe("class") == "date")
                        {
                            xml.Read();
                            parsed_item["date"] = xml.GetNodeType() == XMLParser.NodeType.Text ? xml.GetNodeData() : "";
                        }
                        // <span class="by">Author Name</span>
                        if (xml.GetNamedAttributeValueSafe("class") == "by")
                        {
                            xml.Read();
                            parsed_item["author"] = xml.GetNodeType() == XMLParser.NodeType.Text
                                ? xml.GetNodeData().StripEdges()
                                : "";
                        }

                        break;
                    case "p":
                        // <p class="excerpt">An excerpt of the blog entry to be read in.</p>
                        if (xml.GetNamedAttributeValueSafe("class") == "excerpt")
                        {
                            xml.Read();
                            parsed_item["contents"] =
                                xml.GetNodeType() == XMLParser.NodeType.Text ? xml.GetNodeData() : "";
                        }

                        break;
                    case "img":
                        // <img class="avatar" width="25" height="25" src="https://godotengine.org/storage/app/uploads/public/....." alt="">
                        if (xml.GetNamedAttributeValueSafe("class") == "avatar")
                        {
                            parsed_item["avatar"] = GODOT_URL + xml.GetNamedAttributeValueSafe("src");
                        }
                        break;
                }
            }

            xml.Read();
        }

        return parsed_item;
    }
}
