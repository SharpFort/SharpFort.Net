/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

namespace SharpFort.AspNetCore.Authentication.OAuth.Gitee;

/// <summary>
/// Default values used by the Gitee authentication middleware.
/// </summary>
public static class GiteeAuthenticationDefaults
{
    /// <summary>
    /// Default value for <c>AuthenticationScheme.Name</c>.
    /// </summary>
    public const string AuthenticationScheme = "Gitee";

    /// <summary>
    /// Default value for <c>AuthenticationScheme.DisplayName</c>.
    /// </summary>
    public static readonly string DisplayName = "Gitee";

    /// <summary>
    /// Default value for <c>AuthenticationSchemeOptions.ClaimsIssuer</c>.
    /// </summary>
    public static readonly string Issuer = "Gitee";

    /// <summary>
    /// Default value for <c>RemoteAuthenticationOptions.CallbackPath</c>.
    /// </summary>
    public static readonly string CallbackPath = "/signin-gitee";

    /// <summary>
    /// Default value for <c>OAuthOptions.AuthorizationEndpoint</c>.
    /// </summary>
    public static readonly string AuthorizationEndpoint = "https://gitee.com/oauth/authorize";

    /// <summary>
    /// Default value for <c>OAuthOptions.TokenEndpoint</c>.
    /// </summary>
    public static readonly string TokenEndpoint = "https://gitee.com/oauth/token";

    /// <summary>
    /// Default value for <c>OAuthOptions.UserInformationEndpoint</c>.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://gitee.com/api/v5/user";

    /// <summary>
    /// Default value for <c>GiteeAuthenticationOptions.UserEmailsEndpoint</c>.
    /// </summary>
    public static readonly string UserEmailsEndpoint = "https://gitee.com/api/v5/emails";
}
