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
    private Button packPakButton = null!;
    private Button openOutputFolderButton = null!;
    private DataGridView planesGrid = null!;
    private TextBox logTextBox = null!;

    private void InitializeComponent()
    {
        this.pakPathTextBox = new TextBox();
        this.browsePakButton = new Button();
        this.unrealPakPathTextBox = new TextBox();
        this.browseUnrealPakButton = new Button();
        this.unpackAndLoadButton = new Button();
        this.applyAndSaveButton = new Button();
        this.packPakButton = new Button();
        this.openOutputFolderButton = new Button();
        this.planesGrid = new DataGridView();
        this.logTextBox = new TextBox();

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
        this.applyAndSaveButton.Text = "Apply && Save UAsset";
        this.applyAndSaveButton.Click += this.applyAndSaveButton_Click;

        // packPakButton
        this.packPakButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.packPakButton.Location = new System.Drawing.Point(284, 80);
        this.packPakButton.Size = new System.Drawing.Size(90, 27);
        this.packPakButton.Text = "Pack PAK";
        this.packPakButton.Click += this.packPakButton_Click;

        // openOutputFolderButton
        this.openOutputFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.openOutputFolderButton.Location = new System.Drawing.Point(380, 80);
        this.openOutputFolderButton.Size = new System.Drawing.Size(140, 27);
        this.openOutputFolderButton.Text = "Open Output Folder";
        this.openOutputFolderButton.Click += this.openOutputFolderButton_Click;

        // planesGrid
        this.planesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.planesGrid.Location = new System.Drawing.Point(12, 113);
        this.planesGrid.Size = new System.Drawing.Size(606, 260);
        this.planesGrid.AllowUserToAddRows = false;
        this.planesGrid.AllowUserToDeleteRows = false;
        this.planesGrid.RowHeadersVisible = false;
        this.planesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.planesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled",
            Width = 80
        };
        var idColumn = new DataGridViewTextBoxColumn
        {
            Name = "PlaneStringID",
            HeaderText = "PlaneStringID",
            DataPropertyName = "PlaneStringID",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        this.planesGrid.Columns.Add(enabledColumn);
        this.planesGrid.Columns.Add(idColumn);

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
        this.Controls.Add(this.packPakButton);
        this.Controls.Add(this.openOutputFolderButton);
        this.Controls.Add(this.planesGrid);
        this.Controls.Add(this.logTextBox);
        this.MinimumSize = new System.Drawing.Size(650, 510);
        this.Text = "Ace Combat 7 Disable-Enabler";

        ((System.ComponentModel.ISupportInitialize)(this.planesGrid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}

