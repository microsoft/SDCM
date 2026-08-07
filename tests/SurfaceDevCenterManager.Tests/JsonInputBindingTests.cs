/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Json;
using Xunit;

namespace SurfaceDevCenterManager.Tests;

public class JsonInputBindingTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    private string WriteTempFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"sdcm-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (string file in _tempFiles)
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void FlattenedProductPayload_DeserializesIntoNewProduct()
    {
        string path = WriteTempFile("""
            {
              "productName": "ProductName_HLK",
              "testHarness": "HLK",
              "deviceType": "external",
              "requestedSignatures": ["WINDOWS_v100_X64_RS4_FULL"]
            }
            """);

        NewProduct product = InputFileReader.Read<NewProduct>(path);

        Assert.Equal("ProductName_HLK", product.ProductName);
        Assert.Equal("HLK", product.TestHarness);
        Assert.Equal("external", product.DeviceType);
        Assert.Equal(["WINDOWS_v100_X64_RS4_FULL"], product.RequestedSignatures);
    }

    [Fact]
    public void FlattenedSubmissionPayload_DeserializesIntoNewSubmission()
    {
        string path = WriteTempFile("""{ "name": "sub-1", "type": "initial" }""");

        NewSubmission submission = InputFileReader.Read<NewSubmission>(path);

        Assert.Equal("sub-1", submission.Name);
        Assert.Equal("initial", submission.Type);
    }

    [Fact]
    public void OldEnvelope_ProducesExplicitMigrationError()
    {
        string path = WriteTempFile("""
            {
              "createType": "product",
              "createProduct": { "productName": "x" }
            }
            """);

        InputFileException ex = Assert.Throws<InputFileException>(() => InputFileReader.Read<NewProduct>(path));

        Assert.Contains("createType", ex.Message);
        Assert.Contains("README", ex.Message);
    }

    [Fact]
    public void MalformedJson_ThrowsInputFileException()
    {
        string path = WriteTempFile("{ not valid json ");

        Assert.Throws<InputFileException>(() => InputFileReader.Read<NewProduct>(path));
    }

    [Fact]
    public void MissingFile_ThrowsInputFileException()
    {
        Assert.Throws<InputFileException>(() => InputFileReader.Read<NewProduct>("does-not-exist.json"));
    }

    [Fact]
    public void CaseInsensitivePropertyNames_StillBind()
    {
        string path = WriteTempFile("""{ "PRODUCTNAME": "case-test", "testharness": "HLK" }""");

        NewProduct product = InputFileReader.Read<NewProduct>(path);

        Assert.Equal("case-test", product.ProductName);
        Assert.Equal("HLK", product.TestHarness);
    }
}
