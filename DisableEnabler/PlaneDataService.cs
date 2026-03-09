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

        // Heuristic: walk all string properties named PlaneStringID
        var tokens = root.SelectTokens("$..[?(@.Name=='PlaneStringID')]");
        foreach (var token in tokens)
        {
            if (token is not JObject obj)
                continue;

            var valueToken = obj["Value"];
            var planeId = valueToken?.ToString();
            if (string.IsNullOrWhiteSpace(planeId))
                continue;

            if (rows.All(r => r.PlaneStringID != planeId))
            {
                rows.Add(new PlaneDataRow
                {
                    PlaneStringID = planeId,
                    Enabled = true
                });
            }
        }

        log($"Extracted {rows.Count} unique PlaneStringID entries from JSON.");
        return rows;
    }

    public static void ApplyEnableFlagsToJson(string jsonPath, IEnumerable<PlaneDataRow> rows, Action<string> log)
    {
        var text = File.ReadAllText(jsonPath);
        var root = JObject.Parse(text);

        var byId = rows.ToDictionary(r => r.PlaneStringID, r => r.Enabled);

        foreach (var rowToken in root.SelectTokens("$..[?(@.Name=='PlaneStringID')]").OfType<JObject>())
        {
            var valueToken = rowToken["Value"];
            var planeId = valueToken?.ToString();
            if (string.IsNullOrWhiteSpace(planeId))
                continue;

            if (!byId.TryGetValue(planeId, out var enabled))
                continue;

            // For this row, find sibling TargetMode and DLCID and patch them
            var parentArray = rowToken.Parent as JArray;
            if (parentArray == null)
                continue;

            foreach (var sibling in parentArray.OfType<JObject>())
            {
                var name = sibling["Name"]?.ToString();
                if (string.Equals(name, "TargetMode", StringComparison.Ordinal))
                {
                    if (!enabled)
                    {
                        sibling["Value"] = "EPlaneTargetMode::None";
                    }
                }
                else if (string.Equals(name, "DLCID", StringComparison.Ordinal))
                {
                    if (!enabled)
                    {
                        sibling["Value"] = "DummyId";
                    }
                }
            }
        }

        File.WriteAllText(jsonPath, root.ToString());
        log($"Applied enable/disable flags to JSON at {jsonPath}");
    }

    public static void SaveJsonBackToUAsset(string assetPath, string jsonPath, Action<string> log)
    {
        var jsonText = File.ReadAllText(jsonPath);
        var asset = UAsset.DeserializeJson(jsonText);
        asset.Write(assetPath);

        log($"Saved modified PlayerPlaneDataTable.uasset back to {assetPath}");
    }
}

