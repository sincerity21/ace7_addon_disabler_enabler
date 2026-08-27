using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace DisableEnabler;

internal enum WorkflowStep
{
    NoPak,
    ReadyToUnpack,
    ReadyToPack
}

public partial class MainForm : Form
{
    private readonly BindingList<PlaneDataRow> _planes = new();
    private readonly List<PlaneDataRow> _allPlanes = new();
    private readonly HashSet<int> _selectedRowIndexes = new();
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DisableEnabler.config");
    private bool _isDarkMode;
    private string? _modsOutputFolder;
    private WorkflowStep _workflowStep = WorkflowStep.NoPak;
    private int _pakScanRunning;
    private bool _pakScanInProgress;
    private Font? _modLinkFont;
    private Color _modLinkColor = Color.Blue;

    public MainForm()
    {
        InitializeComponent();
        planesGrid.AutoGenerateColumns = false;
        planesGrid.DataSource = _planes;
        LoadConfig();
        AddonDatabaseService.LoadLocal(Log);
        _ = CheckAddonDatabaseUpdateAsync();
        ApplyTheme();
        UpdateStatusChips();
        Shown += async (_, _) =>
        {
            EnsureCheckboxColumnFitsDpi();
            LayoutContentColumns();
            // Brief pause so the themed window paints before the background PPDT scan starts.
            await Task.Delay(200);
            await TryAutoScanSavedModsFolderAsync();
        };
    }

