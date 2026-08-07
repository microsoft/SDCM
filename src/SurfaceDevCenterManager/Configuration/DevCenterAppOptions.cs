/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Configuration;

/// <summary>
///     Non-secret defaults bound from the "DevCenter" section of appsettings.json.
///     Values here are overridden per-profile by authconfig.json when present.
/// </summary>
public sealed class DevCenterAppOptions
{
    public const string SectionName = "DevCenter";

    public string Url { get; set; } = "https://manage.devcenter.microsoft.com";

    public string UrlPrefix { get; set; } = "v2.0/my";

    public string Authority { get; set; } = "https://login.microsoftonline.com/organizations/";

    public string RedirectUri { get; set; } = "http://localhost";
}
