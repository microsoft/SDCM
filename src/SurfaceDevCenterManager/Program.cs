/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SurfaceDevCenterManager;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Configuration;
using SurfaceDevCenterManager.Services;

// See the "Architecture" section of the modernization plan: the command tree (with every leaf's
// action already attached) is built and parsed first, purely to learn the global option values
// needed to configure the generic host - particularly which authconfig.json to load. The actions
// themselves don't run services.GetRequiredService(...) until ParseResult.InvokeAsync() below, by
// which point the host has been built and ServiceProviderAccessor.Provider has been assigned.
ServiceProviderAccessor accessor = new();
RootCommand rootCommand = CommandTreeBuilder.Build(accessor);
ParseResult parseResult = rootCommand.Parse(args);

string? explicitConfigPath = parseResult.GetValue(GlobalOptions.Config);
bool verbose = parseResult.GetValue(GlobalOptions.Verbose);

OutputFormat outputFormat;
if (!EnumParsing.TryParseKebab(parseResult.GetValue(GlobalOptions.Output), out outputFormat))
{
    await Console.Error.WriteLineAsync(
        $"Invalid value for --output: '{parseResult.GetValue(GlobalOptions.Output)}'. Allowed values: text, json.");
    return (int)ExitCode.InvalidArguments;
}

string? authConfigPath = ConfigPathResolver.Resolve(explicitConfigPath);

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", true, false);

if (authConfigPath != null)
{
    builder.Configuration.AddJsonFile(authConfigPath, true, false);
}

builder.Configuration.AddEnvironmentVariables("SDCM_");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
// Diagnostics only, and always on stderr: --output json must keep stdout as valid, parseable JSON.
builder.Services.Configure<ConsoleLoggerOptions>(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.AddFilter(null, verbose ? LogLevel.Debug : LogLevel.Warning);

builder.Services.Configure<DevCenterAppOptions>(builder.Configuration.GetSection(DevCenterAppOptions.SectionName));
builder.Services.Configure<AuthConfigEntry>(builder.Configuration);
builder.Services.AddSdcmServices(outputFormat);

using IHost host = builder.Build();
accessor.Provider = host.Services;

return await parseResult.InvokeAsync().ConfigureAwait(false);
