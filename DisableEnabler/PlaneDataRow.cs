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

    /// <summary>Display name from addon_database.json (not written to PAK).</summary>
    public string PlaneName { get; set; } = string.Empty;

    /// <summary>Mod column label (Notes, or URL when Notes is empty).</summary>
    public string ModText { get; set; } = string.Empty;

    /// <summary>When set, Mod column is a clickable link to this URL.</summary>
    public string ModUrl { get; set; } = string.Empty;

    /// <summary>Notes column label (Notes2 from database).</summary>
    public string NotesText { get; set; } = string.Empty;
}

