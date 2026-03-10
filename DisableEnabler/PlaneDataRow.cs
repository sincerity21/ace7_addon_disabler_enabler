namespace DisableEnabler;

public class PlaneDataRow
{
    public string PlaneStringID { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int PlaneID { get; set; }
    public int OriginalPlaneID { get; set; }
    /// <summary>TargetMode as read from the asset (before any disable overwrite).</summary>
    public string TargetMode { get; set; } = string.Empty;
    /// <summary>DLCID as read from the asset (before any disable overwrite).</summary>
    public string DLCID { get; set; } = string.Empty;
}

