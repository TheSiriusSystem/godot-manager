﻿using Godot;
using Newtonsoft.Json;
using TimeSpan = System.TimeSpan;

[JsonObject(MemberSerialization.OptIn)]
public class CustomEngineDownload : Object
{
    [JsonProperty] public int Id;
    [JsonProperty] public string Name;
    [JsonProperty] public string Url;
    [JsonProperty] public bool NightlyBuild;
    [JsonProperty] public TimeSpan Interval;
    [JsonProperty] public string TagName;
    [JsonProperty] public int DownloadSize;

    public CustomEngineDownload()
    {
        Id = -1;
        Name = "";
        Url = "";
        NightlyBuild = false;
        Interval = TimeSpan.Zero;
        TagName = "";
        DownloadSize = 0;
    }
}