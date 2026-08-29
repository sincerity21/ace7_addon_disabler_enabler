using System;
using System.Drawing;
using System.Windows.Forms;

namespace DisableEnabler;

public partial class MainForm
{
    private const int ContentPad = 12;
    private const int PathButtonWidth = 140;
    private const int Gap = 8;
    private const int BoxPadY = 4;
    private const int BoxPadX = 10;
    private const int SectionGap = 10;

    private TableLayoutPanel rootLayout = null!;
    private Panel headerPanel = null!;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private Panel activeModsChip = null!;
    private Panel activeModsDot = null!;
    private Label activeModsLabel = null!;
    private Panel statusChip = null!;
    private Label statusPrefixLabel = null!;
    private Label statusValueLabel = null!;

    private Panel pathsPanel = null!;
    private Label modsPathLabel = null!;
    private CenteredPathTextBox pakPathTextBox = null!;
    private Button scanModsFolderButton = null!;
    private Label unrealPakLabel = null!;
    private CenteredPathTextBox unrealPakPathTextBox = null!;
    private Button browseUnrealPakButton = null!;

    private Panel actionsPanel = null!;
    private TableLayoutPanel actionsLayout = null!;
    private FlowLayoutPanel actionsLeft = null!;
    private FlowLayoutPanel actionsRight = null!;
    private Button unpackAndLoadButton = null!;
    private Button applyAndSaveButton = null!;
    private Button openOutputFolderButton = null!;
    private CheckBox hideBaseGameCheckBox = null!;
    private CheckBox hideVrPlanesCheckBox = null!;
    private CheckBox darkModeCheckBox = null!;

    private Panel searchPanel = null!;
    private CenteredPathTextBox searchTextBox = null!;

    private DataGridView planesGrid = null!;

    private Panel logPanel = null!;
    private Label logTitleLabel = null!;
    private TextBox logTextBox = null!;

