using System.Windows.Forms;

namespace DisableEnabler;

public partial class MainForm
{
    private TextBox pakPathTextBox = null!;
    private Button browsePakButton = null!;
    private TextBox unrealPakPathTextBox = null!;
    private Button browseUnrealPakButton = null!;
    private Button unpackAndLoadButton = null!;
    private Button applyAndSaveButton = null!;
    private Button openOutputFolderButton = null!;
    private CheckBox hideBaseGameCheckBox = null!;
    private CheckBox hideVrPlanesCheckBox = null!;
    private TextBox searchTextBox = null!;
    private DataGridView planesGrid = null!;
    private TextBox logTextBox = null!;
    private CheckBox darkModeCheckBox = null!;

    private void InitializeComponent()
    {
        this.pakPathTextBox = new TextBox();
        this.browsePakButton = new Button();
        this.unrealPakPathTextBox = new TextBox();
        this.browseUnrealPakButton = new Button();
        this.unpackAndLoadButton = new Button();
        this.applyAndSaveButton = new Button();
        this.openOutputFolderButton = new Button();
        this.hideBaseGameCheckBox = new CheckBox();
        this.hideVrPlanesCheckBox = new CheckBox();
        this.searchTextBox = new TextBox();
        this.planesGrid = new DataGridView();
        this.logTextBox = new TextBox();
        this.darkModeCheckBox = new CheckBox();

        ((System.ComponentModel.ISupportInitialize)(this.planesGrid)).BeginInit();
        this.SuspendLayout();

        // pakPathTextBox
        this.pakPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.pakPathTextBox.Location = new System.Drawing.Point(12, 12);
        this.pakPathTextBox.Size = new System.Drawing.Size(520, 23);

        // browsePakButton
        this.browsePakButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.browsePakButton.Location = new System.Drawing.Point(538, 11);
        this.browsePakButton.Size = new System.Drawing.Size(80, 25);
        this.browsePakButton.Text = "Browse PAK";
        this.browsePakButton.Click += this.browsePakButton_Click;

        // unrealPakPathTextBox
        this.unrealPakPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.unrealPakPathTextBox.Location = new System.Drawing.Point(12, 43);
        this.unrealPakPathTextBox.Size = new System.Drawing.Size(520, 23);

        // browseUnrealPakButton
        this.browseUnrealPakButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.browseUnrealPakButton.Location = new System.Drawing.Point(538, 42);
        this.browseUnrealPakButton.Size = new System.Drawing.Size(80, 25);
        this.browseUnrealPakButton.Text = "Browse EXE";
        this.browseUnrealPakButton.Click += this.browseUnrealPakButton_Click;

        // unpackAndLoadButton
        this.unpackAndLoadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.unpackAndLoadButton.Location = new System.Drawing.Point(12, 80);
        this.unpackAndLoadButton.Size = new System.Drawing.Size(120, 27);
        this.unpackAndLoadButton.Text = "Unpack && Load";
        this.unpackAndLoadButton.Click += this.unpackAndLoadButton_Click;

        // applyAndSaveButton
        this.applyAndSaveButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.applyAndSaveButton.Location = new System.Drawing.Point(138, 80);
        this.applyAndSaveButton.Size = new System.Drawing.Size(140, 27);
        this.applyAndSaveButton.Text = "Apply, Save && Pack";
        this.applyAndSaveButton.Click += this.applyAndSaveButton_Click;

        // openOutputFolderButton
        this.openOutputFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.openOutputFolderButton.Location = new System.Drawing.Point(284, 80);
        this.openOutputFolderButton.Size = new System.Drawing.Size(140, 27);
        this.openOutputFolderButton.Text = "Open Output Folder";
        this.openOutputFolderButton.Click += this.openOutputFolderButton_Click;

        // hideBaseGameCheckBox
        this.hideBaseGameCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.hideBaseGameCheckBox.AutoSize = true;
        this.hideBaseGameCheckBox.Location = new System.Drawing.Point(526, 80);
        this.hideBaseGameCheckBox.Text = "Hide Base Game";
        this.hideBaseGameCheckBox.Checked = true;
        this.hideBaseGameCheckBox.CheckState = CheckState.Checked;
        this.hideBaseGameCheckBox.CheckedChanged += this.filterCheckBox_CheckedChanged;

        // hideVrPlanesCheckBox
        this.hideVrPlanesCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.hideVrPlanesCheckBox.AutoSize = true;
        this.hideVrPlanesCheckBox.Location = new System.Drawing.Point(526, 105);
        this.hideVrPlanesCheckBox.Text = "Hide VR Planes";
        this.hideVrPlanesCheckBox.Checked = true;
        this.hideVrPlanesCheckBox.CheckState = CheckState.Checked;
        this.hideVrPlanesCheckBox.CheckedChanged += this.filterCheckBox_CheckedChanged;

        // darkModeCheckBox
        this.darkModeCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.darkModeCheckBox.AutoSize = true;
        this.darkModeCheckBox.Location = new System.Drawing.Point(430, 84);
        this.darkModeCheckBox.Text = "Dark mode";
        this.darkModeCheckBox.CheckedChanged += this.darkModeCheckBox_CheckedChanged;

        // searchTextBox
        this.searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.searchTextBox.Location = new System.Drawing.Point(12, 108);
        this.searchTextBox.Size = new System.Drawing.Size(508, 23);
        this.searchTextBox.PlaceholderText = "Search PlaneStringID or PlaneID";
        this.searchTextBox.TextChanged += this.searchTextBox_TextChanged;

        // planesGrid
        this.planesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.planesGrid.Location = new System.Drawing.Point(12, 135);
        this.planesGrid.Size = new System.Drawing.Size(606, 238);
        this.planesGrid.AllowUserToAddRows = false;
        this.planesGrid.AllowUserToDeleteRows = false;
        this.planesGrid.AllowUserToResizeRows = false;
        this.planesGrid.RowHeadersVisible = false;
        this.planesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.planesGrid.MultiSelect = true;
        this.planesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "",
            DataPropertyName = "Enabled",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 30,
            Resizable = DataGridViewTriState.False
        };
        var planeIdColumn = new DataGridViewTextBoxColumn
        {
            Name = "PlaneID",
            HeaderText = "PlaneID",
            DataPropertyName = "PlaneID",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 60,
            ReadOnly = true
        };
        var idColumn = new DataGridViewTextBoxColumn
        {
            Name = "PlaneStringID",
            HeaderText = "PlaneStringID",
            DataPropertyName = "PlaneStringID",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true
        };
        this.planesGrid.Columns.Add(enabledColumn);
        this.planesGrid.Columns.Add(planeIdColumn);
        this.planesGrid.Columns.Add(idColumn);
        this.planesGrid.CurrentCellDirtyStateChanged += this.planesGrid_CurrentCellDirtyStateChanged;
        this.planesGrid.CellValueChanged += this.planesGrid_CellValueChanged;
        this.planesGrid.CellMouseDown += this.planesGrid_CellMouseDown;

