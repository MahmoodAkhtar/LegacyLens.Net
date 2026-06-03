namespace LegacyLens.Core.Analysis;

public sealed class ModernisationReviewPrioritiser
{
    public IReadOnlyList<ModernisationReviewArea> Prioritise(
        IReadOnlyList<ModernisationHint> hints)
    {
        ArgumentNullException.ThrowIfNull(hints);

        return hints
            .GroupBy(GetReviewArea, StringComparer.OrdinalIgnoreCase)
            .Select(CreateReviewArea)
            .OrderByDescending(x => GetSeverityRank(x.HighestSeverity))
            .ThenByDescending(x => x.RiskCount)
            .ThenByDescending(x => x.WarningCount)
            .ThenByDescending(x => x.InfoCount)
            .ThenBy(x => x.Area)
            .ToList();
    }

    private static ModernisationReviewArea CreateReviewArea(
        IGrouping<string, ModernisationHint> group)
    {
        var hints = group.ToList();

        var riskCount = hints.Count(x => x.Severity == ModernisationHintSeverity.Risk);
        var warningCount = hints.Count(x => x.Severity == ModernisationHintSeverity.Warning);
        var infoCount = hints.Count(x => x.Severity == ModernisationHintSeverity.Info);

        var highestSeverity = hints
            .Select(x => x.Severity)
            .OrderByDescending(GetSeverityRank)
            .First();

        return new ModernisationReviewArea
        {
            Area = group.Key,
            HighestSeverity = highestSeverity,
            RiskCount = riskCount,
            WarningCount = warningCount,
            InfoCount = infoCount,
            Summary = BuildSummary(group.Key, riskCount, warningCount, infoCount)
        };
    }

    private static string GetReviewArea(ModernisationHint hint)
    {
        if (IsWcfHint(hint))
        {
            return "WCF migration";
        }

        if (IsRoutingHint(hint))
        {
            return "Routing review";
        }

        if (IsStartupOrPipelineHint(hint))
        {
            return "Startup and request pipeline review";
        }

        if (IsLegacyAspNetHint(hint))
        {
            return "Legacy ASP.NET migration";
        }

        if (hint.Area.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
        {
            return "Configuration review";
        }

        if (hint.Area.Equals("Packages", StringComparison.OrdinalIgnoreCase))
        {
            return "Dependency review";
        }

        if (hint.Area.Equals("Target Framework", StringComparison.OrdinalIgnoreCase))
        {
            return "Target framework review";
        }

        if (hint.Area.Equals("Project Dependencies", StringComparison.OrdinalIgnoreCase))
        {
            return "Project dependency review";
        }

        return "Other review";
    }

    private static bool IsWcfHint(ModernisationHint hint)
    {
        return hint.Area.StartsWith("WCF", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("System.ServiceModel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyAspNetHint(ModernisationHint hint)
    {
        return hint.Area.StartsWith("Legacy ASP.NET", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRoutingHint(ModernisationHint hint)
    {
        return hint.Area.Contains("Routing", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("RouteConfig", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("MapHttpRoute", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("RoutePrefix", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("[Route]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStartupOrPipelineHint(ModernisationHint hint)
    {
        return hint.Area.Contains("Startup", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Contains("Bundling", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Contains("Filters", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Contains("Attributes", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("Application_Start", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("GlobalConfiguration.Configure", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("RegisterGlobalFilters", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("RegisterBundles", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSummary(
        string area,
        int riskCount,
        int warningCount,
        int infoCount)
    {
        var countSummary = $"{riskCount} risk, {warningCount} warning, {infoCount} info hint(s)";

        return area switch
        {
            "WCF migration" =>
                $"{countSummary}. Review service boundaries, bindings, security, timeout, payload, metadata, contract, and WCF package usage before choosing a migration approach.",

            "Legacy ASP.NET migration" =>
                $"{countSummary}. Review classic ASP.NET, System.Web, WebForms, ASMX, handlers, MVC, or Web API usage before planning an ASP.NET Core migration.",

            "Routing review" =>
                $"{countSummary}. Review conventional routes, attribute routes, area routes, and Web API route registrations to preserve URL and client compatibility.",

            "Startup and request pipeline review" =>
                $"{countSummary}. Review application startup, global filters, action attributes, bundling, and cross-cutting request behaviour that may need ASP.NET Core equivalents.",

            "Configuration review" =>
                $"{countSummary}. Review appSettings, connection strings, and custom configuration sections for runtime behaviour and external dependencies.",

            "Dependency review" =>
                $"{countSummary}. Review package dependencies that may affect migration, replacement, compatibility, or framework upgrade planning.",

            "Target framework review" =>
                $"{countSummary}. Review target frameworks to understand upgrade paths, .NET Framework dependencies, and modern .NET migration constraints.",

            "Project dependency review" =>
                $"{countSummary}. Review project coupling to understand which projects may be harder to refactor, split, or migrate independently.",

            _ =>
                $"{countSummary}. Review these findings as part of the initial modernisation assessment."
        };
    }

    private static int GetSeverityRank(ModernisationHintSeverity severity)
    {
        return severity switch
        {
            ModernisationHintSeverity.Risk => 3,
            ModernisationHintSeverity.Warning => 2,
            ModernisationHintSeverity.Info => 1,
            _ => 0
        };
    }
}