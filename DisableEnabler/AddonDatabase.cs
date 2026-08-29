using System.Collections.Generic;
using Newtonsoft.Json;

namespace DisableEnabler;

public sealed class AddonDatabaseFile
{
    [JsonProperty("revision")]
    public int Revision { get; set; }

    [JsonProperty("planes")]
    public Dictionary<string, AddonPlaneEntry> Planes { get; set; } = new();
}

public sealed class AddonPlaneEntry
{
    [JsonProperty("PlaneName")]
    public string PlaneName { get; set; } = string.Empty;

    [JsonProperty("Notes")]
    public string? Notes { get; set; }

    [JsonProperty("Notes2")]
    public string? Notes2 { get; set; }

    [JsonProperty("URL")]
    public string? URL { get; set; }
}
