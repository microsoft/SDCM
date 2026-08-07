/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.Utility;
using Microsoft.Extensions.Options;
using SurfaceDevCenterManager.Configuration;

namespace SurfaceDevCenterManager.Services;

public sealed class CredentialsProvider(
    IOptions<AuthConfigEntry> authConfig,
    IOptions<DevCenterAppOptions> appOptions) : ICredentialsProvider
{
    public ResolvedCredentials Resolve(string profileName, AuthMode mode)
    {
        if (!authConfig.Value.Profiles.TryGetValue(profileName, out AuthProfile? profile))
        {
            throw new InvalidOperationException(
                $"No profile named '{profileName}' was found in authconfig.json. Run 'sdcm config path' to see " +
                "where sdcm looks for it, or 'sdcm config init' to create a starter file.");
        }

        if (profile.IsPlaceholder())
        {
            throw new InvalidOperationException(
                $"Profile '{profileName}' still contains placeholder values for tenantId/clientId. Edit the file " +
                "reported by 'sdcm config path' and fill in your app registration's details.");
        }

        DevCenterAppOptions defaults = appOptions.Value;
        string url = profile.Url ?? defaults.Url;
        string urlPrefix = profile.UrlPrefix ?? defaults.UrlPrefix;
        string authority = profile.Authority ?? defaults.Authority;

        AuthMode effective = mode == AuthMode.Auto ? DetermineAutoMode(profile) : mode;

        if (effective == AuthMode.Interactive)
        {
            return new ResolvedCredentials(null, effective, profile, url, urlPrefix, authority);
        }

        AuthorizationHandlerCredentials credentials = new()
        {
            TenantId = profile.TenantId,
            ClientId = profile.ClientId,
            Authority = authority,
            Url = new Uri(url, UriKind.Absolute),
            UrlPrefix = new Uri(urlPrefix, UriKind.Relative)
        };

        switch (effective)
        {
            case AuthMode.ManagedIdentity:
                if (string.IsNullOrWhiteSpace(profile.ManagedIdentityClientId))
                {
                    throw new InvalidOperationException(
                        $"Profile '{profileName}' has no managedIdentityClientId configured; " +
                        "--auth managed-identity requires one.");
                }

                credentials.ManagedIdentityClientId = profile.ManagedIdentityClientId;
                credentials.Scope = url.TrimEnd('/') + "/.default";
                break;

            case AuthMode.ClientSecret:
                if (string.IsNullOrWhiteSpace(profile.Key))
                {
                    throw new InvalidOperationException(
                        $"Profile '{profileName}' has no client secret ('key') configured; " +
                        "--auth client-secret requires one.");
                }

                credentials.Key = profile.Key;
                break;
        }

        return new ResolvedCredentials(credentials, effective, profile, url, urlPrefix, authority);
    }

    private static AuthMode DetermineAutoMode(AuthProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.ManagedIdentityClientId))
        {
            return AuthMode.ManagedIdentity;
        }

        if (!string.IsNullOrWhiteSpace(profile.Key))
        {
            return AuthMode.ClientSecret;
        }

        return AuthMode.Interactive;
    }
}
