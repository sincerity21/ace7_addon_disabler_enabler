using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace DisableEnabler;

public partial class MainForm : Form
{
    private readonly BindingList<PlaneDataRow> _planes = new();
    private readonly List<PlaneDataRow> _allPlanes = new();
    private readonly HashSet<int> _selectedRowIndexes = new();
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DisableEnabler.config");
    private bool _isDarkMode;

    /// <summary>Max search bar width so it stops at the toggles (left edge of Dark mode / Hide Base Game).</summary>
    private const int SearchBarMaxWidth = 430 - 12 - 8; // toggle area starts at 430, left margin 12, gap 8
    private const int SearchBarLeft = 12;
    private const int SearchExportGap = 8;
    private const int ExportButtonWidth = 90;
    private const int ImportButtonWidth = 92;

    public MainForm()
    {
        InitializeComponent();
        planesGrid.AutoGenerateColumns = false;
        planesGrid.DataSource = _planes;
        LoadConfig();
        ApplyTheme();
        Resize += MainForm_Resize;
        LayoutSearchAndExportImport();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        LayoutSearchAndExportImport();
    }

    private void LayoutSearchAndExportImport()
    {
        var rightReserved = SearchExportGap + ExportButtonWidth + SearchExportGap + ImportButtonWidth;
        var w = Math.Min(ClientSize.Width - SearchBarLeft - rightReserved - 12, SearchBarMaxWidth); // don't extend past toggles
        if (w < 80) w = 80;
        searchTextBox.Width = w;
        searchTextBox.Left = SearchBarLeft;
        var x = SearchBarLeft + w + SearchExportGap;
        exportStateButton.Left = x;
        exportStateButton.Top = 118;
        importStateButton.Left = x + ExportButtonWidth + SearchExportGap;
        importStateButton.Top = 118;
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

            // If the packed state list exists (from a previous Apply, Save and Pack), apply it so state is restored
            var stateJsonPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? unpackDir, "DisableEnabler_plane_states.json");
            if (File.Exists(stateJsonPath))
            {
                try
                {
                    var stateJson = File.ReadAllText(stateJsonPath);
                    var entries = JsonConvert.DeserializeObject<List<PlaneStateExportEntry>>(stateJson);
                    if (entries != null && entries.Count > 0)
                    {
                        var byId = entries.ToDictionary(e => e.PlaneStringID, e => e, StringComparer.OrdinalIgnoreCase);
                        var updated = 0;
                        foreach (var plane in _allPlanes)
                        {
                            if (byId.TryGetValue(plane.PlaneStringID, out var entry))
                            {
                                plane.Enabled = entry.Enabled;
                                if (!string.IsNullOrEmpty(entry.OriginalDLCID))
                                    plane.DLCID = entry.OriginalDLCID;
                                updated++;
                            }
                        }
                        Log($"Applied packed state from DisableEnabler_plane_states.json ({updated} planes).");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Could not apply packed state: {ex.Message}");
                }
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
            var assetDir = Path.GetDirectoryName(assetPath) ?? unpackDir;
            var jsonPath = Path.Combine(assetDir, "PlayerPlaneDataTable.json");

            var stateJsonPath = Path.Combine(assetDir, "DisableEnabler_plane_states.json");
            var originalDlcIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(stateJsonPath))
            {
                try
                {
                    var stateJson = File.ReadAllText(stateJsonPath);
                    var stateEntries = JsonConvert.DeserializeObject<List<PlaneStateExportEntry>>(stateJson);
                    if (stateEntries != null)
                        foreach (var e in stateEntries.Where(e => !string.IsNullOrEmpty(e.OriginalDLCID)))
                            originalDlcIds[e.PlaneStringID] = e.OriginalDLCID!;
                }
                catch (Exception ex)
                {
                    Log($"Could not load state JSON for original DLCIDs: {ex.Message}");
                }
            }
            foreach (var p in _allPlanes)
            {
                if (!string.IsNullOrEmpty(p.DLCID) && !originalDlcIds.ContainsKey(p.PlaneStringID))
                    originalDlcIds[p.PlaneStringID] = p.DLCID;
            }

            PlaneDataService.ApplyEnableFlagsToJson(jsonPath, _allPlanes, originalDlcIds, Log);
            PlaneDataService.SaveJsonBackToUAsset(assetPath, jsonPath, Log);

            // Save the same importable state JSON into the unpack folder so it gets packed and can be re-imported after unpack.
            var entriesToSave = _allPlanes
                .Select(p => new PlaneStateExportEntry
                {
                    PlaneStringID = p.PlaneStringID,
                    Enabled = p.Enabled,
                    OriginalDLCID = string.IsNullOrEmpty(p.DLCID) ? null : p.DLCID
                })
                .ToList();
            var jsonToWrite = JsonConvert.SerializeObject(entriesToSave, Formatting.Indented);
            File.WriteAllText(stateJsonPath, jsonToWrite);
            Log($"Saved plane state list to {stateJsonPath}");

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
            // If the mod is already a DisableEnabler output, don't add the suffix again
            var outputFileName = baseNameWithoutSuffix.EndsWith("_DisableEnabler", StringComparison.OrdinalIgnoreCase)
                ? $"{baseNameWithoutSuffix}_P.pak"
                : $"{baseNameWithoutSuffix}_DisableEnabler_P.pak";
            var outputPakPath = Path.Combine(Path.GetDirectoryName(pakPath) ?? string.Empty, outputFileName);

            const string internalPath = "../../../Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset";
            var assetPath = PlaneDataService.FindPlayerPlaneDataTable(unpackDir);
            var stateJsonPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? unpackDir, "DisableEnabler_plane_states.json");
            PakService.CreatePak(unrealPakPath, unpackDir, outputPakPath, internalPath, stateJsonPath, Log);

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

