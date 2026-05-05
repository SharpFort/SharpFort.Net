/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

namespace SharpFort.AspNetCore.Authentication.OAuth.QQ;

/// <summary>
/// Default values for QQ authentication.
/// </summary>
public static class QQAuthenticationDefaults
{
    /// <summary>
    /// Default value for <c>AuthenticationScheme.Name</c>.
    /// </summary>
    public const string AuthenticationScheme = "QQ";

    /// <summary>
    /// Default value for <c>AuthenticationScheme.DisplayName</c>.
    /// </summary>
    public static readonly string DisplayName = "QQ";

    /// <summary>
    /// Default value for <c>AuthenticationSchemeOptions.ClaimsIssuer</c>.
    /// </summary>
    public static readonly string Issuer = "QQ";

    /// <summary>
    /// Default value for <c>RemoteAuthenticationOptions.CallbackPath</c>.
    /// </summary>
    public static readonly string CallbackPath = "/signin-qq";

    /// <summary>
    /// Default value for <c>OAuthOptions.AuthorizationEndpoint</c>.
    /// </summary>
    public static readonly string AuthorizationEndpoint = "https://graph.qq.com/oauth2.0/authorize";

    /// <summary>
    /// Default value for <c>OAuthOptions.TokenEndpoint</c>.
    /// </summary>
    public static readonly string TokenEndpoint = "https://graph.qq.com/oauth2.0/token";

    /// <summary>
    /// Default value for <c>QQAuthenticationOptions.UserIdentificationEndpoint</c>.
    /// </summary>
    public static readonly string UserIdentificationEndpoint = "https://graph.qq.com/oauth2.0/me";

    /// <summary>
    /// Default value for <c>OAuthOptions.UserInformationEndpoint</c>.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://graph.qq.com/user/get_user_info";
}