    private async Task CheckAddonDatabaseUpdateAsync()
    {
        await AddonDatabaseService.TryUpdateFromRemoteAsync(msg =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(() => Log(msg));
        }).ConfigureAwait(true);
    }

    private void EnrichPlanesFromAddonDatabase()
    {
        AddonDatabaseService.LoadLocal(Log);
        var enriched = AddonDatabaseService.Enrich(_allPlanes);
        Log($"Enriched {enriched}/{_allPlanes.Count} planes with catalog metadata.");
    }

    /// <summary>
    /// At &gt;100% DPI, DataGridViewCheckBoxColumn stays at MinimumWidth 30 while
    /// checkbox PreferredSize grows, so glyphs clip and the column looks empty.
    /// </summary>
    private void EnsureCheckboxColumnFitsDpi()
    {
        if (planesGrid.Columns["Enabled"] is not DataGridViewCheckBoxColumn enabledCol)
            return;

        enabledCol.HeaderText = "Active";

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

        FitCompactGridColumns();
    }

    /// <summary>
    /// Keeps PlaneStringID and Name tight to content so they stay left-aligned next to PlaneID.
    /// Mod uses Fill for any remaining grid width.
    /// </summary>
    private void FitCompactGridColumns()
    {
        foreach (var columnName in new[] { "PlaneStringID", "Name" })
        {
            if (planesGrid.Columns[columnName] is { } col)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        planesGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

        if (planesGrid.Columns["Mod"] is { } modCol)
            modCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private void darkModeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        _isDarkMode = darkModeCheckBox.Checked;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var dark = _isDarkMode;
        BackColor = UiTheme.AppBg(dark);
        ForeColor = UiTheme.TextPrimary(dark);
        rootLayout.BackColor = UiTheme.AppBg(dark);

        StylePanel(headerPanel, dark, separator: false);
        headerPanel.BackColor = UiTheme.AppBg(dark);
        StylePanel(pathsPanel, dark, separator: false);
        pathsPanel.BackColor = UiTheme.AppBg(dark);
        StylePanel(actionsPanel, dark, separator: false);
        actionsPanel.BackColor = UiTheme.AppBg(dark);
        actionsLayout.BackColor = UiTheme.AppBg(dark);
        actionsLeft.BackColor = UiTheme.AppBg(dark);
        actionsRight.BackColor = UiTheme.AppBg(dark);
        if (actionsLayout.GetControlFromPosition(1, 0) is Control mid)
            mid.BackColor = UiTheme.AppBg(dark);

        StylePanel(searchPanel, dark, separator: false);
        searchPanel.BackColor = UiTheme.AppBg(dark);
        StylePanel(logPanel, dark, separator: false);
        logPanel.BackColor = UiTheme.AppBg(dark);
        StyleChip(activeModsChip, dark, cyanBorder: true);
        StyleChip(statusChip, dark, cyanBorder: false);

        titleLabel.ForeColor = UiTheme.TextPrimary(dark);
        titleLabel.BackColor = Color.Transparent;
        subtitleLabel.ForeColor = UiTheme.TextMuted(dark);
        subtitleLabel.BackColor = Color.Transparent;

        activeModsLabel.ForeColor = UiTheme.TextPrimary(dark);
        activeModsLabel.BackColor = Color.Transparent;
        activeModsDot.BackColor = Color.Transparent;
        activeModsDot.Paint -= ActiveModsDot_Paint;
        activeModsDot.Paint += ActiveModsDot_Paint;
        activeModsDot.Invalidate();

        statusPrefixLabel.ForeColor = UiTheme.TextMuted(dark);
        statusPrefixLabel.BackColor = Color.Transparent;
        statusValueLabel.ForeColor = UiTheme.Orange;
        statusValueLabel.BackColor = Color.Transparent;

        modsPathLabel.ForeColor = UiTheme.TextMuted(dark);
        modsPathLabel.BackColor = Color.Transparent;
        unrealPakLabel.ForeColor = UiTheme.TextMuted(dark);
        unrealPakLabel.BackColor = Color.Transparent;

        StylePathField(pakPathTextBox, dark);
        StylePathField(unrealPakPathTextBox, dark);
        StyleTextBox(searchTextBox, dark);
        StyleTextBox(logTextBox, dark, isLog: true);

        StyleSecondaryButton(scanModsFolderButton, dark);
        StyleSecondaryButton(browseUnrealPakButton, dark);
        UpdateActionButtons();

        scanModsFolderButton.Width = PathButtonWidth;
        browseUnrealPakButton.Width = PathButtonWidth;

        StyleCheckBox(darkModeCheckBox, dark);
        StyleCheckBox(hideBaseGameCheckBox, dark);
        StyleCheckBox(hideVrPlanesCheckBox, dark);
        CenterCheckBoxesWithButtons();

        logTitleLabel.ForeColor = UiTheme.TextMuted(dark);
        logTitleLabel.BackColor = Color.Transparent;

        ApplyThemeToDataGridView(planesGrid);
        UpdateStatusChips();
        LayoutContentColumns();
    }

    private void ActiveModsDot_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel dot)
            return;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(UiTheme.Success);
        e.Graphics.FillEllipse(brush, 0, 0, dot.Width - 1, dot.Height - 1);
    }

    private static void StylePanel(Panel panel, bool dark, bool separator)
    {
        panel.BackColor = UiTheme.PanelBg(dark);
        panel.BorderStyle = BorderStyle.None;
        // Bottom hairline only — avoids double borders and fullscreen vertical edge artifacts.
        panel.Tag = separator ? UiTheme.Border(dark) : null;
        panel.Paint -= PanelSeparator_Paint;
        panel.Paint += PanelSeparator_Paint;
        panel.Invalidate();
    }

    private static void StyleChip(Panel panel, bool dark, bool cyanBorder)
    {
        panel.BackColor = UiTheme.PanelBg(dark);
        panel.BorderStyle = BorderStyle.None;
        panel.Region = null;
        panel.Tag = cyanBorder ? UiTheme.CyanDim(dark) : UiTheme.Border(dark);
        panel.Paint -= PanelChipBorder_Paint;
        panel.Paint += PanelChipBorder_Paint;
        panel.Resize -= SoftChip_Resize;
        panel.Invalidate();
    }

    private static void SoftChip_Resize(object? sender, EventArgs e)
    {
        // Kept for compatibility; chips use hard corners again.
    }

    private static void PanelSeparator_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel || panel.Tag is not Color lineColor)
            return;

        using var pen = new Pen(Color.FromArgb(80, lineColor), 1);
        var y = panel.ClientSize.Height - 1;
        e.Graphics.DrawLine(pen, 0, y, panel.ClientSize.Width, y);
    }

    private static void PanelChipBorder_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel || panel.Tag is not Color borderColor)
            return;

        using var pen = new Pen(Color.FromArgb(130, borderColor), 1);
        var r = panel.ClientRectangle;
        e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
    }

    private static void StylePathField(CenteredPathTextBox field, bool dark)
    {
        field.ApplyFieldColors(UiTheme.InputBg(dark), UiTheme.TextPrimary(dark));
    }

    private static void StyleTextBox(TextBox box, bool dark, bool isLog = false)
    {
        box.BackColor = isLog ? UiTheme.LogBg(dark) : UiTheme.InputBg(dark);
        box.ForeColor = UiTheme.TextPrimary(dark);
        box.BorderStyle = BorderStyle.FixedSingle;
        if (!isLog)
            box.Height = box.PreferredHeight;
    }

    private static void StyleCheckBox(CheckBox box, bool dark)
    {
        box.FlatStyle = FlatStyle.Standard;
        box.UseVisualStyleBackColor = false;
        box.BackColor = UiTheme.AppBg(dark);
        box.ForeColor = UiTheme.TextMuted(dark);
        box.AutoCheck = true;
        box.Enabled = true;
    }

    private void CenterCheckBoxesWithButtons()
    {
        // Match action-button height so checkbox labels sit on the same baseline.
        var btnH = Math.Max(
            Math.Max(unpackAndLoadButton.Height, applyAndSaveButton.Height),
            openOutputFolderButton.Height);
        foreach (var cb in new[] { hideBaseGameCheckBox, hideVrPlanesCheckBox, darkModeCheckBox })
        {
            var textW = TextRenderer.MeasureText(cb.Text, cb.Font).Width;
            cb.AutoSize = false;
            cb.Size = new Size(textW + SystemInformation.MenuCheckSize.Width + 12, btnH);
            cb.TextAlign = ContentAlignment.MiddleLeft;
            cb.Margin = new Padding(0, 0, cb == darkModeCheckBox ? 0 : 16, 0);
        }
    }

    private void UpdateActionButtons()
    {
        var dark = _isDarkMode;

        switch (_workflowStep)
        {
            case WorkflowStep.NoPak:
                StyleWorkflowButton(unpackAndLoadButton, dark, highlighted: false, enabled: false);
                StyleWorkflowButton(applyAndSaveButton, dark, highlighted: false, enabled: false);
                StyleWorkflowButton(openOutputFolderButton, dark, highlighted: false, enabled: false);
                break;
            case WorkflowStep.ReadyToUnpack:
                StyleWorkflowButton(unpackAndLoadButton, dark, highlighted: true, enabled: true);
                StyleWorkflowButton(applyAndSaveButton, dark, highlighted: false, enabled: false);
                StyleWorkflowButton(openOutputFolderButton, dark, highlighted: false, enabled: false);
                break;
            case WorkflowStep.ReadyToPack:
                StyleWorkflowButton(unpackAndLoadButton, dark, highlighted: false, enabled: false);
                StyleWorkflowButton(applyAndSaveButton, dark, highlighted: true, enabled: true);
                StyleWorkflowButton(openOutputFolderButton, dark, highlighted: false, enabled: true);
                break;
        }

        unpackAndLoadButton.Width = 140;
        applyAndSaveButton.Width = 170;
        openOutputFolderButton.Width = 165;
        CenterCheckBoxesWithButtons();
    }

    private static void StyleWorkflowButton(Button btn, bool dark, bool highlighted, bool enabled)
    {
        ApplyButtonBoxMetrics(btn);
        btn.FlatStyle = FlatStyle.Flat;
        btn.Enabled = enabled;
        btn.FlatAppearance.BorderSize = 0;
        ClearSoftButton(btn);

        if (!enabled)
        {
            btn.BackColor = UiTheme.DisabledButtonBg(dark);
            btn.ForeColor = UiTheme.DisabledButtonText(dark);
            btn.FlatAppearance.MouseOverBackColor = btn.BackColor;
            btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            btn.Cursor = Cursors.Default;
            return;
        }

        btn.Cursor = Cursors.Hand;
        if (highlighted)
        {
            btn.BackColor = UiTheme.Orange;
            btn.ForeColor = Color.FromArgb(28, 24, 20);
            btn.FlatAppearance.MouseOverBackColor = UiTheme.OrangeHover;
            btn.FlatAppearance.MouseDownBackColor = UiTheme.OrangePressed;
        }
        else
        {
            btn.BackColor = UiTheme.SecondaryButtonBg(dark);
            btn.ForeColor = UiTheme.TextPrimary(dark);
            btn.FlatAppearance.MouseOverBackColor = UiTheme.PanelBgAlt(dark);
            btn.FlatAppearance.MouseDownBackColor = UiTheme.Border(dark);
        }
    }

    private static void StyleSecondaryButton(Button btn, bool dark)
    {
        ApplyButtonBoxMetrics(btn);
        btn.FlatStyle = FlatStyle.Flat;
        btn.BackColor = UiTheme.SecondaryButtonBg(dark);
        btn.ForeColor = UiTheme.TextPrimary(dark);
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = UiTheme.PanelBgAlt(dark);
        btn.FlatAppearance.MouseDownBackColor = UiTheme.Border(dark);
        btn.Cursor = Cursors.Hand;
        ClearSoftButton(btn);
    }

    private static void ClearSoftButton(Button btn)
    {
        btn.Resize -= SoftButton_Resize;
        btn.Region = null;
    }

    private static void SoftButton_Resize(object? sender, EventArgs e)
    {
        // Unused — hard corners restored.
    }

    private static void ApplyButtonBoxMetrics(Button btn)
    {
        btn.AutoSize = false;
        btn.TextAlign = ContentAlignment.MiddleCenter;
        btn.Padding = new Padding(BoxPadX + 2, 0, BoxPadX + 2, 0);
        btn.UseCompatibleTextRendering = false;
        btn.Height = MeasureSingleLineBoxHeight(btn.Font);
    }

    private void ApplyThemeToDataGridView(DataGridView grid)
    {
        var dark = _isDarkMode;
        _modLinkColor = UiTheme.Cyan(dark);

        grid.BackgroundColor = UiTheme.AppBg(dark);
        grid.GridColor = UiTheme.Border(dark);
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.PanelBg(dark);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextMuted(dark);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiTheme.PanelBg(dark);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiTheme.TextMuted(dark);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

        grid.DefaultCellStyle.BackColor = UiTheme.GridRow(dark);
        grid.DefaultCellStyle.ForeColor = UiTheme.TextPrimary(dark);
        grid.DefaultCellStyle.SelectionBackColor = UiTheme.SelectionBg(dark);
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary(dark);
        grid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.GridRowAlt(dark);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = UiTheme.TextPrimary(dark);
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiTheme.SelectionBg(dark);
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary(dark);

        _modLinkFont?.Dispose();
        _modLinkFont = new Font(grid.Font, FontStyle.Underline);
    }

    private void UpdateStatusChips()
    {
        var total = _allPlanes.Count;
        var active = _allPlanes.Count(p => p.Enabled);
        activeModsLabel.Text = $"{active} / {total} ACTIVE";

        // Grow chip to fit text with padding
        var need = TextRenderer.MeasureText(activeModsLabel.Text, activeModsLabel.Font).Width + 48;
        activeModsChip.Width = Math.Max(150, need);
        LayoutHeaderChips();

        if (_pakScanInProgress)
        {
            statusValueLabel.Text = "SCANNING";
            statusValueLabel.ForeColor = UiTheme.Orange;
            return;
        }

        if (total == 0)
        {
            statusValueLabel.Text = "IDLE";
            statusValueLabel.ForeColor = UiTheme.TextMuted(_isDarkMode);
        }
        else
        {
            statusValueLabel.Text = "TARGET READY";
            statusValueLabel.ForeColor = UiTheme.Orange;
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
        UpdateStatusChips();
    }

    private void planesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0)
            return;
        if (planesGrid.Columns[e.ColumnIndex].Name != "Mod")
            return;
        if (planesGrid.Rows[e.RowIndex].DataBoundItem is not PlaneDataRow row)
            return;
        if (string.IsNullOrWhiteSpace(row.ModUrl) || e.CellStyle == null)
            return;

        e.CellStyle.ForeColor = _modLinkColor;
        if (_modLinkFont != null)
            e.CellStyle.Font = _modLinkFont;
    }

    private void planesGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;
        if (planesGrid.Columns[e.ColumnIndex].Name != "Mod")
            return;
        if (planesGrid.Rows[e.RowIndex].DataBoundItem is not PlaneDataRow row)
            return;
        if (string.IsNullOrWhiteSpace(row.ModUrl))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(row.ModUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"Could not open mod link: {ex.Message}");
        }
    }

    private async void scanModsFolderButton_Click(object? sender, EventArgs e)
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
            if (!string.IsNullOrWhiteSpace(_modsOutputFolder) && Directory.Exists(_modsOutputFolder))
                fbd.SelectedPath = _modsOutputFolder;

            if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                return;

            await TrySelectWinningPakFromFolderAsync(fbd.SelectedPath, showDialogs: true);
        }
        catch (Exception ex)
        {
            Log($"Unexpected error during Choose ~mods: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task TryAutoScanSavedModsFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(_modsOutputFolder) || !Directory.Exists(_modsOutputFolder))
            return;

        var unrealPakPath = unrealPakPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
            return;

        await TrySelectWinningPakFromFolderAsync(_modsOutputFolder, showDialogs: false, isAutoScan: true);
    }

    private sealed class PakScanResult
    {
        public string? WinningPak { get; set; }
        public string? ErrorMessage { get; set; }
        public bool NoMatch { get; set; }
    }

    private static PakScanResult ScanWinningPakInFolder(
        string unrealPakPath,
        string chosenFolder,
        IProgress<string> logProgress)
    {
        var result = new PakScanResult();
        void LogLine(string msg) => logProgress.Report(msg);

        try
        {
            var winningPak = PakService.FindWinningPlanePakInFolder(unrealPakPath, chosenFolder, LogLine);
            if (string.IsNullOrWhiteSpace(winningPak))
            {
                result.NoMatch = true;
                return result;
            }

            result.WinningPak = winningPak;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            LogLine($"Error while choosing PAK: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Batches scan log lines onto the UI thread so the window stays responsive during long folder scans.
    /// </summary>
    private sealed class StreamingLogProgress : IProgress<string>, IDisposable
    {
        private readonly Control _uiRoot;
        private readonly Action<string> _log;
        private readonly System.Windows.Forms.Timer _flushTimer;
        private readonly List<string> _pending = new();
        private readonly object _lock = new();

        public StreamingLogProgress(Control uiRoot, Action<string> log)
        {
            _uiRoot = uiRoot;
            _log = log;
            _flushTimer = new System.Windows.Forms.Timer { Interval = 75 };
            _flushTimer.Tick += (_, _) => FlushPending();
        }

        public void Report(string value)
        {
            lock (_lock)
                _pending.Add(value);

            if (!_uiRoot.IsHandleCreated || _uiRoot.IsDisposed)
                return;

            try
            {
                _uiRoot.BeginInvoke(EnableTimer);
            }
            catch (InvalidOperationException)
            {
                // Form is closing.
            }
        }

        private void EnableTimer()
        {
            if (!_flushTimer.Enabled)
                _flushTimer.Start();
        }

        public void Flush()
        {
            if (_uiRoot.InvokeRequired)
            {
                try
                {
                    _uiRoot.Invoke(Flush);
                }
                catch (InvalidOperationException)
                {
                    // Form is closing.
                }

                return;
            }

            FlushPending();
        }

        private void FlushPending()
        {
            List<string> batch;
            lock (_lock)
            {
                if (_pending.Count == 0)
                {
                    _flushTimer.Stop();
                    return;
                }

                batch = new List<string>(_pending);
                _pending.Clear();
            }

            foreach (var line in batch)
                _log(line);
        }

        public void Dispose()
        {
            _flushTimer.Stop();
            _flushTimer.Dispose();
        }
    }

    private async Task<bool> TrySelectWinningPakFromFolderAsync(
        string chosenFolder,
        bool showDialogs,
        bool isAutoScan = false)
    {
        var unrealPakPath = unrealPakPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(unrealPakPath) || !File.Exists(unrealPakPath))
        {
            if (showDialogs)
                MessageBox.Show(this, "Select a valid UnrealPak.exe first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(chosenFolder) || !Directory.Exists(chosenFolder))
            return false;

        if (Interlocked.CompareExchange(ref _pakScanRunning, 1, 0) != 0)
            return false;

        _modsOutputFolder = chosenFolder;
        SaveConfig();

        if (isAutoScan)
            Log($"Auto-scanning saved ~mods folder: {chosenFolder}");
        else
            Log($"Choosing PAK from folder: {chosenFolder}");

        PakScanResult scanResult;
        StreamingLogProgress? logProgress = null;
        try
        {
            _pakScanInProgress = true;
            UpdateStatusChips();
            await Task.Yield();

            logProgress = new StreamingLogProgress(this, Log);
            scanResult = await Task.Run(() => ScanWinningPakInFolder(unrealPakPath, chosenFolder, logProgress));
        }
        finally
        {
            logProgress?.Flush();
            logProgress?.Dispose();
            _pakScanInProgress = false;
            Interlocked.Exchange(ref _pakScanRunning, 0);
        }

        if (IsDisposed)
            return false;

        UpdateStatusChips();

        if (!string.IsNullOrWhiteSpace(scanResult.ErrorMessage))
        {
            if (showDialogs)
                MessageBox.Show(this, scanResult.ErrorMessage, "Choose PAK failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (scanResult.NoMatch || string.IsNullOrWhiteSpace(scanResult.WinningPak))
        {
            if (showDialogs)
            {
                MessageBox.Show(this,
                    "No PAKs with PlayerPlaneDataTable.uasset were found in the selected folder.",
                    "No matching PAK",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                Log("No PAKs with PlayerPlaneDataTable.uasset were found in the saved ~mods folder.");
            }

            return false;
        }

        pakPathTextBox.Text = scanResult.WinningPak;
        _workflowStep = WorkflowStep.ReadyToUnpack;
        _planes.Clear();
        _allPlanes.Clear();
        ApplyPlaneFilters();
        UpdateStatusChips();
        UpdateActionButtons();
        Log($"Using winning plane PAK: {scanResult.WinningPak}. You can now click \"Unpack && Load\".");
        return true;
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
            _ = TryAutoScanSavedModsFolderAsync();
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

            var unpackDir = GetUnpackDirectory(pakPath);

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

            EnrichPlanesFromAddonDatabase();
            ApplyPlaneFilters();
            UpdateStatusChips();
            _workflowStep = WorkflowStep.ReadyToPack;
            UpdateActionButtons();

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

            var unpackDir = GetUnpackDirectory(pakPath);

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
            var unpackDir = GetUnpackDirectory(pakPath);
            var outputDir = GetPackedPakOutputDirectory(pakPath);
            var outputPakPath = Path.Combine(outputDir, outputFileName);

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

        var unpackDir = GetUnpackDirectory(pakPath);

        if (!Directory.Exists(unpackDir))
        {
            MessageBox.Show(this, "Unpacked folder does not exist yet. Run Unpack & Load first.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start("explorer.exe", unpackDir);
        Log($"Opened unpacked folder: {unpackDir}");
    }

    private static string GetUnpackDirectory(string pakPath)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, Path.GetFileNameWithoutExtension(pakPath) + "_unpacked");
    }

    /// <summary>
    /// Packed PAKs go into the scanned ~mods folder when available; otherwise next to the source PAK.
    /// </summary>
    private string GetPackedPakOutputDirectory(string pakPath)
    {
        if (!string.IsNullOrWhiteSpace(_modsOutputFolder) && Directory.Exists(_modsOutputFolder))
            return _modsOutputFolder;

        var pakDir = Path.GetDirectoryName(pakPath);
        if (!string.IsNullOrWhiteSpace(pakDir) && Directory.Exists(pakDir))
            return pakDir;

        return AppDomain.CurrentDomain.BaseDirectory;
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
                else if (key.Equals("ModsFolder", StringComparison.OrdinalIgnoreCase) && Directory.Exists(value))
                {
                    _modsOutputFolder = value;
                }
                else if (key.Equals("AddonDatabaseUrl", StringComparison.OrdinalIgnoreCase))
                {
                    AddonDatabaseService.SetRemoteUrlOverride(value);
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
            if (!string.IsNullOrWhiteSpace(_modsOutputFolder))
                sb.AppendLine($"ModsFolder={_modsOutputFolder}");

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
        _modLinkFont?.Dispose();
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

        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.ScrollToCaret();
    }

    private void ApplyPlaneFilters()
    {
        _planes.Clear();

        if (_allPlanes.Count == 0)
        {
            UpdateStatusChips();
            return;
        }

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
                var matchesName = plane.PlaneName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                var matchesMod = plane.ModText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                var matchesId = searchIsNumeric
                    ? plane.PlaneID == searchNumeric
                    : plane.PlaneID.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase);

                if (!matchesString && !matchesName && !matchesMod && !matchesId)
                    continue;
            }

            _planes.Add(plane);
        }

        EnsureCheckboxColumnFitsDpi();
        UpdateStatusChips();
    }
}

