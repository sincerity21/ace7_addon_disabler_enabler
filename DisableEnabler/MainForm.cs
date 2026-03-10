using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;

namespace DisableEnabler;

public partial class MainForm : Form
{
    private readonly BindingList<PlaneDataRow> _planes = new();
    private readonly List<PlaneDataRow> _allPlanes = new();
    private readonly HashSet<int> _selectedRowIndexes = new();
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DisableEnabler.config");
    private bool _isDarkMode;

    public MainForm()
    {
        InitializeComponent();
        planesGrid.AutoGenerateColumns = false;
        planesGrid.DataSource = _planes;
        LoadConfig();
        ApplyTheme();
    }

    private void darkModeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        _isDarkMode = darkModeCheckBox.Checked;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var formBackColor = _isDarkMode ? Color.FromArgb(38, 38, 38) : SystemColors.Control;
        var formForeColor = _isDarkMode ? Color.WhiteSmoke : SystemColors.ControlText;

        BackColor = formBackColor;
        ForeColor = formForeColor;

        ApplyThemeToControl(this);

        if (planesGrid != null)
        {
            ApplyThemeToDataGridView(planesGrid);
        }
    }

    private void ApplyThemeToControl(Control control)
    {
        if (control is DataGridView)
        {
            // Handled separately
        }
        else         if (control is TextBox)
        {
            control.BackColor = _isDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(252, 252, 252);
            control.ForeColor = _isDarkMode ? Color.WhiteSmoke : SystemColors.WindowText;
        }
        else if (control is Button btn)
        {
            btn.BackColor = _isDarkMode ? Color.FromArgb(50, 50, 50) : SystemColors.Control;
            btn.ForeColor = _isDarkMode ? Color.WhiteSmoke : SystemColors.ControlText;
            if (btn.FlatStyle == FlatStyle.Flat)
            {
                btn.FlatAppearance.BorderColor = _isDarkMode ? Color.FromArgb(70, 70, 70) : SystemColors.ControlDark;
                btn.FlatAppearance.BorderSize = 1;
            }
        }
        else if (control is CheckBox)
        {
            control.BackColor = _isDarkMode ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
            control.ForeColor = _isDarkMode ? Color.WhiteSmoke : SystemColors.ControlText;
        }
        else
        {
            control.BackColor = _isDarkMode ? Color.FromArgb(38, 38, 38) : SystemColors.Control;
            control.ForeColor = _isDarkMode ? Color.WhiteSmoke : SystemColors.ControlText;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private void ApplyThemeToDataGridView(DataGridView grid)
    {
        if (_isDarkMode)
        {
            grid.BackgroundColor = Color.FromArgb(38, 38, 38);
            grid.GridColor = Color.FromArgb(60, 60, 60);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            grid.DefaultCellStyle.ForeColor = Color.WhiteSmoke;
            grid.DefaultCellStyle.SelectionBackColor = Color.SteelBlue;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.WhiteSmoke;
            grid.EnableHeadersVisualStyles = false;
        }
        else
        {
            grid.BackgroundColor = Color.FromArgb(252, 252, 252);
            grid.GridColor = Color.FromArgb(220, 220, 220);
            grid.DefaultCellStyle.BackColor = SystemColors.Window;
            grid.DefaultCellStyle.ForeColor = SystemColors.ControlText;
            grid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
            grid.EnableHeadersVisualStyles = false;
        }
    }

    private void filterCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        ApplyPlaneFilters();
    }

    private void planesGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (planesGrid.CurrentCell is DataGridViewCheckBoxCell)
        {
            planesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void planesGrid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        _selectedRowIndexes.Clear();

        if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        // Only customize behavior for the Enabled checkbox column.
        if (planesGrid.Columns[e.ColumnIndex].Name != "Enabled")
            return;

        var mods = ModifierKeys;
        var hasModifier = (mods & (Keys.Shift | Keys.Control)) != Keys.None;
        var hasMultiSelection = planesGrid.SelectedRows.Count > 1;

        if (hasModifier || hasMultiSelection)
        {
            foreach (DataGridViewRow row in planesGrid.SelectedRows)
            {
                if (row.Index >= 0)
                {
                    _selectedRowIndexes.Add(row.Index);
                }
            }

            // Ensure the clicked row is included as well.
            _selectedRowIndexes.Add(e.RowIndex);
        }
        else
        {
            // Plain click with single/no selection: only the clicked row should toggle.
            _selectedRowIndexes.Add(e.RowIndex);
        }
    }

    private void planesGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (planesGrid.Columns[e.ColumnIndex].Name != "Enabled")
            return;

        var cellValue = planesGrid[e.ColumnIndex, e.RowIndex].Value;
        if (cellValue is not bool isChecked)
            return;

        var targetIndexes = _selectedRowIndexes.Count > 0
            ? _selectedRowIndexes
            : new HashSet<int> { e.RowIndex };

        foreach (var rowIndex in targetIndexes)
        {
            if (rowIndex < 0 || rowIndex >= planesGrid.Rows.Count)
                continue;

            var row = planesGrid.Rows[rowIndex];
            if (row.DataBoundItem is PlaneDataRow planeRow)
            {
                planeRow.Enabled = isChecked;
            }
        }

        planesGrid.Refresh();
        _selectedRowIndexes.Clear();
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
            SaveConfig();
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
            _allPlanes.Clear();
            var (rows, jsonPath) = PlaneDataService.LoadPlanesToJsonAndRows(assetPath, Log);
            foreach (var row in rows)
            {
                _allPlanes.Add(row);
            }

            ApplyPlaneFilters();

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
        // First apply changes and save the UAsset; only pack if that succeeds.
        if (!ApplyAndSaveUAsset())
        {
            return;
        }

        // Then pack the PAK using the updated asset.
        PackPakFromUpdatedAsset();
    }

    private bool ApplyAndSaveUAsset()
    {
        try
        {
            var pakPath = pakPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var unpackDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

            var assetPath = PlaneDataService.FindPlayerPlaneDataTable(unpackDir);
            var jsonPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? string.Empty, "PlayerPlaneDataTable.json");

            PlaneDataService.ApplyEnableFlagsToJson(jsonPath, _allPlanes, Log);
            PlaneDataService.SaveJsonBackToUAsset(assetPath, jsonPath, Log);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error during Apply & Save UAsset: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool PackPakFromUpdatedAsset()
    {
        try
        {
            var pakPath = pakPathTextBox.Text;
            var unrealPakPath = unrealPakPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show(this, "Select a valid PAK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
            {
                MessageBox.Show(this, "Select a valid UnrealPak.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var pakFileName = Path.GetFileName(pakPath);
            var pakNameWithoutExtension = Path.GetFileNameWithoutExtension(pakFileName);

            if (!pakNameWithoutExtension.EndsWith("_P", StringComparison.Ordinal))
            {
                MessageBox.Show(this, "The selected PAK must end with \"_P.pak\" (for example: MyMod_P.pak).", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var unpackDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

            var baseNameWithoutSuffix = pakNameWithoutExtension[..^2]; // remove trailing "_P"
            var outputFileName = $"{baseNameWithoutSuffix}_DisableEnabler_P.pak";
            var outputPakPath = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty, outputFileName);

            const string internalPath = "../../../Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset";
            PakService.CreatePak(unrealPakPath, unpackDir, outputPakPath, internalPath, Log);

            Log($"Packed new PAK at {outputPakPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error during Pack PAK: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
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

    private void searchTextBox_TextChanged(object? sender, EventArgs e)
    {
        ApplyPlaneFilters();
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return;

            var lines = File.ReadAllLines(ConfigPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key.Equals("UnrealPakPath", StringComparison.OrdinalIgnoreCase) && File.Exists(value))
                {
                    unrealPakPathTextBox.Text = value;
                }
                else if (key.Equals("DarkMode", StringComparison.OrdinalIgnoreCase) &&
                         bool.TryParse(value, out var darkMode))
                {
                    _isDarkMode = darkMode;
                    darkModeCheckBox.Checked = darkMode;
                }
                else if (key.Equals("HideBaseGame", StringComparison.OrdinalIgnoreCase) &&
                         bool.TryParse(value, out var hideBase))
                {
                    hideBaseGameCheckBox.Checked = hideBase;
                }
                else if (key.Equals("HideVrPlanes", StringComparison.OrdinalIgnoreCase) &&
                         bool.TryParse(value, out var hideVr))
                {
                    hideVrPlanesCheckBox.Checked = hideVr;
                }
            }
        }
        catch
        {
            // Ignore config load errors
        }
    }

    private void SaveConfig()
    {
        try
        {
            var sb = new StringBuilder();
            var unrealPakPath = unrealPakPathTextBox.Text;

            if (!string.IsNullOrWhiteSpace(unrealPakPath))
            {
                sb.AppendLine($"UnrealPakPath={unrealPakPath}");
            }

            sb.AppendLine($"DarkMode={_isDarkMode}");
            sb.AppendLine($"HideBaseGame={hideBaseGameCheckBox.Checked}");
            sb.AppendLine($"HideVrPlanes={hideVrPlanesCheckBox.Checked}");

            File.WriteAllText(ConfigPath, sb.ToString());
        }
        catch
        {
            // Ignore config save errors
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveConfig();
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

    private void ApplyPlaneFilters()
    {
        _planes.Clear();

        if (_allPlanes.Count == 0)
            return;

        var hideBase = hideBaseGameCheckBox.Checked;
        var hideVr = hideVrPlanesCheckBox.Checked;

        var searchText = searchTextBox.Text;
        var hasSearch = !string.IsNullOrWhiteSpace(searchText);
        int searchNumeric = 0;
        var searchIsNumeric = false;

        if (hasSearch)
        {
            searchText = searchText.Trim();
            searchIsNumeric = int.TryParse(searchText, out searchNumeric);
        }

        foreach (var plane in _allPlanes)
        {
            var isBaseGame = plane.PlaneID > 0 && plane.PlaneID <= 147;
            var isVr = plane.PlaneStringID.EndsWith("_vr", StringComparison.OrdinalIgnoreCase);

            if ((hideBase && isBaseGame) || (hideVr && isVr))
                continue;

            if (hasSearch)
            {
                var matchesString = plane.PlaneStringID.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                var matchesId = searchIsNumeric
                    ? plane.PlaneID == searchNumeric
                    : plane.PlaneID.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase);

                if (!matchesString && !matchesId)
                    continue;
            }

            _planes.Add(plane);
        }
    }
}

