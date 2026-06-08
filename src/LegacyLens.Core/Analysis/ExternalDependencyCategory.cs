namespace LegacyLens.Core.Analysis;

public enum ExternalDependencyCategory
{
    Database,
    HttpApi,
    WcfServiceEndpoint,
    MessagingQueue,
    FileSystemFileShare,
    EmailSmtp,
    CacheDistributedState,
    AuthenticationIdentityProvider,
    CloudService,
    PrivatePackageFeed,
    ExternalAssemblyVendorDll,
    UnknownRequiresReview
}