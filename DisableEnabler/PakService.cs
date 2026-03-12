using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DisableEnabler;

public static class PakService
{
    public static bool PakContainsFile(string unrealPakExe, string pakPath, string fileNameSubstring, Action<string> log)
    {
        var args = $"\"{pakPath}\" -list";
        log($"Listing contents of PAK: {unrealPakExe} {args}");

        var psi = new ProcessStartInfo
        {
            FileName = unrealPakExe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start UnrealPak process.");
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (!string.IsNullOrWhiteSpace(error))
            log(error.Trim());

        if (proc.ExitCode != 0)
        {
            log($"UnrealPak -list exited with code {proc.ExitCode} for {pakPath}");
            return false;
        }

        if (string.IsNullOrEmpty(output))
            return false;

        // Normalize search to forward slashes and case-insensitive contains check.
        var normalizedSearch = fileNameSubstring.Replace('\\', '/');
        using var reader = new StringReader(output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var normalizedLine = line.Replace('\\', '/');
            if (normalizedLine.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    public static string? FindWinningPlanePakInFolder(string unrealPakExe, string modsFolder, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(modsFolder) || !Directory.Exists(modsFolder))
        {
            throw new DirectoryNotFoundException($"Mods folder not found: {modsFolder}");
        }

        // Search all subdirectories as managers often deploy PAKs inside per-mod folders.
        // Sort by full path so directory prefixes (e.g. \"AAU-...\" vs \"AAQ-...\") affect load order like the engine.
        var pakFiles = Directory.GetFiles(modsFolder, "*.pak", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pakFiles.Count == 0)
        {
            log($"No .pak files found in folder: {modsFolder}");
            return null;
        }

        log($"Found {pakFiles.Count} .pak file(s) in folder: {modsFolder}");

        var candidates = new List<string>();
        // Match by file name so we don't depend on the exact internal path printed by UnrealPak -list.
        const string planeTableMarker = "PlayerPlaneDataTable.uasset";

        foreach (var pak in pakFiles)
        {
            if (PakContainsFile(unrealPakExe, pak, planeTableMarker, log))
            {
                candidates.Add(pak);
                log($"Plane table found in PAK (candidate): {pak}");
            }
        }

        if (candidates.Count == 0)
        {
            log("No PAKs with PlayerPlaneDataTable.uasset were found in this folder.");
            return null;
        }

        var winningPak = candidates[^1];
        log($"Selected winning plane PAK (last in load order): {winningPak}");
        return winningPak;
    }

    public static void ExtractPak(string unrealPakExe, string pakPath, string extractDir, Action<string> log)
    {
        Directory.CreateDirectory(extractDir);

        var args = $"\"{pakPath}\" -extract \"{extractDir}\"";
        log($"Running UnrealPak extract: {unrealPakExe} {args}");

        var psi = new ProcessStartInfo
        {
            FileName = unrealPakExe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start UnrealPak process.");
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
            log(output.Trim());
        if (!string.IsNullOrWhiteSpace(error))
            log(error.Trim());

        log($"UnrealPak exited with code {proc.ExitCode}");

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"UnrealPak extract failed with exit code {proc.ExitCode}.");
        }
    }

    public static void CreatePak(string unrealPakExe, string sourceDir, string outputPakPath, string internalRelativePath, string? stateJsonPath, Action<string> log)
    {
        // internalRelativePath is something like "Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset"
        var fileListFileName = "filelist_disable_enabler.txt";
        var unrealPakDir = Path.GetDirectoryName(unrealPakExe) ?? string.Empty;
        if (string.IsNullOrEmpty(unrealPakDir))
        {
            throw new InvalidOperationException("Could not determine UnrealPak.exe directory.");
        }
        var fileListPath = Path.Combine(unrealPakDir, fileListFileName);

        // Expect the three related files in sourceDir
        var baseName = Path.GetFileNameWithoutExtension(internalRelativePath);
        var internalDir = Path.GetDirectoryName(internalRelativePath)?.Replace('\\', '/') ?? string.Empty;
        if (string.IsNullOrEmpty(internalDir))
        {
            throw new InvalidOperationException("internalRelativePath must include a directory path.");
        }

        var sourceUassetPath = Path.Combine(sourceDir, $"{baseName}.uasset");
        if (!File.Exists(sourceUassetPath))
        {
            throw new FileNotFoundException("Modified PlayerPlaneDataTable.uasset not found in sourceDir.", sourceUassetPath);
        }

        var sourceUexpPath = Path.Combine(sourceDir, $"{baseName}.uexp");
        var sourceJsonPath = Path.Combine(sourceDir, $"{baseName}.json");

        var sb = new StringBuilder();
        // Always include .uasset
        sb.AppendLine($"\"{sourceUassetPath}\" \"{internalDir}/{baseName}.uasset\"");

        // Include .uexp if present
        if (File.Exists(sourceUexpPath))
        {
            sb.AppendLine($"\"{sourceUexpPath}\" \"{internalDir}/{baseName}.uexp\"");
        }

        // Include .json if present
        if (File.Exists(sourceJsonPath))
        {
            sb.AppendLine($"\"{sourceJsonPath}\" \"{internalDir}/{baseName}.json\"");
        }

        // Include DisableEnabler plane state JSON (same format as Import) so it survives unpack/repack
        if (!string.IsNullOrEmpty(stateJsonPath) && File.Exists(stateJsonPath))
        {
            sb.AppendLine($"\"{stateJsonPath}\" \"{internalDir}/DisableEnabler_plane_states.json\"");
        }

        File.WriteAllText(fileListPath, sb.ToString(), Encoding.UTF8);
        log($"Created file list at {fileListPath} for base '{baseName}' in '{internalDir}'");

        var args = $"\"{outputPakPath}\" -create={fileListFileName} -compress";
        log($"Running UnrealPak create: {unrealPakExe} {args}");

        var psi = new ProcessStartInfo
        {
            FileName = unrealPakExe,
            Arguments = args,
            WorkingDirectory = unrealPakDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start UnrealPak process.");
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
            log(output.Trim());
        if (!string.IsNullOrWhiteSpace(error))
            log(error.Trim());

        log($"UnrealPak exited with code {proc.ExitCode}");

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"UnrealPak create failed with exit code {proc.ExitCode}.");
        }
    }
}

