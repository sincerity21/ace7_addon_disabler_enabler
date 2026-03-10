namespace DisableEnabler;

public class PlaneDataRow
{
    public string PlaneStringID { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int PlaneID { get; set; }
    public int OriginalPlaneID { get; set; }
}

