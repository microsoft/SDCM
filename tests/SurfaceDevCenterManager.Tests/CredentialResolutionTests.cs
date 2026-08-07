/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Extensions.Options;
using SurfaceDevCenterManager.Configuration;
using SurfaceDevCenterManager.Services;
using Xunit;

namespace SurfaceDevCenterManager.Tests;

public class CredentialResolutionTests
{
    private static CredentialsProvider CreateProvider(AuthConfigEntry authConfig, DevCenterAppOptions? appOptions = null)
    {
        return new CredentialsProvider(Options.Create(authConfig), Options.Create(appOptions ?? new DevCenterAppOptions()));
    }

    private static AuthProfile ValidProfile(string? key = null, string? managedIdentityClientId = null)
    {
        return new AuthProfile
        {
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            Key = key,
            ManagedIdentityClientId = managedIdentityClientId
        };
    }

    [Fact]
    public void UnknownProfile_Throws()
    {
        CredentialsProvider provider = CreateProvider(new AuthConfigEntry());

        Assert.Throws<InvalidOperationException>(() => provider.Resolve("default", AuthMode.Auto));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "22222222-2222-2222-2222-222222222222")]
    [InlineData("11111111-1111-1111-1111-111111111111", "00000000-0000-0000-0000-000000000000")]
    [InlineData(null, "22222222-2222-2222-2222-222222222222")]
    public void PlaceholderProfile_Throws(string? tenantId, string? clientId)
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = new AuthProfile { TenantId = tenantId, ClientId = clientId } }
        };
        CredentialsProvider provider = CreateProvider(config);

        Assert.Throws<InvalidOperationException>(() => provider.Resolve("default", AuthMode.Auto));
    }

    [Fact]
    public void Auto_PrefersManagedIdentity_WhenConfigured()
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = ValidProfile(key: "secret", managedIdentityClientId: "mi-client-id") }
        };
        CredentialsProvider provider = CreateProvider(config);

        ResolvedCredentials resolved = provider.Resolve("default", AuthMode.Auto);

        Assert.Equal(AuthMode.ManagedIdentity, resolved.EffectiveMode);
        Assert.Equal("mi-client-id", resolved.LibraryCredentials!.ManagedIdentityClientId);
    }

    [Fact]
    public void Auto_FallsBackToClientSecret_WhenNoManagedIdentity()
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = ValidProfile(key: "secret") }
        };
        CredentialsProvider provider = CreateProvider(config);

        ResolvedCredentials resolved = provider.Resolve("default", AuthMode.Auto);

        Assert.Equal(AuthMode.ClientSecret, resolved.EffectiveMode);
        Assert.Equal("secret", resolved.LibraryCredentials!.Key);
    }

    [Fact]
    public void Auto_FallsBackToInteractive_WhenNothingElseConfigured()
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = ValidProfile() }
        };
        CredentialsProvider provider = CreateProvider(config);

        ResolvedCredentials resolved = provider.Resolve("default", AuthMode.Auto);

        Assert.Equal(AuthMode.Interactive, resolved.EffectiveMode);
        Assert.Null(resolved.LibraryCredentials);
    }

    [Fact]
    public void ManagedIdentity_WithoutClientId_Throws()
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = ValidProfile() }
        };
        CredentialsProvider provider = CreateProvider(config);

        Assert.Throws<InvalidOperationException>(() => provider.Resolve("default", AuthMode.ManagedIdentity));
    }

    [Fact]
    public void ClientSecret_WithoutKey_Throws()
    {
        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = ValidProfile() }
        };
        CredentialsProvider provider = CreateProvider(config);

        Assert.Throws<InvalidOperationException>(() => provider.Resolve("default", AuthMode.ClientSecret));
    }

    [Fact]
    public void ProfileUrl_OverridesAppDefaults()
    {
        AuthProfile profile = ValidProfile(key: "secret");
        profile.Url = "https://example.test";
        profile.UrlPrefix = "v3/my";

        AuthConfigEntry config = new()
        {
            Profiles = { ["default"] = profile }
        };
        CredentialsProvider provider = CreateProvider(config, new DevCenterAppOptions { Url = "https://manage.devcenter.microsoft.com" });

        ResolvedCredentials resolved = provider.Resolve("default", AuthMode.ClientSecret);

        Assert.Equal("https://example.test", resolved.Url);
        Assert.Equal("v3/my", resolved.UrlPrefix);
    }
}
