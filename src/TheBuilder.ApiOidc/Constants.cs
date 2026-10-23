namespace TheBuilder.ApiOidc;

internal static class Constants
{
    public const string PackageName = "TheBuilder.ApiOidc";

    /// <summary>
    ///     Where the Delivery API lives. Umbraco 14 and later fixed the backoffice path at /umbraco, so the
    ///     Delivery API's content and media routes all sit under this prefix.
    /// </summary>
    public const string DeliveryApiPathPrefix = "/umbraco/delivery/api";

    public static class Vercel
    {
        public const string Host = "https://oidc.vercel.com";
        public const string Owner = "owner";
        public const string Project = "project";
        public const string ProjectId = "project_id";
        public const string Environment = "environment";
    }

    public static class GitHubActions
    {
        public const string Issuer = "https://token.actions.githubusercontent.com";
        public const string Owner = "repository_owner";
        public const string Repository = "repository";
        public const string Ref = "ref";
        public const string Environment = "environment";
    }

    public static class Azure
    {
        public const string Host = "https://login.microsoftonline.com";
        /// <summary>Where v1.0 access tokens say they come from, though their keys are published under <see cref="Host" />.</summary>
        public const string V1Host = "https://sts.windows.net";
        public const string ObjectId = "oid";
    }
}