    private void exportStateButton_Click(object? sender, EventArgs e)
    {
        if (_allPlanes.Count == 0)
        {
            MessageBox.Show(this, "Load plane data first (Unpack & Load).", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = "DisableEnabler_plane_states.json"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var entries = _allPlanes
                .Select(p => new PlaneStateExportEntry
                {
                    PlaneStringID = p.PlaneStringID,
                    Enabled = p.Enabled,
                    OriginalDLCID = string.IsNullOrEmpty(p.DLCID) ? null : p.DLCID
                })
                .ToList();
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(sfd.FileName, json);
            Log($"Exported {entries.Count} plane states to {sfd.FileName}");
            MessageBox.Show(this, $"Exported {entries.Count} plane states.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Export failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void importStateButton_Click(object? sender, EventArgs e)
    {
        if (_allPlanes.Count == 0)
        {
            MessageBox.Show(this, "Load plane data first (Unpack & Load).", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var ofd = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var json = File.ReadAllText(ofd.FileName);
            var entries = JsonConvert.DeserializeObject<List<PlaneStateExportEntry>>(json);
            if (entries == null || entries.Count == 0)
            {
                MessageBox.Show(this, "No entries in file or invalid format.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var byId = entries.ToDictionary(e => e.PlaneStringID, e => e, StringComparer.OrdinalIgnoreCase);
            var updated = 0;
            foreach (var plane in _allPlanes)
            {
                if (byId.TryGetValue(plane.PlaneStringID, out var entry))
                {
                    plane.Enabled = entry.Enabled;
                    if (!string.IsNullOrEmpty(entry.OriginalDLCID))
                        plane.DLCID = entry.OriginalDLCID;
                    updated++;
                }
            }

            ApplyPlaneFilters();
            planesGrid.Refresh();
            Log($"Imported plane states from {ofd.FileName}; updated {updated} of {_allPlanes.Count} planes.");
            MessageBox.Show(this, $"Updated {updated} plane(s) from import.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Import failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

