using System;
using System.Collections.Generic;

namespace DisableEnabler;

public sealed class PakScanSelection
{
    public string? SourcePak { get; init; }
    public IReadOnlyList<string> BlacklistedDePaks { get; init; } = Array.Empty<string>();
}
