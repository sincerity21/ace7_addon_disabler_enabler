using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DisableEnabler;

public static class PakService
{
    private const string PlaneTableMarker = "PlayerPlaneDataTable.uasset";
    private const string StateJsonMarker = "DisableEnabler_plane_states.json";

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

    public static bool IsDisableEnablerPakByFileName(string pakPath)
    {
        var name = Path.GetFileNameWithoutExtension(pakPath);
        if (string.IsNullOrEmpty(name) || !name.EndsWith("_P", StringComparison.OrdinalIgnoreCase))
            return false;

        var baseName = name[..^2].TrimStart('~');
        return baseName.EndsWith("_DisableEnabler", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDisableEnablerPak(string pakPath, string unrealPakExe, Action<string> log)
    {
        if (IsDisableEnablerPakByFileName(pakPath))
            return true;

        return PakContainsFile(unrealPakExe, pakPath, StateJsonMarker, log);
    }

    public static PakScanSelection FindWinningPlanePakInFolder(string unrealPakExe, string modsFolder, Action<string> log)
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
            return new PakScanSelection();
        }

        log($"Found {pakFiles.Count} .pak file(s) in folder: {modsFolder}");

        var sourceCandidates = new List<string>();
        var deCandidates = new List<string>();

        foreach (var pak in pakFiles)
        {
            if (!PakContainsFile(unrealPakExe, pak, PlaneTableMarker, log))
                continue;

            if (IsDisableEnablerPak(pak, unrealPakExe, log))
            {
                deCandidates.Add(pak);
                log($"Skipped DisableEnabler output PAK (blacklisted): {pak}");
                continue;
            }

            sourceCandidates.Add(pak);
            log($"Plane table found in PAK (source candidate): {pak}");
        }

        if (sourceCandidates.Count == 0)
        {
            if (deCandidates.Count > 0)
            {
                log("No non-DisableEnabler PPDT source PAK was found. Only DisableEnabler output PAK(s) contain PlayerPlaneDataTable.uasset.");
            }
            else
            {
                log("No PAKs with PlayerPlaneDataTable.uasset were found in this folder.");
            }

            return new PakScanSelection { BlacklistedDePaks = deCandidates };
        }

        var winningPak = sourceCandidates[^1];
        log($"Selected winning plane PAK (last in load order): {winningPak}");
        return new PakScanSelection
        {
            SourcePak = winningPak,
            BlacklistedDePaks = deCandidates
        };
    }

    public static bool TryExtractFileFromPak(
        string unrealPakExe,
        string pakPath,
        string fileNameSubstring,
        string destFilePath,
        Action<string> log)
    {
        if (!PakContainsFile(unrealPakExe, pakPath, fileNameSubstring, log))
        {
            log($"File matching '{fileNameSubstring}' was not found in PAK: {pakPath}");
            return false;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"DisableEnabler_migrate_{Guid.NewGuid():N}");
        try
        {
            ExtractPak(unrealPakExe, pakPath, tempDir, log);

            var matches = Directory.GetFiles(tempDir, $"*{Path.GetFileName(fileNameSubstring)}", SearchOption.AllDirectories);
            if (matches.Length == 0)
            {
                log($"Extracted PAK but could not find '{fileNameSubstring}' on disk under {tempDir}");
                return false;
            }

            var sourceFile = matches[0];
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath) ?? ".");
            File.Copy(sourceFile, destFilePath, overwrite: true);
            log($"Extracted {fileNameSubstring} from {pakPath} to {destFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            log($"Failed to extract '{fileNameSubstring}' from {pakPath}: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                log($"Could not delete temp migration folder {tempDir}: {ex.Message}");
            }
        }
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
