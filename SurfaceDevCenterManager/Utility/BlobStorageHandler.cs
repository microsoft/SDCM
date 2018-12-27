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

        /// <summary>
        /// Handles upload and download of files for HWDC Azure Blob Storage URLs
        /// </summary>
        /// <param name="SASUrl">URL String to the blob</param>
        public BlobStorageHandler(string SASUrl)
        {
            blob = new CloudBlockBlob(new Uri(SASUrl));
        }

        /// <summary>
        /// Uploads specified file to HWDC Azure Storage
        /// </summary>
        /// <param name="filepath">Path to the file to upload to the Azure Blob URL</param>
        /// <returns>True if the upload succeeded</returns>
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

        private void ReportProgress(Microsoft.WindowsAzure.Storage.Core.Util.StorageProgress value)
        {
            long percent = (int)(value.BytesTransferred * 100) / fileSize;
            Console.Write("\b\b\b\b{0,3:##0}%", percent);
        }

        private long fileSize;

        /// <summary>
        /// Downloads to specified file from HWDC Azure Storage
        /// </summary>
        /// <param name="filepath">Path to the file to download to from the Azure Blob URL</param>
        /// <returns>True if the download succeeded</returns>
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

        /// <summary>
        /// Downloads to specified file from HWDC Azure Storage as a string
        /// </summary>
        /// <returns>String representing the content from Azure Storage</returns>
        public async Task<string> DownloadToString()
        {
            return await blob.DownloadTextAsync();
        }
    }
}
