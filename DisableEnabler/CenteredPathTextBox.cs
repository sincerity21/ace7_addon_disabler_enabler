using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace DisableEnabler;

/// <summary>
/// Bordered single-line field with an inner textbox vertically centered in the outer box.
/// </summary>
internal sealed class CenteredPathTextBox : Panel
{
    private const int InnerPadX = 4;

    private readonly TextBox _inner;

    public CenteredPathTextBox(bool readOnly = true)
    {
        BorderStyle = BorderStyle.FixedSingle;
        _inner = new TextBox
        {
            ReadOnly = readOnly,
            BorderStyle = BorderStyle.None,
            TabStop = !readOnly,
            Multiline = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        Controls.Add(_inner);
    }

    public string PlaceholderText
    {
        get => _inner.PlaceholderText;
        set => _inner.PlaceholderText = value;
    }

    public new event EventHandler? TextChanged
    {
        add => _inner.TextChanged += value;
        remove => _inner.TextChanged -= value;
    }

    [AllowNull]
    public override string Text
    {
        get => _inner.Text;
        set => _inner.Text = value ?? string.Empty;
    }

    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set
        {
            base.Font = value ?? Control.DefaultFont;
            _inner.Font = base.Font;
            LayoutInner();
        }
    }

    public void ApplyFieldColors(Color back, Color fore)
    {
        BackColor = back;
        _inner.BackColor = back;
        _inner.ForeColor = fore;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutInner();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!ReferenceEquals(_inner.Font, Font))
            _inner.Font = Font;
        LayoutInner();
    }

    private void LayoutInner()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        var h = _inner.PreferredHeight;
        var y = Math.Max(0, (ClientSize.Height - h) / 2);
        _inner.SetBounds(InnerPadX, y, Math.Max(0, ClientSize.Width - InnerPadX * 2), h);
    }
}
