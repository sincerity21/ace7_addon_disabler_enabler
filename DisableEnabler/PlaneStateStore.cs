using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DisableEnabler;

public static class PlaneStateStore
{
    public static string SidecarPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DisableEnabler_plane_states.json");

    public static bool Exists() => File.Exists(SidecarPath);

    public static List<PlaneStateExportEntry>? Load(Action<string> log)
    {
        if (!Exists())
            return null;

        try
        {
            var json = File.ReadAllText(SidecarPath);
            var entries = JsonConvert.DeserializeObject<List<PlaneStateExportEntry>>(json);
            if (entries == null || entries.Count == 0)
            {
                log($"Plane state sidecar is empty: {SidecarPath}");
                return null;
            }

            log($"Loaded plane state sidecar ({entries.Count} entries) from {SidecarPath}");
            return entries;
        }
        catch (Exception ex)
        {
            log($"Could not load plane state sidecar from {SidecarPath}: {ex.Message}");
            return null;
        }
    }

    public static void Save(IEnumerable<PlaneStateExportEntry> entries, Action<string> log)
    {
        var list = entries.ToList();
        var json = JsonConvert.SerializeObject(list, Formatting.Indented);
        File.WriteAllText(SidecarPath, json);
        log($"Saved plane state sidecar ({list.Count} entries) to {SidecarPath}");
    }

    public static void CopyTo(string destPath, Action<string> log)
    {
        if (!Exists())
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");
        File.Copy(SidecarPath, destPath, overwrite: true);
        log($"Copied plane state sidecar to {destPath}");
    }

    public static int ApplyToPlanes(IEnumerable<PlaneDataRow> planes, Action<string> log)
    {
        var entries = Load(log);
        if (entries == null || entries.Count == 0)
            return 0;

        var byId = entries.ToDictionary(e => e.PlaneStringID, e => e, StringComparer.OrdinalIgnoreCase);
        var updated = 0;
        foreach (var plane in planes)
        {
            if (!byId.TryGetValue(plane.PlaneStringID, out var entry))
                continue;

            plane.Enabled = entry.Enabled;
            if (!string.IsNullOrEmpty(entry.OriginalDLCID))
                plane.DLCID = entry.OriginalDLCID;
            updated++;
        }

        log($"Applied plane state sidecar to {updated} plane(s).");
        return updated;
    }

    public static Dictionary<string, string> BuildOriginalDlcIdMap(Action<string> log)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entries = Load(log);
        if (entries == null)
            return map;

        foreach (var entry in entries.Where(e => !string.IsNullOrEmpty(e.OriginalDLCID)))
            map[entry.PlaneStringID] = entry.OriginalDLCID!;

        return map;
    }
}
