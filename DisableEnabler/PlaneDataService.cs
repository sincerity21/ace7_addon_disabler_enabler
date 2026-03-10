using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace DisableEnabler;

public static class PlaneDataService
{
    public static string FindPlayerPlaneDataTable(string rootDir)
    {
        var flatPath = Path.Combine(rootDir, "PlayerPlaneDataTable.uasset");
        if (File.Exists(flatPath))
            return flatPath;

        var matches = Directory.GetFiles(rootDir, "PlayerPlaneDataTable.uasset", SearchOption.AllDirectories);
        if (matches.Length == 0)
            throw new FileNotFoundException("PlayerPlaneDataTable.uasset not found under unpack directory.", rootDir);

        return matches[0];
    }

    public static (List<PlaneDataRow> rows, string jsonPath) LoadPlanesToJsonAndRows(string assetPath, Action<string> log)
    {
        var jsonPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? string.Empty, "PlayerPlaneDataTable.json");
        log($"Exporting JSON to {jsonPath}");

        // Use UAssetAPI to load the asset and export JSON
        var asset = new UAsset(assetPath, EngineVersion.VER_UE4_18);
        var json = asset.SerializeJson(true);
        File.WriteAllText(jsonPath, json);

        var jObject = JObject.Parse(json);
        var rows = ExtractPlaneRows(jObject, log);
        return (rows, jsonPath);
    }

    private static List<PlaneDataRow> ExtractPlaneRows(JObject root, Action<string> log)
    {
        var rows = new List<PlaneDataRow>();

        // Heuristic: walk all string properties named PlaneStringID and capture their PlaneID / OriginalPlaneID
        var tokens = root.SelectTokens("$..[?(@.Name=='PlaneStringID')]");
        foreach (var token in tokens)
        {
            if (token is not JObject obj)
                continue;

            var valueToken = obj["Value"];
            var planeId = valueToken?.ToString();
            if (string.IsNullOrWhiteSpace(planeId))
                continue;

            var planeNumericId = -1;
            var originalPlaneId = -1;
            var targetMode = string.Empty;
            var dlcId = string.Empty;
            var parentArray = obj.Parent as JArray;
            if (parentArray != null)
            {
                foreach (var sibling in parentArray.OfType<JObject>())
                {
                    var name = sibling["Name"]?.ToString();
                    if (string.Equals(name, "PlaneID", StringComparison.Ordinal))
                    {
                        var idToken = sibling["Value"];
                        if (idToken != null && int.TryParse(idToken.ToString(), out var parsedId))
                        {
                            planeNumericId = parsedId;
                        }
                    }
                    else if (string.Equals(name, "OriginalPlaneID", StringComparison.Ordinal))
                    {
                        var idToken = sibling["Value"];
                        if (idToken != null && int.TryParse(idToken.ToString(), out var parsedId))
                        {
                            originalPlaneId = parsedId;
                        }
                    }
                    else if (string.Equals(name, "TargetMode", StringComparison.Ordinal))
                    {
                        targetMode = sibling["Value"]?.ToString() ?? string.Empty;
                    }
                    else if (string.Equals(name, "DLCID", StringComparison.Ordinal))
                    {
                        dlcId = sibling["Value"]?.ToString() ?? string.Empty;
                    }
                }
            }

            if (rows.All(r => r.PlaneStringID != planeId))
            {
                var isDisabledInAsset = string.Equals(targetMode, "EPlaneTargetMode::None", StringComparison.Ordinal)
                    && string.Equals(dlcId, "DummyId", StringComparison.Ordinal);
                rows.Add(new PlaneDataRow
                {
                    PlaneStringID = planeId,
                    Enabled = !isDisabledInAsset,
                    PlaneID = planeNumericId,
                    OriginalPlaneID = originalPlaneId,
                    TargetMode = targetMode,
                    DLCID = dlcId
                });
            }
        }

        log($"Extracted {rows.Count} unique PlaneStringID entries from JSON.");
        return rows;
    }

    /// <param name="originalDlcIds">Optional map of PlaneStringID to DLCID to restore when re-enabling (e.g. from DisableEnabler_plane_states.json). If null or missing key, falls back to current row DLCID.</param>
    public static void ApplyEnableFlagsToJson(string jsonPath, IEnumerable<PlaneDataRow> rows, IReadOnlyDictionary<string, string>? originalDlcIds, Action<string> log)
    {
        var text = File.ReadAllText(jsonPath);
        var root = JObject.Parse(text);

        var rowsList = rows.ToList();
        var byId = rowsList.ToDictionary(r => r.PlaneStringID, r => r.Enabled);
        var dlcById = rowsList.ToDictionary(r => r.PlaneStringID, r => r.DLCID, StringComparer.OrdinalIgnoreCase);

        foreach (var rowToken in root.SelectTokens("$..[?(@.Name=='PlaneStringID')]").OfType<JObject>())
        {
            var valueToken = rowToken["Value"];
            var planeId = valueToken?.ToString();
            if (string.IsNullOrWhiteSpace(planeId))
                continue;

            if (!byId.TryGetValue(planeId, out var enabled))
                continue;

            var isVr = planeId.EndsWith("_vr", StringComparison.OrdinalIgnoreCase);
            var targetModeValue = enabled
                ? (isVr ? "EPlaneTargetMode::VR" : "EPlaneTargetMode::CampaignAndOnline")
                : "EPlaneTargetMode::None";
            var dlcIdValue = enabled
                ? (originalDlcIds != null && originalDlcIds.TryGetValue(planeId, out var saved) ? saved : (dlcById.TryGetValue(planeId, out var current) ? current : string.Empty))
                : "DummyId";

            var parentArray = rowToken.Parent as JArray;
            if (parentArray == null)
                continue;

            foreach (var sibling in parentArray.OfType<JObject>())
            {
                var name = sibling["Name"]?.ToString();
                if (string.Equals(name, "TargetMode", StringComparison.Ordinal))
                {
                    sibling["Value"] = targetModeValue;
                }
                else if (string.Equals(name, "DLCID", StringComparison.Ordinal))
                {
                    sibling["Value"] = dlcIdValue;
                }
            }
        }

        File.WriteAllText(jsonPath, root.ToString());
        log($"Applied enable/disable flags to JSON at {jsonPath}");
    }

    public static void SaveJsonBackToUAsset(string assetPath, string jsonPath, Action<string> log)
    {
        var jsonText = File.ReadAllText(jsonPath);

        // Ensure the NameMap contains TargetMode enum values we write when applying (None, VR, CampaignAndOnline).
        try
        {
            var root = JObject.Parse(jsonText);
            if (root["NameMap"] is JArray nameMapArray)
            {
                var existing = new HashSet<string>(nameMapArray.Values<string>().OfType<string>(), StringComparer.Ordinal);
                var toAdd = new[] { "EPlaneTargetMode::None", "EPlaneTargetMode::VR", "EPlaneTargetMode::CampaignAndOnline" };
                var added = false;
                foreach (var name in toAdd)
                {
                    if (!existing.Contains(name))
                    {
                        nameMapArray.Add(name);
                        existing.Add(name);
                        added = true;
                    }
                }
                if (added)
                {
                    jsonText = root.ToString();
                    File.WriteAllText(jsonPath, jsonText);
                    log("Added TargetMode enum value(s) to NameMap in JSON and UAsset.");
                }
            }
        }
        catch (Exception)
        {
            log("Warning: Failed to add TargetMode enum values to NameMap; proceeding without modification.");
        }

        var asset = UAsset.DeserializeJson(jsonText);
        asset.Write(assetPath);

        log($"Saved modified PlayerPlaneDataTable.uasset back to {assetPath}");
    }
}

