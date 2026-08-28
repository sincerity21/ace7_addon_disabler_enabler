using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DisableEnabler;

public static class AddonDatabaseService
{
    private const string DatabaseFileName = "addon_database.json";
    private const string DefaultRemoteUrl =
        "https://raw.githubusercontent.com/sincerity21/ace7_addon_disabler_enabler/main/DisableEnabler/addon_database.json";

    private static readonly string LocalPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);

    private static AddonDatabaseFile _cached = new();
    private static string? _remoteUrlOverride;

    public static int Revision => _cached.Revision;

    public static int EntryCount => _cached.Planes.Count;

    public static void SetRemoteUrlOverride(string? url)
    {
        _remoteUrlOverride = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    public static AddonDatabaseFile LoadLocal(Action<string>? log = null)
    {
        if (!File.Exists(LocalPath))
        {
            log?.Invoke($"Addon database not found at {LocalPath}; using empty catalog.");
            _cached = new AddonDatabaseFile();
            return _cached;
        }

        try
        {
            var json = File.ReadAllText(LocalPath);
            _cached = JsonConvert.DeserializeObject<AddonDatabaseFile>(json) ?? new AddonDatabaseFile();
            if (_cached.Planes == null)
                _cached.Planes = new Dictionary<string, AddonPlaneEntry>();

            log?.Invoke($"Loaded addon database revision {_cached.Revision} ({_cached.Planes.Count} entries).");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Could not load addon database: {ex.Message}");
            _cached = new AddonDatabaseFile();
        }

        return _cached;
    }

    public static async Task<bool> TryUpdateFromRemoteAsync(Action<string>? log = null)
    {
        var remoteUrl = _remoteUrlOverride ?? DefaultRemoteUrl;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var separator = remoteUrl.Contains('?') ? "&" : "?";
            var cacheBustedUrl = $"{remoteUrl}{separator}_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            using var request = new HttpRequestMessage(HttpMethod.Get, cacheBustedUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            request.Headers.Pragma.ParseAdd("no-cache");

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var remote = JsonConvert.DeserializeObject<AddonDatabaseFile>(json);
            if (remote == null || remote.Planes == null)
            {
                log?.Invoke("Remote addon database response was invalid; keeping local copy.");
                return false;
            }

            var localRevision = File.Exists(LocalPath)
                ? ReadRevisionFromDisk()
                : 0;

            if (remote.Revision <= localRevision)
            {
                log?.Invoke($"Addon database up to date (revision {localRevision}).");
                return false;
            }

            var tempPath = LocalPath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(LocalPath))
                File.Delete(LocalPath);
            File.Move(tempPath, LocalPath);

            _cached = remote;
            log?.Invoke($"Updated addon database to revision {remote.Revision} ({remote.Planes.Count} entries).");
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Addon database update check failed: {ex.Message}");
            return false;
        }
    }

    public static int Enrich(IEnumerable<PlaneDataRow> rows)
    {
        var lookup = new Dictionary<string, AddonPlaneEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, entry) in _cached.Planes)
            lookup[key] = entry;

        var enriched = 0;
        foreach (var row in rows)
        {
            row.PlaneName = string.Empty;
            row.ModText = string.Empty;
            row.ModUrl = string.Empty;

            if (!lookup.TryGetValue(row.PlaneStringID, out var entry))
                continue;

            row.PlaneName = entry.PlaneName ?? string.Empty;

            var notes = entry.Notes?.Trim() ?? string.Empty;
            var url = entry.URL?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(url))
            {
                row.ModUrl = url;
                row.ModText = !string.IsNullOrEmpty(notes) ? notes : url;
            }
            else if (!string.IsNullOrEmpty(notes))
            {
                row.ModText = notes;
            }

            enriched++;
        }

        return enriched;
    }

    private static int ReadRevisionFromDisk()
    {
        try
        {
            var json = File.ReadAllText(LocalPath);
            var file = JsonConvert.DeserializeObject<AddonDatabaseFile>(json);
            return file?.Revision ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
