using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace DisableEnabler;

public partial class MainForm : Form
{
    private readonly BindingList<PlaneDataRow> _planes = new();

    public MainForm()
    {
        InitializeComponent();
        planesGrid.AutoGenerateColumns = false;
        planesGrid.DataSource = _planes;
    }

    private void browsePakButton_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Pak files (*.pak)|*.pak|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            pakPathTextBox.Text = ofd.FileName;
            Log($"Selected PAK: {ofd.FileName}");
        }
    }

    private void browseUnrealPakButton_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "UnrealPak.exe|UnrealPak.exe|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            unrealPakPathTextBox.Text = ofd.FileName;
            Log($"Selected UnrealPak.exe: {ofd.FileName}");
        }
    }

    private void unpackAndLoadButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var pakPath = pakPathTextBox.Text;
            var unrealPakPath = unrealPakPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
            {
                MessageBox.Show(this, "Select a valid UnrealPak.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var unpackDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

            PakService.ExtractPak(unrealPakPath, pakPath, unpackDir, Log);

            var assetPath = PlaneDataService.FindPlayerPlaneDataTable(unpackDir);
            Log($"Found PlayerPlaneDataTable.uasset at {assetPath}");

            _planes.Clear();
            var (rows, jsonPath) = PlaneDataService.LoadPlanesToJsonAndRows(assetPath, Log);
            foreach (var row in rows)
            {
                _planes.Add(row);
            }

            Log($"Loaded {rows.Count} planes from {jsonPath}");
        }
        catch (Exception ex)
        {
            Log($"Error during Unpack & Load: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void applyAndSaveButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var pakPath = pakPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var unpackDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

            var assetPath = PlaneDataService.FindPlayerPlaneDataTable(unpackDir);
            var jsonPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? string.Empty, "PlayerPlaneDataTable.json");

            PlaneDataService.ApplyEnableFlagsToJson(jsonPath, _planes, Log);
            PlaneDataService.SaveJsonBackToUAsset(assetPath, jsonPath, Log);
        }
        catch (Exception ex)
        {
            Log($"Error during Apply & Save UAsset: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void packPakButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var pakPath = pakPathTextBox.Text;
            var unrealPakPath = unrealPakPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
            {
                MessageBox.Show(this, "Select a valid UnrealPak.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var unpackDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

            var outputPakPath = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_DisableEnabler.pak");

            const string internalPath = "Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset";
            PakService.CreatePak(unrealPakPath, unpackDir, outputPakPath, internalPath, Log);

            Log($"Packed new PAK at {outputPakPath}");
        }
        catch (Exception ex)
        {
            Log($"Error during Pack PAK: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void openOutputFolderButton_Click(object? sender, EventArgs e)
    {
        var pakPath = pakPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
        {
            MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var outputDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

        if (!Directory.Exists(outputDir))
        {
            MessageBox.Show(this, "Output folder does not exist yet.", "Info", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Process.Start("explorer.exe", outputDir);
        Log($"Opened output folder: {outputDir}");
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (logTextBox.TextLength == 0)
        {
            logTextBox.Text = line;
        }
        else
        {
            logTextBox.AppendText(Environment.NewLine + line);
        }
    }
}

