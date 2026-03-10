namespace DisableEnabler;

/// <summary>
/// One entry in the export/import JSON for plane enable state.
/// </summary>
public class PlaneStateExportEntry
{
    public string PlaneStringID { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    /// <summary>DLCID to restore when re-enabling. Saved so it can be applied from the JSON file.</summary>
    public string? OriginalDLCID { get; set; }
}
