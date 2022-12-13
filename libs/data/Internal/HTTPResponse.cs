using Godot;
using Godot.Collections;
using Task = System.Threading.Tasks.Task;
using Exception = System.Exception;
using SPath = System.IO.Path;
using SFile = System.IO.File;
using FileInfo = System.IO.FileInfo;
using FileAttributes = System.IO.FileAttributes;
using FileStream = System.IO.FileStream;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

public class HTTPResponse : Object {
	public int ResponseCode;
	public Dictionary Headers;
	public byte[] BodyRaw;
	public string Body;
	public bool Cancelled;

	private HTTPClient client;
	private Object call_from;
	private bool bBinary;

	public async Task FromClient(Object call_from, HTTPClient client, bool binary = false) {
		this.client = client;
		this.call_from = call_from;
		ResponseCode = client.GetResponseCode();
		Headers = client.GetResponseHeadersAsDictionary();
		call_from.EmitSignal("headers_received", Headers);
		Cancelled = false;
		bBinary = binary;
		Task res = GetBody();
		while (!res.IsCompleted) {
			await this.IdleFrame();
		}
	}

	private async Task GetBody() {
		string tmpfile = SPath.GetTempFileName();
		FileInfo info = new FileInfo(tmpfile);
		info.Attributes = FileAttributes.Temporary;

		using (FileStream streamWriter = SFile.OpenWrite(tmpfile)) {
			DateTime start = DateTime.Now;
			while (client.GetStatus() == HTTPClient.Status.Body)
			{
				if (Cancelled) {
					return;
				}
				var res = client.Poll();
				byte[] chunk = client.ReadResponseBodyChunk();
				if (chunk.Length == 0)
					await this.IdleFrame();
				else {
					streamWriter.Write(chunk, 0, chunk.Length);
					//rb.AddRange(chunk);
					call_from.EmitSignal("chunk_received", chunk.Length);
				}
			}
			TimeSpan elapsed = DateTime.Now - start;
		}
		BodyRaw = SFile.ReadAllBytes(tmpfile);
		SFile.Delete(tmpfile);
#pragma warning disable CS0168
		if (!bBinary) {
			try {
				Body = BodyRaw.GetStringFromUTF8();
			} catch (Exception ex) {
				// We don't care about exceptions from this, as we may not be getting strings
				// but raw bytes, such as zip files, executables, etc, etc.
			}
		}
#pragma warning restore CS0168
	}
}