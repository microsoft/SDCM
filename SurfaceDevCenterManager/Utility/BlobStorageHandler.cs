/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.Utility
{
    internal class BlobStorageHandler
    {
        private readonly CloudBlockBlob blob;
        private const int BlockSize = 256 * 1024;

        public BlobStorageHandler(string SASUrl)
        {
            blob = new CloudBlockBlob(new Uri(SASUrl));
        }

        public async Task<bool> Upload(string filepath)
        {
            fileSize = new System.IO.FileInfo(filepath).Length;
            Console.Write("  0%");
            await blob.UploadFromFileAsync(filepath,
                AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(),
                new OperationContext(),
                new Progress<Microsoft.WindowsAzure.Storage.Core.Util.StorageProgress>(ReportProgress),
                new System.Threading.CancellationToken());
            Console.WriteLine();
            return true;
        }

        public void ReportProgress(Microsoft.WindowsAzure.Storage.Core.Util.StorageProgress value)
        {
            long percent = (int)(value.BytesTransferred * 100) / fileSize;
            Console.Write("\b\b\b\b{0,3:##0}%", percent);
        }

        private long fileSize;

        public async Task<bool> Download(string filepath)
        {
            blob.FetchAttributes();
            fileSize = blob.Properties.Length;
            Console.Write("  0%");
            await blob.DownloadToFileAsync(filepath, FileMode.OpenOrCreate,
                AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(),
                new OperationContext(),
                new Progress<Microsoft.WindowsAzure.Storage.Core.Util.StorageProgress>(ReportProgress),
                new System.Threading.CancellationToken());
            Console.WriteLine();
            return true;
        }
    }
}
