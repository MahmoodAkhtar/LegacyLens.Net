namespace LegacyLens.Core.Analysis;

public sealed class UpgradeBlockersReport
{
    public string? RequestedUpgradeTarget { get; init; }

    public IReadOnlyList<UpgradeBlocker> Blockers { get; init; } =
        Array.Empty<UpgradeBlocker>();
}