        // logTextBox
        this.logTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.logTextBox.Location = new System.Drawing.Point(12, 379);
        this.logTextBox.Size = new System.Drawing.Size(606, 80);
        this.logTextBox.Multiline = true;
        this.logTextBox.ScrollBars = ScrollBars.Vertical;
        this.logTextBox.ReadOnly = true;

        // MainForm
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(630, 471);
        this.Controls.Add(this.pakPathTextBox);
        this.Controls.Add(this.browsePakButton);
        this.Controls.Add(this.unrealPakPathTextBox);
        this.Controls.Add(this.browseUnrealPakButton);
        this.Controls.Add(this.unpackAndLoadButton);
        this.Controls.Add(this.applyAndSaveButton);
        this.Controls.Add(this.openOutputFolderButton);
        this.Controls.Add(this.hideBaseGameCheckBox);
        this.Controls.Add(this.hideVrPlanesCheckBox);
        this.Controls.Add(this.darkModeCheckBox);
        this.Controls.Add(this.searchTextBox);
        this.Controls.Add(this.planesGrid);
        this.Controls.Add(this.logTextBox);
        this.MinimumSize = new System.Drawing.Size(650, 510);
        this.Text = "Ace Combat 7 Disable-Enabler";
        this.FormClosing += this.MainForm_FormClosing;

        ((System.ComponentModel.ISupportInitialize)(this.planesGrid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}

