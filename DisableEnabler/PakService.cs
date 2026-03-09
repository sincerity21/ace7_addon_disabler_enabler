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

    public static void CreatePak(string unrealPakExe, string sourceDir, string outputPakPath, string internalRelativePath, Action<string> log)
    {
        // internalRelativePath is something like "Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset"
        var fileListPath = Path.Combine(sourceDir, "filelist_disable_enabler.txt");
        var sourceAssetPath = Path.Combine(sourceDir, "PlayerPlaneDataTable.uasset");

        if (!File.Exists(sourceAssetPath))
        {
            throw new FileNotFoundException("Modified PlayerPlaneDataTable.uasset not found in sourceDir.", sourceAssetPath);
        }

        var line = $"\"{sourceAssetPath}\" \"{internalRelativePath}\"";
        File.WriteAllText(fileListPath, line, Encoding.UTF8);
        log($"Created file list at {fileListPath} with mapping: {line}");

        var args = $"\"{outputPakPath}\" -create=\"{fileListPath}\"";
        log($"Running UnrealPak create: {unrealPakExe} {args}");

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
            throw new InvalidOperationException($"UnrealPak create failed with exit code {proc.ExitCode}.");
        }
    }
}

