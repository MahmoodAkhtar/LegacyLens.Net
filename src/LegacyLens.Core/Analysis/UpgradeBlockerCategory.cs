namespace LegacyLens.Core.Analysis;

public enum UpgradeBlockerCategory
{
    LegacyAspNetSystemWeb,
    WcfServiceModel,
    Ef6EdmxDataAccess,
    PackageManagement,
    DirectAssemblyReferences,
    ConfigurationRuntimeCoupling,
    WindowsOnlyPlatformSpecificApis,
    CustomBuildMsBuildBehaviour,
    UnknownRequiresManualReview
}