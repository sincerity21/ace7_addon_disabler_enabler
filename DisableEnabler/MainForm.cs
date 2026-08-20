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

    public MainForm()
    {
        InitializeComponent();
        planesGrid.AutoGenerateColumns = false;
        planesGrid.DataSource = _planes;
        LoadConfig();
        ApplyTheme();
        Shown += (_, _) => EnsureCheckboxColumnFitsDpi();
    }

    /// <summary>
    /// At &gt;100% DPI, DataGridViewCheckBoxColumn stays at MinimumWidth 30 while
    /// checkbox PreferredSize grows, so glyphs clip and the column looks empty.
    /// </summary>
    private void EnsureCheckboxColumnFitsDpi()
    {
        if (planesGrid.Columns["Enabled"] is not DataGridViewCheckBoxColumn enabledCol)
            return;

        enabledCol.HeaderText = "Enabled";

        // Designer used 30px at 96 DPI; scale for the glyph, then grow enough for the header text.
        var minW = Math.Max(48, (int)Math.Ceiling(48.0 * DeviceDpi / 96.0));
        var headerFont = planesGrid.ColumnHeadersDefaultCellStyle.Font ?? planesGrid.Font;
        var headerNeed = TextRenderer.MeasureText(
            enabledCol.HeaderText,
            headerFont,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 20;
        minW = Math.Max(minW, headerNeed);

        enabledCol.MinimumWidth = minW;
        enabledCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        enabledCol.Width = minW;

        var rowH = Math.Max(28, (int)Math.Ceiling(28.0 * DeviceDpi / 96.0));
        planesGrid.RowTemplate.Height = rowH;
        foreach (DataGridViewRow row in planesGrid.Rows)
        {
            row.Height = rowH;
        }
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

    private void scanModsFolderButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var unrealPakPath = unrealPakPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
            {
                MessageBox.Show(this, "Select a valid UnrealPak.exe first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = "Select a folder that contains PAK file(s)",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                return;

            var chosenFolder = fbd.SelectedPath;
            Log($"Choosing PAK from folder: {chosenFolder}");

            string? winningPak;
            try
            {
                winningPak = PakService.FindWinningPlanePakInFolder(unrealPakPath, chosenFolder, Log);
            }
            catch (Exception ex)
            {
                Log($"Error while choosing PAK: {ex.Message}");
                MessageBox.Show(this, ex.Message, "Choose PAK failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(winningPak))
            {
                MessageBox.Show(this,
                    "No PAKs with PlayerPlaneDataTable.uasset were found in the selected folder.",
                    "No matching PAK",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            pakPathTextBox.Text = winningPak;
            Log($"Using winning plane PAK: {winningPak}. You can now click \"Unpack && Load\".");
        }
        catch (Exception ex)
        {
            Log($"Unexpected error during Choose PAK: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var unpackDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

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

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var unpackDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

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

            var baseNameWithoutSuffix = pakNameWithoutExtension[..^2]; // remove trailing "_P"
            // If the mod is already a DisableEnabler output, don't add the suffix again
            var outputFileName = baseNameWithoutSuffix.EndsWith("_DisableEnabler", StringComparison.OrdinalIgnoreCase)
                ? $"{baseNameWithoutSuffix}_P.pak"
                : $"{baseNameWithoutSuffix}_DisableEnabler_P.pak";

            // If the chosen PAK name already has leading tildes, add 5 more so
            // the generated output wins load-order priority over the original.
            var hasLeadingTildes = !string.IsNullOrEmpty(pakNameWithoutExtension) && pakNameWithoutExtension[0] == '~';
            var isAlreadyDisableEnabler = baseNameWithoutSuffix.EndsWith("_DisableEnabler", StringComparison.OrdinalIgnoreCase);
            if (hasLeadingTildes && !isAlreadyDisableEnabler)
            {
                outputFileName = new string('~', 5) + outputFileName;
            }
            // Place the packed PAK inside the program's unpack folder alongside the extracted files.
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var unpackDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");
            var outputPakPath = Path.Combine(unpackDir, outputFileName);

            var assetPath = PlaneDataService.FindPlayerPlaneDataTable(unpackDir);
            var assetDir = Path.GetDirectoryName(assetPath) ?? unpackDir;
            var stateJsonPath = Path.Combine(assetDir, "DisableEnabler_plane_states.json");
            // Use the original internal path that matches stock AC7 layouts
            const string internalPath = "../../../Nimbus/Content/Blueprint/Information/PlayerPlaneDataTable.uasset";
            PakService.CreatePak(unrealPakPath, assetDir, outputPakPath, internalPath, stateJsonPath, Log);

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

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var outputDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");

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

        EnsureCheckboxColumnFitsDpi();
    }
}

