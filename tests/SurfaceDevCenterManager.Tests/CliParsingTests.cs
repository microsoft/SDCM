/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Services;
using Xunit;

namespace SurfaceDevCenterManager.Tests;

public class CliParsingTests
{
    private static RootCommand BuildTree()
    {
        return CommandTreeBuilder.Build(new ServiceProviderAccessor());
    }

    [Theory]
    [InlineData("product create --input file.json")]
    [InlineData("product list")]
    [InlineData("product list --product-id 42")]
    [InlineData("submission create --product-id 1 --input file.json")]
    [InlineData("submission list --product-id 1")]
    [InlineData("submission commit --product-id 1 --submission-id 2")]
    [InlineData("submission upload --product-id 1 --submission-id 2 --package pkg.zip")]
    [InlineData("submission download --product-id 1 --submission-id 2 --output-file out.zip")]
    [InlineData("submission wait --product-id 1 --submission-id 2")]
    [InlineData("submission metadata create --product-id 1 --submission-id 2")]
    [InlineData("submission metadata download --product-id 1 --submission-id 2 --output-file meta.zip")]
    [InlineData("shipping-label create --product-id 1 --submission-id 2 --input file.json")]
    [InlineData("shipping-label list --product-id 1 --submission-id 2")]
    [InlineData("shipping-label wait --product-id 1 --submission-id 2 --shipping-label-id 3")]
    [InlineData("partner-submission list --publisher-id p --product-id 1 --submission-id 2")]
    [InlineData("partner-submission translate --publisher-id p --product-id 1 --submission-id 2")]
    [InlineData("audience list")]
    [InlineData("config path")]
    [InlineData("config init")]
    public void ValidCommandLines_ParseWithoutErrors(string commandLine)
    {
        RootCommand root = BuildTree();
        ParseResult result = root.Parse(commandLine);

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("product create")] // missing --input
    [InlineData("submission create --product-id 1")] // missing --input
    [InlineData("submission commit --product-id 1")] // missing --submission-id
    [InlineData("submission upload --product-id 1 --submission-id 2")] // missing --package
    [InlineData("shipping-label wait --product-id 1 --submission-id 2")] // missing --shipping-label-id
    [InlineData("partner-submission list --product-id 1 --submission-id 2")] // missing --publisher-id
    public void MissingRequiredOptions_ProduceParseErrors(string commandLine)
    {
        RootCommand root = BuildTree();
        ParseResult result = root.Parse(commandLine);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void GlobalOptions_ResolveRecursivelyOnLeafCommands()
    {
        RootCommand root = BuildTree();
        ParseResult result = root.Parse("product list --profile ci --auth client-secret --output json -v");

        Assert.Empty(result.Errors);
        Assert.Equal("ci", result.GetValue(GlobalOptions.Profile));
        Assert.Equal("client-secret", result.GetValue(GlobalOptions.Auth));
        Assert.Equal("json", result.GetValue(GlobalOptions.Output));
        Assert.True(result.GetValue(GlobalOptions.Verbose));
    }

    [Fact]
    public void GlobalOptions_BeforeOrAfterSubcommand_BothWork()
    {
        RootCommand root = BuildTree();
        ParseResult before = root.Parse("--profile ci product list");
        ParseResult after = root.Parse("product list --profile ci");

        Assert.Empty(before.Errors);
        Assert.Empty(after.Errors);
        Assert.Equal("ci", before.GetValue(GlobalOptions.Profile));
        Assert.Equal("ci", after.GetValue(GlobalOptions.Profile));
    }

    [Theory]
    [InlineData("auto", AuthMode.Auto)]
    [InlineData("managed-identity", AuthMode.ManagedIdentity)]
    [InlineData("client-secret", AuthMode.ClientSecret)]
    [InlineData("interactive", AuthMode.Interactive)]
    public void AuthMode_ParsesKebabCase(string value, AuthMode expected)
    {
        Assert.True(EnumParsing.TryParseKebab(value, out AuthMode actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("never", AadPromptMode.Never)]
    [InlineData("prompt", AadPromptMode.Prompt)]
    [InlineData("always", AadPromptMode.Always)]
    [InlineData("refresh-session", AadPromptMode.RefreshSession)]
    [InlineData("select-account", AadPromptMode.SelectAccount)]
    public void AadPromptMode_ParsesKebabCase(string value, AadPromptMode expected)
    {
        Assert.True(EnumParsing.TryParseKebab(value, out AadPromptMode actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParsing_InvalidValue_ReturnsFalse()
    {
        Assert.False(EnumParsing.TryParseKebab<AuthMode>("not-a-real-mode", out _));
    }

    [Fact]
    public void EnumParsing_ParseKebabOrThrow_ThrowsWithAllowedValuesListed()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => EnumParsing.ParseKebabOrThrow<AuthMode>("bogus", "--auth"));

        Assert.Contains("managed-identity", ex.Message);
    }
}
