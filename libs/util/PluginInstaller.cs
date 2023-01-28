using Godot;
using Godot.Collections;
using Directory = System.IO.Directory;
using SFile = System.IO.File;
using System.IO.Compression;
using System.Text.RegularExpressions;

public class PluginInstaller : Object
{
	AssetPlugin _plugin;
	public AssetPlugin AssetPlugin => _plugin;

	string subFolder = "";

	Array<string> _zipContents;
	Array<string> _fileList;

	Regex resMatch = new Regex(pattern: @"res:\/\/addons\/([\w\d\s-_]+)\/");

	public PluginInstaller(AssetPlugin plugin) {
		_plugin = plugin;
		_zipContents = new Array<string>();
		_fileList = new Array<string>();
	}

	private void GetSubFolder(ZipArchive za) {
		foreach (ZipArchiveEntry zae in za.Entries) {
			if (zae.Name.EndsWith(".gd")) {
				byte[] buffer = new byte[zae.Length];
				using (var fh = zae.Open()) {
					fh.Read(buffer,0,(int)zae.Length);
				}
				string data = System.Text.Encoding.UTF8.GetString(buffer, 0, (int)zae.Length);
				var res = resMatch.Matches(data);
				if (res.Count > 0) {
					subFolder = res[0].Groups[1].ToString();
					break;
				}
			}
		}
	}

	public Array<string> GetZipContents() {
		if (_zipContents.Count != 0)
			return _zipContents;
		
		Array<string> files = new Array<string>();

		using (ZipArchive za = ZipFile.OpenRead(_plugin.Location.GetOSDir())) {
			foreach (ZipArchiveEntry zae in za.Entries) {
				files.Add(zae.FullName);
			}
		}
		return files;
	}

	public Array<string> GetFileList() {
		if (_fileList.Count != 0)
			return _fileList;
		
		Array<string> files = new Array<string>();
		Array<string> ret = new Array<string>();

		using (ZipArchive za = ZipFile.OpenRead(_plugin.Location.GetOSDir())) {
			bool needAddonsFolder = true;
			foreach (ZipArchiveEntry zae in za.Entries) {
				files.Add(zae.FullName);
			}

			foreach (string file in files) {
				if (file.IndexOf("addons") >= 0) {
					needAddonsFolder = false;
					break;
				}
			}

			if (needAddonsFolder)
				GetSubFolder(za);
			else
				return files;
			
			foreach (string file in files) {
				string path = file.Substr(file.IndexOf("/")+1,file.Length);
				if (string.IsNullOrEmpty(subFolder))
					path = "addons/".Join(subFolder,path);
				else
					path = "addons/".Join(path);
				ret.Add(path);
			}
		}
		return ret;
	}

	public async void Install(string instLocation) {
		Array<string> files = _plugin.InstallFiles;
		bool needAddonsFolder = true;
		bool confirm_all = false;
		foreach (string file in files) {
			if (file.IndexOf("addons") >= 0) {
				needAddonsFolder = false;
				break;
			}
		}

		using (ZipArchive za = ZipFile.OpenRead(_plugin.Location.GetOSDir())) {
			if (needAddonsFolder)
				GetSubFolder(za);
			
			foreach (ZipArchiveEntry zae in za.Entries) {
				string path = zae.FullName;
				if (path.IndexOf("/") >= 0)
					path = path.Substr(path.IndexOf("/")+1,path.Length);
				
				if (string.IsNullOrEmpty(path))
					continue;

				if (needAddonsFolder) {
					if (string.IsNullOrEmpty(subFolder))
						path = "addons/".Join(subFolder,path);
					else
						path = "addons/".Join(path);
					
					if (!Directory.Exists(instLocation.Join(path).GetBaseDir().NormalizePath()))
						Directory.CreateDirectory(instLocation.Join(path).GetBaseDir().NormalizePath());
				}
				if (files.Contains(zae.FullName)) {
					if (zae.FullName.EndsWith("/")) {
						if (!Directory.Exists(instLocation.Join(path).NormalizePath()))
							Directory.CreateDirectory(instLocation.Join(path).NormalizePath());
					} else {
						if (SFile.Exists(instLocation.Join(path).NormalizePath())) {
							FileConflictDialog.ConflictAction res;
							if (!confirm_all) {
								AppDialogs.FileConflictDialog.ArchiveFile = zae.FullName;
								AppDialogs.FileConflictDialog.DestinationFile = instLocation.Join(path).NormalizePath();
								res = await AppDialogs.FileConflictDialog.ShowDialog();
								if (res == FileConflictDialog.ConflictAction.ConfirmAll)
									confirm_all = true;
							} else
								res = FileConflictDialog.ConflictAction.ConfirmAll;
							switch (res) {
								case FileConflictDialog.ConflictAction.Confirm:
								case FileConflictDialog.ConflictAction.ConfirmAll:
									try {
										zae.ExtractToFile(instLocation.Join(path).NormalizePath(), true);
									} catch (System.Exception ex) {
										GD.PrintErr($"Failed to write to file \"{zae.Name}\". Error Code: {ex.Message}");
									}
									break;
								case FileConflictDialog.ConflictAction.Abort:
									return;
							}
						} else {
							try {
								zae.ExtractToFile(instLocation.Join(path).NormalizePath());
							} catch (System.Exception ex) {
								GD.PrintErr($"Failed to write to file \"{zae.Name}\". Error Code: {ex.Message}");
							}
						}
					}
				}

			}
		}
	}

	void DeleteImports(string path) {
		if (!Directory.Exists(path.GetOSDir().NormalizePath()))
			return;
		
		foreach (string file in Directory.EnumerateFiles(path)) {
			if (file.EndsWith(".import"))
				SFile.Delete(file);
		}
	}
}