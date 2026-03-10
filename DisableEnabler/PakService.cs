using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DisableEnabler;

public static class PakService
{
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

        var args = $"\"{outputPakPath}\" -create={fileListFileName}";
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

