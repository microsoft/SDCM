/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Handlers;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSdcmServices(this IServiceCollection services, OutputFormat outputFormat)
    {
        services.AddSingleton(new ConsoleOutputWriter(outputFormat));
        services.AddSingleton<IOutputWriter>(sp => sp.GetRequiredService<ConsoleOutputWriter>());
        services.AddSingleton<RunContext>();
        services.AddSingleton<IErrorReporter, ErrorReporter>();
        services.AddSingleton<ICredentialsProvider, CredentialsProvider>();
        services.AddSingleton<IAadTokenProvider, MsalAadTokenProvider>();
        services.AddSingleton<IDevCenterHandlerFactory, DevCenterHandlerFactory>();

        services.AddTransient<ProductCreateHandler>();
        services.AddTransient<ProductListHandler>();
        services.AddTransient<SubmissionCreateHandler>();
        services.AddTransient<SubmissionListHandler>();
        services.AddTransient<SubmissionCommitHandler>();
        services.AddTransient<SubmissionUploadHandler>();
        services.AddTransient<SubmissionDownloadHandler>();
        services.AddTransient<SubmissionWaitHandler>();
        services.AddTransient<SubmissionMetadataDownloadHandler>();
        services.AddTransient<SubmissionMetadataCreateHandler>();
        services.AddTransient<ShippingLabelCreateHandler>();
        services.AddTransient<ShippingLabelListHandler>();
        services.AddTransient<ShippingLabelWaitHandler>();
        services.AddTransient<PartnerSubmissionListHandler>();
        services.AddTransient<PartnerSubmissionTranslateHandler>();
        services.AddTransient<AudienceListHandler>();
        services.AddTransient<ConfigPathHandler>();
        services.AddTransient<ConfigInitHandler>();

        return services;
    }
}