    private void InitializeComponent()
    {
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        titleLabel = new Label();
        subtitleLabel = new Label();
        activeModsChip = new Panel();
        activeModsDot = new Panel();
        activeModsLabel = new Label();
        statusChip = new Panel();
        statusPrefixLabel = new Label();
        statusValueLabel = new Label();

        pathsPanel = new Panel();
        modsPathLabel = new Label();
        pakPathTextBox = new CenteredPathTextBox();
        scanModsFolderButton = new Button();
        unrealPakLabel = new Label();
        unrealPakPathTextBox = new CenteredPathTextBox();
        browseUnrealPakButton = new Button();

        actionsPanel = new Panel();
        actionsLayout = new TableLayoutPanel();
        actionsLeft = new FlowLayoutPanel();
        actionsRight = new FlowLayoutPanel();
        unpackAndLoadButton = new Button();
        applyAndSaveButton = new Button();
        openOutputFolderButton = new Button();
        hideBaseGameCheckBox = new CheckBox();
        hideVrPlanesCheckBox = new CheckBox();
        darkModeCheckBox = new CheckBox();

        searchPanel = new Panel();
        searchTextBox = new CenteredPathTextBox(readOnly: false);

        planesGrid = new DataGridView();
        logPanel = new Panel();
        logTitleLabel = new Label();
        logTextBox = new TextBox();

        ((System.ComponentModel.ISupportInitialize)planesGrid).BeginInit();
        SuspendLayout();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        pathsPanel.SuspendLayout();
        actionsPanel.SuspendLayout();
        actionsLayout.SuspendLayout();
        actionsLeft.SuspendLayout();
        actionsRight.SuspendLayout();
        searchPanel.SuspendLayout();
        logPanel.SuspendLayout();

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Padding = new Padding(16);
        rootLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        rootLayout.RowCount = 6;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));

        // header
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Margin = new Padding(0);
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Location = new Point(ContentPad, 4);
        titleLabel.Text = "THE DISABLE-ENABLER";
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        subtitleLabel.Location = new Point(ContentPad, 36);
        subtitleLabel.Text = "AN ADD-ON MANAGEMENT TOOL BY SINCERITY";

        activeModsChip.Anchor = AnchorStyles.None;
        activeModsChip.Size = new Size(170, 30);
        activeModsDot.Size = new Size(10, 10);
        activeModsDot.Location = new Point(12, 10);
        activeModsLabel.AutoSize = true;
        activeModsLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        activeModsLabel.Location = new Point(28, 7);
        activeModsLabel.Text = "0 / 0 ACTIVE";
        activeModsChip.Controls.Add(activeModsDot);
        activeModsChip.Controls.Add(activeModsLabel);

        statusChip.Anchor = AnchorStyles.None;
        statusChip.Size = new Size(200, 30);
        statusPrefixLabel.AutoSize = true;
        statusPrefixLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        statusPrefixLabel.Location = new Point(12, 7);
        statusPrefixLabel.Text = "STATUS:";
        statusValueLabel.AutoSize = true;
        statusValueLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        statusValueLabel.Location = new Point(72, 7);
        statusValueLabel.Text = "IDLE";
        statusChip.Controls.Add(statusPrefixLabel);
        statusChip.Controls.Add(statusValueLabel);

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(activeModsChip);
        headerPanel.Controls.Add(statusChip);

        // paths — absolute layout driven by LayoutContentColumns
        pathsPanel.Dock = DockStyle.Fill;
        pathsPanel.Margin = new Padding(0);
        modsPathLabel.AutoSize = true;
        modsPathLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        modsPathLabel.Text = "~mods Directory";
        scanModsFolderButton.FlatStyle = FlatStyle.Flat;
        scanModsFolderButton.Text = "Choose ~mods";
        scanModsFolderButton.UseMnemonic = false;
        scanModsFolderButton.Click += scanModsFolderButton_Click;

        unrealPakLabel.AutoSize = true;
        unrealPakLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        unrealPakLabel.Text = "UnrealPak.exe Directory";
        browseUnrealPakButton.FlatStyle = FlatStyle.Flat;
        browseUnrealPakButton.Text = "Browse EXE";
        browseUnrealPakButton.Click += browseUnrealPakButton_Click;

        pathsPanel.Controls.Add(modsPathLabel);
        pathsPanel.Controls.Add(pakPathTextBox);
        pathsPanel.Controls.Add(scanModsFolderButton);
        pathsPanel.Controls.Add(unrealPakLabel);
        pathsPanel.Controls.Add(unrealPakPathTextBox);
        pathsPanel.Controls.Add(browseUnrealPakButton);

        // actions — 3-column table: left | stretch empty | right (no Dock seam)
        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.Margin = new Padding(0);
        actionsLayout.Dock = DockStyle.Fill;
        actionsLayout.ColumnCount = 3;
        actionsLayout.RowCount = 1;
        actionsLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionsLayout.Padding = new Padding(ContentPad, 0, ContentPad, 0);

        actionsLeft.AutoSize = true;
        actionsLeft.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        actionsLeft.Dock = DockStyle.Fill;
        actionsLeft.FlowDirection = FlowDirection.LeftToRight;
        actionsLeft.WrapContents = false;
        actionsLeft.Padding = new Padding(0);
        actionsLeft.Margin = new Padding(0);
        actionsLeft.BorderStyle = BorderStyle.None;

        actionsRight.AutoSize = true;
        actionsRight.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        actionsRight.Dock = DockStyle.Fill;
        actionsRight.FlowDirection = FlowDirection.LeftToRight;
        actionsRight.WrapContents = false;
        actionsRight.Padding = new Padding(0);
        actionsRight.Margin = new Padding(0);
        actionsRight.BorderStyle = BorderStyle.None;

        unpackAndLoadButton.FlatStyle = FlatStyle.Flat;
        unpackAndLoadButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        unpackAndLoadButton.Margin = new Padding(0, 0, Gap, 0);
        unpackAndLoadButton.Text = "Unpack && Load";
        unpackAndLoadButton.Click += unpackAndLoadButton_Click;

        applyAndSaveButton.FlatStyle = FlatStyle.Flat;
        applyAndSaveButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        applyAndSaveButton.Margin = new Padding(0, 0, Gap, 0);
        applyAndSaveButton.Text = "Apply, Save && Pack";
        applyAndSaveButton.Click += applyAndSaveButton_Click;

        openOutputFolderButton.FlatStyle = FlatStyle.Flat;
        openOutputFolderButton.Margin = new Padding(0);
        openOutputFolderButton.Text = "Open Unpacked Folder";
        openOutputFolderButton.Click += openOutputFolderButton_Click;

        hideBaseGameCheckBox.AutoSize = true;
        hideBaseGameCheckBox.Margin = new Padding(0, 0, 16, 0);
        hideBaseGameCheckBox.TextAlign = ContentAlignment.MiddleLeft;
        hideBaseGameCheckBox.Text = "Hide Base Game";
        hideBaseGameCheckBox.Checked = true;
        hideBaseGameCheckBox.CheckedChanged += filterCheckBox_CheckedChanged;

        hideVrPlanesCheckBox.AutoSize = true;
        hideVrPlanesCheckBox.Margin = new Padding(0, 0, 16, 0);
        hideVrPlanesCheckBox.TextAlign = ContentAlignment.MiddleLeft;
        hideVrPlanesCheckBox.Text = "Hide VR Planes";
        hideVrPlanesCheckBox.Checked = true;
        hideVrPlanesCheckBox.CheckedChanged += filterCheckBox_CheckedChanged;

        darkModeCheckBox.AutoSize = true;
        darkModeCheckBox.Margin = new Padding(0);
        darkModeCheckBox.TextAlign = ContentAlignment.MiddleLeft;
        darkModeCheckBox.Text = "Dark Mode";
        darkModeCheckBox.CheckedChanged += darkModeCheckBox_CheckedChanged;

        actionsLeft.Controls.Add(unpackAndLoadButton);
        actionsLeft.Controls.Add(applyAndSaveButton);
        actionsLeft.Controls.Add(openOutputFolderButton);
        actionsRight.Controls.Add(hideBaseGameCheckBox);
        actionsRight.Controls.Add(hideVrPlanesCheckBox);
        actionsRight.Controls.Add(darkModeCheckBox);
        actionsLayout.Controls.Add(actionsLeft, 0, 0);
        actionsLayout.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 1, 0);
        actionsLayout.Controls.Add(actionsRight, 2, 0);
        actionsPanel.Controls.Add(actionsLayout);

        // search — same left/right inset as path fields
        searchPanel.Dock = DockStyle.Fill;
        searchPanel.Margin = new Padding(0);
        searchTextBox.PlaceholderText = "Filter plane list...";
        searchTextBox.TextChanged += searchTextBox_TextChanged;
        searchPanel.Controls.Add(searchTextBox);

        // grid
        planesGrid.Dock = DockStyle.Fill;
        planesGrid.Margin = new Padding(0);
        planesGrid.BorderStyle = BorderStyle.FixedSingle;
        planesGrid.BackgroundColor = Color.FromArgb(18, 22, 30);
        planesGrid.RowTemplate.Height = 28;
        planesGrid.AllowUserToAddRows = false;
        planesGrid.AllowUserToDeleteRows = false;
        planesGrid.AllowUserToResizeRows = false;
        planesGrid.RowHeadersVisible = false;
        planesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        planesGrid.MultiSelect = true;
        planesGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        planesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        planesGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        planesGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        planesGrid.ColumnHeadersHeight = 36;
        planesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        planesGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Active",
            DataPropertyName = "Enabled",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 30,
            Resizable = DataGridViewTriState.False
        });
        planesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PlaneID",
            HeaderText = "Plane ID",
            DataPropertyName = "PlaneID",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 60,
            ReadOnly = true
        });
        planesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PlaneStringID",
            HeaderText = "String ID",
            DataPropertyName = "PlaneStringID",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 80,
            ReadOnly = true
        });
        planesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Plane Name",
            DataPropertyName = "PlaneName",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 80,
            ReadOnly = true
        });
        planesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Mod",
            HeaderText = "Origin",
            DataPropertyName = "ModText",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 80,
            ReadOnly = true
        });
        planesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Notes",
            HeaderText = "Notes",
            DataPropertyName = "NotesText",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 80,
            ReadOnly = true
        });
        planesGrid.CurrentCellDirtyStateChanged += planesGrid_CurrentCellDirtyStateChanged;
        planesGrid.CellValueChanged += planesGrid_CellValueChanged;
        planesGrid.CellMouseDown += planesGrid_CellMouseDown;
        planesGrid.CellContentClick += planesGrid_CellContentClick;
        planesGrid.CellFormatting += planesGrid_CellFormatting;

        // log
        logPanel.Dock = DockStyle.Fill;
        logPanel.Margin = new Padding(0);
        logTitleLabel.AutoSize = true;
        logTitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        logTitleLabel.Text = "DIAGNOSTIC SYSTEM OUTPUT LOG";
        logTextBox.BorderStyle = BorderStyle.FixedSingle;
        logTextBox.Multiline = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.ReadOnly = true;
        logTextBox.Font = new Font("Consolas", 9F);
        logPanel.Controls.Add(logTitleLabel);
        logPanel.Controls.Add(logTextBox);

        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(pathsPanel, 0, 1);
        rootLayout.Controls.Add(actionsPanel, 0, 2);
        rootLayout.Controls.Add(searchPanel, 0, 3);
        rootLayout.Controls.Add(planesGrid, 0, 4);
        rootLayout.Controls.Add(logPanel, 0, 5);

        Font = new Font("Segoe UI", 9F);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 860);
        Controls.Add(rootLayout);
        MinimumSize = new Size(1100, 720);
        Text = "THE DISABLE-ENABLER";
        FormClosing += MainForm_FormClosing;
        Resize += (_, _) => LayoutContentColumns();

        ((System.ComponentModel.ISupportInitialize)planesGrid).EndInit();
        logPanel.ResumeLayout(false);
        logPanel.PerformLayout();
        searchPanel.ResumeLayout(false);
        actionsRight.ResumeLayout(false);
        actionsRight.PerformLayout();
        actionsLeft.ResumeLayout(false);
        actionsLayout.ResumeLayout(false);
        actionsLayout.PerformLayout();
        actionsPanel.ResumeLayout(false);
        pathsPanel.ResumeLayout(false);
        pathsPanel.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        rootLayout.ResumeLayout(false);
        ResumeLayout(false);
    }

    /// <summary>
    /// Single content column: same left/right edges for paths, search, log, and header chips.
    /// </summary>
    private void LayoutContentColumns()
    {
        LayoutHeaderChips();
        LayoutPaths();
        LayoutMiddleSection();
        LayoutSearch();
        LayoutLog();
    }

    private void LayoutHeaderChips()
    {
        var right = headerPanel.ClientSize.Width - ContentPad;
        statusChip.Top = 14;
        activeModsChip.Top = 14;
        statusChip.Left = right - statusChip.Width;
        activeModsChip.Left = statusChip.Left - Gap - activeModsChip.Width;
        headerPanel.Invalidate();
    }

    private void LayoutPaths()
    {
        var left = ContentPad;
        var right = pathsPanel.ClientSize.Width - ContentPad;
        // Shared row height; CenteredPathTextBox vertically centers text inside via EM_SETRECT.
        var boxH = MeasureSingleLineBoxHeight(Font);
        var fieldRight = right - PathButtonWidth - Gap;
        var fieldW = Math.Max(120, fieldRight - left);

        modsPathLabel.Location = new Point(left, 8);
        var row1Y = modsPathLabel.Bottom + BoxPadY;
        pakPathTextBox.SetBounds(left, row1Y, fieldW, boxH);
        scanModsFolderButton.SetBounds(fieldRight + Gap, row1Y, PathButtonWidth, boxH);

        unrealPakLabel.Location = new Point(left, pakPathTextBox.Bottom + SectionGap);
        var row2Y = unrealPakLabel.Bottom + BoxPadY;
        unrealPakPathTextBox.SetBounds(left, row2Y, fieldW, boxH);
        browseUnrealPakButton.SetBounds(fieldRight + Gap, row2Y, PathButtonWidth, boxH);
    }

    private void LayoutMiddleSection()
    {
        var boxH = MeasureSingleLineBoxHeight(Font);
        var btnH = Math.Max(
            Math.Max(unpackAndLoadButton.Height, applyAndSaveButton.Height),
            openOutputFolderButton.Height);
        if (btnH <= 0)
            btnH = boxH;

        var pathsBottom = Math.Max(browseUnrealPakButton.Bottom, unrealPakPathTextBox.Bottom);
        if (pathsBottom <= 0)
            pathsBottom = 120;

        rootLayout.RowStyles[1].Height = pathsBottom + SectionGap;
        rootLayout.RowStyles[2].Height = btnH;
        rootLayout.RowStyles[3].Height = SectionGap + boxH;

        planesGrid.Margin = new Padding(0, SectionGap, 0, 0);
    }

    private void LayoutSearch()
    {
        var left = ContentPad;
        var w = Math.Max(120, searchPanel.ClientSize.Width - ContentPad * 2);
        var boxH = MeasureSingleLineBoxHeight(Font);
        searchTextBox.SetBounds(left, SectionGap, w, boxH);
    }

    private void LayoutLog()
    {
        var left = ContentPad;
        var w = Math.Max(120, logPanel.ClientSize.Width - ContentPad * 2);
        logTitleLabel.Location = new Point(left, BoxPadY);
        var logTop = logTitleLabel.Bottom + BoxPadY;
        logTextBox.SetBounds(left, logTop, w, Math.Max(40, logPanel.ClientSize.Height - logTop - BoxPadY));
    }

    private static int MeasureSingleLineBoxHeight(Font font)
    {
        var textH = TextRenderer.MeasureText("Ag", font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
        // Room for glyphs + equal visual pad; Flat buttons need a little extra vs TextBox.
        return Math.Max(28, textH + BoxPadY * 2 + 4);
    }
}
