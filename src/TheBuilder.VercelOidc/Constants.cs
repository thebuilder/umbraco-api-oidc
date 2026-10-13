namespace TheBuilder.VercelOidc;

internal static class Constants
{
    public const string PackageName = "TheBuilder.VercelOidc";

    /// <summary>
    ///     Where the Delivery API lives. Umbraco 14 and later fixed the backoffice path at /umbraco, so the
    ///     Delivery API's content and media routes all sit under this prefix.
    /// </summary>
    public const string DeliveryApiPathPrefix = "/umbraco/delivery/api";

    public const string VercelOidcHost = "https://oidc.vercel.com";

    public static class Claims
    {
        public const string Owner = "owner";
        public const string OwnerId = "owner_id";
        public const string Project = "project";
        public const string ProjectId = "project_id";
        public const string Environment = "environment";
    }
}
