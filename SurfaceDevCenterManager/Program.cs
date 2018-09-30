/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mono.Options;
using Newtonsoft.Json;
using SurfaceDevCenterManager.DevCenterAPI;
using SurfaceDevCenterManager.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager
{
    public enum DevCenterHWSubmissionType
    {
        ShippingLabel = 0,
        Product = 1,
        Submission = 2,
        Invalid
    };

    public class CreateInput
    {
        [JsonProperty("createType")]
        public DevCenterHWSubmissionType CreateType { get; set; }

        [JsonProperty("createProduct")]
        public NewProduct CreateProduct { get; set; }

        [JsonProperty("createSubmission")]
        public NewSubmission CreateSubmission { get; set; }

        [JsonProperty("createShippingLabel")]
        public NewShippingLabel CreateShippingLabel { get; set; }
    }

    internal class Program
    {
        private static int verbosity;

        private static int Main(string[] args)
        {
            int result = -1;

            try
            {
                result = MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
                result = -1;
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                //Break debugger to look at command line output before the window disappears
                System.Diagnostics.Debugger.Break();
            }
            return result;
        }

        /// <summary>
        /// Processes command line args and calls into HWDC
        /// </summary>
        /// <returns>Returns 0 success, non-zero on error</returns>
        private static async Task<int> MainAsync(string[] args)
        {
            int retval = 0;
            bool show_help = false;
            string CreateOption = null;
            bool CommitOption = false;
            string ListOption = null;
            string ProductId = null;
            string SubmissionId = null;
            string ShippingLabelId = null;
            string DownloadOption = null;
            string MetadataOption = null;
            string SubmissionPackagePath = null;
            bool WaitOption = false;
            bool WaitForMetaData = false;
            bool AudienceOption = false;
            int OverrideServer = 0;
            string CredentialsOption = null;
            string AADAuthenticationOption = null;

            OptionSet p = new OptionSet() {
                { "c|create=",         "Path to json file with configuration to create", v => CreateOption = v },
                { "commit",            "Commit submission with given ID", v => CommitOption = true },
                { "l|list=",           "List a shippinglabel, product or submission", v => ListOption = v },
                { "u|upload=",         "Upload a package to a specific product and submission", v => SubmissionPackagePath = v },
                { "productid=",        "Specify a specific ProductId", v => ProductId = v },
                { "submissionid=",     "Specify a specific SubmissionId", v => SubmissionId = v },
                { "shippinglabelid=",  "Specify a specific ShippingLabelId", v => ShippingLabelId = v },
                { "v",                 "Increase debug message verbosity", v => { if (v != null) {++verbosity; }} },
                { "d|download=",       "Download a submission to current directory or folder specified", v => DownloadOption = v ?? Environment.CurrentDirectory },
                { "m|metadata=",       "Download a submission metadata to current directory or folder specified", v => MetadataOption = v ?? Environment.CurrentDirectory },
                { "h|help",            "Show this message and exit", v => show_help = v != null },
                { "w|wait",            "Wait for submission id to be done", v => WaitOption = true },
                { "waitmetadata",      "Wait for metadata to be done as well in a submission", v => WaitForMetaData = true },
                { "a|audience",        "List Audiences", v => AudienceOption = true },
                { "server=",           "Specify target DevCenter server from CredSelect enum", v => OverrideServer = int.Parse(v)   },
                { "creds=",            "Option to specify app credentials.  Options: FileOnly, AADOnly, AADThenFile (Default)", v => CredentialsOption = v },
                { "aad=",              "Option to specify AAD auth behavior.  Options: Never (Default), Prompt, Always", v => AADAuthenticationOption = v },
                { "?",                 "Show this message and exit", v => show_help = v != null },
            };

            Console.WriteLine("SurfaceDevCenterManager v" + Assembly.GetExecutingAssembly().GetName().Version);

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                ErrorParsingOptions(e.Message);
                Console.WriteLine("Try running with just '--help' for more information.");
                return -1;
            }

            if (show_help)
            {
                ShowHelp(p);
                return -2;
            }

            List<AuthorizationHandlerCredentials> myCreds = await GetApiCreds(CredentialsOption, AADAuthenticationOption);

            if (myCreds == null)
            {
                ErrorParsingOptions("Unable to get Dev Center Credentials");
                return -7;
            }

            if (OverrideServer < 0 || OverrideServer >= myCreds.Count)
            {
                ErrorParsingOptions("OverrideServer invalid - " + OverrideServer);
                return -5;
            }
            DevCenterHandler api = new DevCenterHandler(myCreds[OverrideServer]);

            if (CreateOption != null && (!File.Exists(CreateOption)))
            {
                ErrorParsingOptions("CreateOption invalid - " + CreateOption);
                return -3;
            }

            DevCenterHWSubmissionType ListOptionEnum = DevCenterHWSubmissionTypeCheck(ListOption);
            if (ListOption != null && ListOptionEnum == DevCenterHWSubmissionType.Invalid)
            {
                ErrorParsingOptions("ListOption invalid - " + ListOption);
                return -4;
            }

            if (CreateOption != null)
            {
                Console.WriteLine("> Create Option");

                CreateInput createInput = JsonConvert.DeserializeObject<CreateInput>(File.ReadAllText(CreateOption));

                if (DevCenterHWSubmissionType.Product == createInput.CreateType)
                {
                    DevCenterResponse<Product> ret = await api.NewProduct(createInput.CreateProduct);
                    if (ret == null || ret.Error != null)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine(ret.Error.Code ?? "");
                        Console.WriteLine(ret.Error.Message ?? "");
                        retval = -1001;
                    }
                    else
                    {
                        DumpProduct(ret.ReturnValue[0]);
                    }

                }
                else if (DevCenterHWSubmissionType.Submission == createInput.CreateType)
                {
                    if (ProductId == null)
                    {
                        Console.WriteLine("> ERROR: productid not specified");
                        retval = -2000;
                    }

                    if (retval == 0)
                    {
                        Console.WriteLine("> Creating Submission");
                        DevCenterResponse<Submission> ret = await api.NewSubmission(ProductId, createInput.CreateSubmission);
                        if (ret == null || ret.Error != null)
                        {
                            Console.WriteLine("ERROR");
                            Console.WriteLine(ret.Error.Code ?? "");
                            Console.WriteLine(ret.Error.Message ?? "");
                            retval = -2001;
                        }
                        else
                        {
                            DumpSubmission(ret.ReturnValue[0]);
                        }
                    }
                }
                else if (DevCenterHWSubmissionType.ShippingLabel == createInput.CreateType)
                {
                    if (ProductId == null)
                    {
                        Console.WriteLine("> ERROR: productid not specified");
                        retval = -2100;
                    }

                    if (SubmissionId == null)
                    {
                        Console.WriteLine("> ERROR: submissionid not specified");
                        retval = -2101;
                    }

                    if (retval == 0)
                    {
                        Console.WriteLine("> Get Driver Metadata");
                        string tmpfile = System.IO.Path.GetTempFileName();

                        DevCenterResponse<Submission> retSubmission = await api.GetSubmission(ProductId, SubmissionId);
                        if (retSubmission == null || retSubmission.Error != null)
                        {
                            Console.WriteLine("ERROR");
                            Console.WriteLine(retSubmission.Error.Code ?? "");
                            Console.WriteLine(retSubmission.Error.Message ?? "");
                            retval = -2102;
                        }

                        List<Submission> submissions = retSubmission.ReturnValue;
                        List<Download.Item> dls = submissions[0].Downloads.Items;
                        foreach (Download.Item dl in dls)
                        {
                            if (dl.Type.ToLower() == Download.Type.driverMetadata.ToString().ToLower())
                            {
                                Console.WriteLine("> driverMetadata Url: " + dl.Url);
                                Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(dl.Url.AbsoluteUri);
                                await bsh.Download(tmpfile);
                            }
                        }

                        string jsonContent = System.IO.File.ReadAllText(tmpfile);
                        DriverMetadata metadata = JsonConvert.DeserializeObject<DriverMetadata>(jsonContent);
                        System.IO.File.Delete(tmpfile);

                        List<HardwareId> labelHwids = new List<HardwareId>();

                        foreach (KeyValuePair<string, DriverMetadataDetails> bundleInfo in metadata.BundleInfoMap)
                        {
                            foreach (KeyValuePair<string, DriverMetadataInfDetails> infInfo in bundleInfo.Value.InfInfoMap)
                            {
                                foreach (KeyValuePair<string, Dictionary<string, DriverMetadataHWID>> osPnpInfo in infInfo.Value.OSPnPInfoMap)
                                {
                                    foreach (KeyValuePair<string, DriverMetadataHWID> pnpInfo in osPnpInfo.Value)
                                    {
                                        HardwareId labelHwid = new HardwareId
                                        {
                                            BundleId = bundleInfo.Key,
                                            InfId = infInfo.Key,
                                            OperatingSystemCode = osPnpInfo.Key,
                                            PnpString = pnpInfo.Key
                                        };
                                        labelHwids.Add(labelHwid);
                                    }
                                }
                            }
                        }

                        createInput.CreateShippingLabel.Targeting.HardwareIds = labelHwids;
                        createInput.CreateShippingLabel.PublishingSpecifications.GoLiveDate = DateTime.Now.AddDays(7);

                        Console.WriteLine("> Creating Shipping Label");
                        DevCenterResponse<ShippingLabel> ret = await api.NewShippingLabel(ProductId, SubmissionId, createInput.CreateShippingLabel);
                        if (ret == null || ret.Error != null)
                        {
                            Console.WriteLine("ERROR");
                            Console.WriteLine(ret.Error.Code ?? "");
                            Console.WriteLine(ret.Error.Message ?? "");
                            retval = -2103;
                        }
                        else
                        {
                            DumpShippingLabel(ret.ReturnValue[0]);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(">  Invalid Create Option selected");
                }
            }
            else if (CommitOption)
            {
                Console.WriteLine("> Commit Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = -3000;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = -3001;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Sending Commit");
                    if (await api.CommitSubmission(ProductId, SubmissionId))
                    {
                        Console.WriteLine("> Commit OK");
                    }
                    else
                    {
                        Console.WriteLine("> Commit Failed");
                        retval = -3002;
                    }
                }
            }
            else if (ListOption != null)
            {
                Console.WriteLine("> List Option {0}", ListOption);

                switch (ListOptionEnum)
                {
                    case DevCenterHWSubmissionType.Product:
                        {
                            DevCenterResponse<Product> ret = await api.GetProducts(ProductId);
                            if (ret == null || ret.Error != null)
                            {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(ret.Error.Code ?? "");
                                Console.WriteLine(ret.Error.Message ?? "");
                                retval = -2103;
                            }
                            else
                            {
                                List<DevCenterAPI.Product> products = ret.ReturnValue;
                                foreach (Product product in products)
                                {
                                    DumpProduct(product);
                                }
                            }
                        }
                        break;
                    case DevCenterHWSubmissionType.Submission:
                        {
                            DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                            if (ret == null || ret.Error != null)
                            {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(ret.Error.Code ?? "");
                                Console.WriteLine(ret.Error.Message ?? "");
                                retval = -2103;
                            }
                            else
                            {
                                List<DevCenterAPI.Submission> submissions = ret.ReturnValue;
                                foreach (Submission submission in submissions)
                                {
                                    DumpSubmission(submission);
                                }
                            }
                        }
                        break;
                    case DevCenterHWSubmissionType.ShippingLabel:
                        {
                            DevCenterResponse<ShippingLabel> ret = await api.GetShippingLabels(ProductId, SubmissionId, ShippingLabelId);
                            if (ret == null || ret.Error != null)
                            {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(ret.Error.Code ?? "");
                                Console.WriteLine(ret.Error.Message ?? "");
                                retval = -2103;
                            }
                            else
                            {
                                List<ShippingLabel> shippingLabels = ret.ReturnValue;
                                foreach (ShippingLabel shippingLabel in shippingLabels)
                                {
                                    DumpShippingLabel(shippingLabel);
                                }
                            }
                        }
                        break;
                    default:
                        Console.WriteLine(">  Invalid List Option selected");
                        break;
                }
            }
            else if (DownloadOption != null)
            {
                Console.WriteLine("> Download Option {0}", DownloadOption);

                // PLE has a habit of passing in strange file names like 'SB2: ' which are invalid paths.  This will catch these and throw a useful error
                string pathNameFull = System.IO.Path.GetFullPath(DownloadOption);

                string FileNamePart = System.IO.Path.GetFileName(DownloadOption);
                string PathNamePart = System.IO.Path.GetDirectoryName(DownloadOption);

                if (!System.IO.Directory.Exists(PathNamePart))
                {
                    Console.WriteLine("> ERROR: Output path does not exist: " + PathNamePart);
                    retval = -4010;
                }

                if (System.IO.File.Exists(DownloadOption))
                {
                    Console.WriteLine("> ERROR: Output file exists already: " + DownloadOption);
                    retval = -4011;
                }

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = -4000;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = -4001;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                    if (ret == null || ret.Error != null)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine(ret.Error.Code ?? "");
                        Console.WriteLine(ret.Error.Message ?? "");
                        retval = -4002;
                    }
                    List<Submission> submissions = ret.ReturnValue;
                    List<Download.Item> dls = submissions[0].Downloads.Items;
                    foreach (Download.Item dl in dls)
                    {
                        if (dl.Type.ToLower() == Download.Type.signedPackage.ToString().ToLower())
                        {
                            Console.WriteLine("> signedPackage Url: " + dl.Url);
                            Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(dl.Url.AbsoluteUri);
                            await bsh.Download(DownloadOption);
                        }
                    }
                }
            }
            else if (MetadataOption != null)
            {
                Console.WriteLine("> Metadata Download Option {0}", MetadataOption);

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = -4000;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = -4001;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                    if (ret == null || ret.Error != null)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine(ret.Error.Code ?? "");
                        Console.WriteLine(ret.Error.Message ?? "");
                        retval = -4002;
                    }
                    List<Submission> submissions = ret.ReturnValue;
                    List<Download.Item> dls = submissions[0].Downloads.Items;
                    bool foundMetaData = false;
                    foreach (Download.Item dl in dls)
                    {
                        if (dl.Type.ToLower() == Download.Type.driverMetadata.ToString().ToLower())
                        {
                            Console.WriteLine("> driverMetadata Url: " + dl.Url);
                            Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(dl.Url.AbsoluteUri);
                            await bsh.Download(MetadataOption);
                            foundMetaData = true;
                        }
                    }

                    if (!foundMetaData)
                    {
                        Console.WriteLine("> ERROR: No Metadata available for this submission");
                    }
                }
            }
            else if (SubmissionPackagePath != null)
            {
                Console.WriteLine("> Upload Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = -5000;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = -5001;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                    if (ret == null || ret.Error != null)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine(ret.Error.Code ?? "");
                        Console.WriteLine(ret.Error.Message ?? "");
                        retval = -5002;
                    }
                    List<Submission> submissions = ret.ReturnValue;
                    List<Download.Item> dls = submissions[0].Downloads.Items;
                    foreach (Download.Item dl in dls)
                    {
                        if (dl.Type.ToLower() == Download.Type.initialPackage.ToString().ToLower())
                        {
                            Console.WriteLine("> initialPackage Url: " + dl.Url);
                            Console.WriteLine("> Uploading Submission Package");
                            Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(dl.Url.AbsoluteUri);
                            await bsh.Upload(SubmissionPackagePath);
                        }
                    }
                }
            }
            else if (WaitOption)
            {
                Console.WriteLine("> Wait Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = -6000;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = -6001;
                }

                if (retval == 0)
                {
                    bool done = false;
                    string lastCurrentStep = "";
                    string lastState = "";

                    while (!done)
                    {
                        if (ShippingLabelId == null)
                        {
                            DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                            if (ret == null || ret.Error != null)
                            {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(ret.Error.Code ?? "");
                                Console.WriteLine(ret.Error.Message ?? "");
                                done = true;
                                retval = -7003;
                                break;
                            }
                            List<DevCenterAPI.Submission> submissions = ret.ReturnValue;
                            Submission sub = submissions[0];

                            if (!done)
                            {
                                if (sub.WorkflowStatus.CurrentStep != lastCurrentStep ||
                                    sub.WorkflowStatus.State != lastState)
                                {
                                    lastCurrentStep = sub.WorkflowStatus.CurrentStep;
                                    lastState = sub.WorkflowStatus.State;
                                    await DumpWorkflowStatus(sub.WorkflowStatus);
                                }

                                bool haveMetadata = false;
                                if (sub.Downloads != null)
                                {
                                    List<Download.Item> dls = sub.Downloads.Items;
                                    foreach (Download.Item dl in dls)
                                    {
                                        if (dl.Type.ToLower() == Download.Type.driverMetadata.ToString().ToLower())
                                        {
                                            Console.WriteLine("> driverMetadata Url: " + dl.Url);
                                            haveMetadata = true;
                                        }
                                    }
                                }

                                bool atLastStep = (lastCurrentStep == "finalizeIngestion" || lastState == "completed");


                                if (lastState == "failed")
                                {
                                    done = true;
                                    retval = -7001;
                                }
                                else if (atLastStep)
                                {
                                    if (WaitForMetaData)
                                    {
                                        if (haveMetadata)
                                        {
                                            done = true;
                                            Console.WriteLine("> Submission Ready with Metadata");
                                        }
                                    }
                                    else
                                    {
                                        done = true;
                                        Console.WriteLine("> Submission Ready");
                                    }
                                }

                                if (!done)
                                {
                                    await Task.Delay(5000);
                                }
                            }
                            else
                            {
                                Console.WriteLine("> Signed Package Ready");
                            }

                        }
                        else
                        {
                            DevCenterResponse<ShippingLabel> ret = await api.GetShippingLabels(ProductId, SubmissionId, ShippingLabelId);
                            if (ret == null || ret.Error != null)
                            {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(ret.Error.Code ?? "");
                                Console.WriteLine(ret.Error.Message ?? "");
                                done = true;
                                retval = -7002;
                                break;
                            }
                            List<ShippingLabel> shippingLabels = ret.ReturnValue;
                            ShippingLabel label = shippingLabels[0];

                            if (label.WorkflowStatus.CurrentStep != lastCurrentStep ||
                                label.WorkflowStatus.State != lastState)
                            {
                                lastCurrentStep = label.WorkflowStatus.CurrentStep;
                                lastState = label.WorkflowStatus.State;
                                await DumpWorkflowStatus(label.WorkflowStatus);
                            }

                            if (lastState == "failed")
                            {
                                done = true;
                                retval = -7000;
                            }
                            else if (lastCurrentStep == "microsoftApproval")
                            {
                                done = true;
                                Console.WriteLine("> Shipping Label Ready");
                            }
                            else
                            {
                                await Task.Delay(5000);
                            }
                        }
                    }
                    Console.WriteLine("> Done");
                }
            }
            else if (AudienceOption)
            {
                Console.WriteLine("> Audience Option");

                DevCenterResponse<Audience> ret = await api.GetAudiences();
                if (ret == null || ret.Error != null)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine(ret.Error.Code ?? "");
                    Console.WriteLine(ret.Error.Message ?? "");
                }
                else
                {
                    List<Audience> audiences = ret.ReturnValue;
                    foreach (Audience audience in audiences)
                    {
                        DumpAudience(audience);
                    }
                }

            }

            return retval;
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: sdcm [OPTIONS]+");
            Console.WriteLine("Manage drivers with Microsoft Dev Center for Surface");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        private static async Task<bool> DumpWorkflowStatus(WorkflowStatus workflowStatus)
        {
            bool retval = true;

            Console.WriteLine("> Step:  {0}", workflowStatus.CurrentStep);
            Console.WriteLine("> State: {0}", workflowStatus.State);
            if (workflowStatus.Messages != null)
            {
                foreach (string msg in workflowStatus.Messages)
                {
                    Console.WriteLine("> Message: {0}", msg);
                }
            }
            if (workflowStatus.ErrorReport != null)
            {
                Console.WriteLine("> Error Report:");
                string tmpfile = System.IO.Path.GetTempFileName();
                Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(workflowStatus.ErrorReport);
                retval = await bsh.Download(tmpfile);

                string errorContent = System.IO.File.ReadAllText(tmpfile);
                Console.WriteLine(errorContent);
                System.IO.File.Delete(tmpfile);
            }
            return retval;
        }

        private static void ErrorParsingOptions(string message)
        {
            Console.Write("Error Parsing Options: ");
            Console.WriteLine(message);
        }

        private static DevCenterHWSubmissionType DevCenterHWSubmissionTypeCheck(string input)
        {
            DevCenterHWSubmissionType retval = DevCenterHWSubmissionType.Invalid;
            if (input == null)
            {
                return retval;
            }

            foreach (DevCenterHWSubmissionType opt in Enum.GetValues(typeof(DevCenterHWSubmissionType)))
            {
                if (string.Compare(input, opt.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    retval = opt;
                    break;
                }
            }
            return retval;
        }

        private static void DumpProduct(DevCenterAPI.Product product)
        {
            Console.WriteLine("---- Product: " + product.Id);
            Console.WriteLine("         Name:      " + product.ProductName ?? "");
            Console.WriteLine("         Shared Id: " + product.SharedProductId ?? "");
            Console.WriteLine("         Type:      " + product.ProductType ?? "");
            Console.WriteLine("         DevType:   " + product.DeviceType ?? "");
            Console.WriteLine("         FWVer:     " + product.FirmwareVersion ?? "");
            Console.WriteLine("         isTestSign:" + product.IsTestSign ?? "");
            Console.WriteLine("         isFlightSign:" + product.IsFlightSign ?? "");
            Console.WriteLine("         createdBy: " + product.CreatedBy ?? "");
            Console.WriteLine("         updatedBy: " + product.UpdatedBy ?? "");
            Console.WriteLine("         createdDateTime:" + product.CreatedDateTime.ToString("s", CultureInfo.CurrentCulture));
            Console.WriteLine("         updatedDateTime:" + product.UpdatedDateTime.ToString("s", CultureInfo.CurrentCulture));
            Console.WriteLine("         announcementDate:" + product.AnnouncementDate.ToString("s", CultureInfo.CurrentCulture));
            Console.WriteLine("         testHarness:" + product.TestHarness ?? "");

            Console.WriteLine("         Signatures: ");
            foreach (string sig in product.RequestedSignatures)
            {
                Console.WriteLine("                   " + sig);
            }

            Console.WriteLine("         deviceMetadataIds: ");
            if (product.DeviceMetadataIds != null)
            {

                foreach (string sig in product.DeviceMetadataIds)
                {
                    Console.WriteLine("                   " + sig);
                }
            }
            Console.WriteLine("         selectedProductTypes: ");
            if (product.SelectedProductTypes != null)
            {
                foreach (KeyValuePair<string, string> entry in product.SelectedProductTypes)
                {
                    Console.WriteLine("                   " + entry.Key + ":" + entry.Value);
                }
            }


            Console.WriteLine("         marketingNames: ");
            if (product.MarketingNames != null)
            {
                foreach (string sig in product.MarketingNames)
                {
                    Console.WriteLine("                   " + sig);
                }
            }

            Console.WriteLine();
        }

        private static async void DumpShippingLabel(ShippingLabel shippingLabel)
        {
            Console.WriteLine("---- Shipping Label: " + shippingLabel.Id);
            Console.WriteLine("         Name:        " + shippingLabel.Name);
            Console.WriteLine("         ProductId:   " + shippingLabel.ProductId);
            Console.WriteLine("         SubmissionId:" + shippingLabel.SubmissionId);

            Console.WriteLine("         Publishing Specifications:");
            Console.WriteLine("           publishToWindows10s:" + shippingLabel.PublishingSpecifications.PublishToWindows10S);
            Console.WriteLine("           isDisclosureRestricted:" + shippingLabel.PublishingSpecifications.IsDisclosureRestricted);
            Console.WriteLine("           isAutoInstallOnApplicableSystems:" + shippingLabel.PublishingSpecifications.IsAutoInstallOnApplicableSystems);
            Console.WriteLine("           isAutoInstallDuringOSUpgrade:" + shippingLabel.PublishingSpecifications.IsAutoInstallDuringOSUpgrade);
            Console.WriteLine("           goLiveDate:" + shippingLabel.PublishingSpecifications.GoLiveDate);
            Console.WriteLine("           additionalInfoForMsApproval:");
            Console.WriteLine("               businessJustification:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.BusinessJustification);
            Console.WriteLine("               hasUiSoftware:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.HasUiSoftware);
            Console.WriteLine("               isCoEngineered:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.IsCoEngineered);
            Console.WriteLine("               isForUnreleasedHardware:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.IsForUnreleasedHardware);
            Console.WriteLine("               isRebootRequired:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.IsRebootRequired);
            Console.WriteLine("               microsoftContact:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.MicrosoftContact);
            Console.WriteLine("               validationsPerformed:" + shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.ValidationsPerformed);
            Console.WriteLine("               affectedOems:");
            foreach (string oem in shippingLabel.PublishingSpecifications.AdditionalInfoForMsApproval.AffectedOems)
            {
                Console.WriteLine("                            " + oem);
            }

            Console.WriteLine("         Links:");
            if (shippingLabel.Links != null)
            {
                foreach (Link link in shippingLabel.Links)
                {
                    DumpLink(link);
                }
            }
            Console.WriteLine("         Status:");
            if (shippingLabel.WorkflowStatus != null)
            {
                await DumpWorkflowStatus(shippingLabel.WorkflowStatus);
            }
            Console.WriteLine();
        }

        private static async void DumpSubmission(DevCenterAPI.Submission submission)
        {
            Console.WriteLine("---- Submission: " + submission.Id);
            Console.WriteLine("         Name:        " + submission.Name);
            Console.WriteLine("         ProductId:   " + submission.ProductId);
            Console.WriteLine("         type:        " + submission.Type ?? "");
            Console.WriteLine("         commitStatus:" + submission.CommitStatus ?? "");
            Console.WriteLine("         CreatedBy:   " + submission.CreatedBy ?? "");
            Console.WriteLine("         CreateTime:  " + submission.CreatedDateTime ?? "");
            Console.WriteLine("         Links:");
            if (submission.Links != null)
            {
                foreach (Link link in submission.Links)
                {
                    DumpLink(link);
                }
            }
            Console.WriteLine("         Status:");
            if (submission.WorkflowStatus != null)
            {
                await DumpWorkflowStatus(submission.WorkflowStatus);
            }
            Console.WriteLine("         Downloads:");
            Console.WriteLine("               - messages: ");
            if (submission.Downloads != null)
            {
                DumpDownload(submission.Downloads);
            }
            Console.WriteLine();
        }

        private static void DumpAudience(Audience audience)
        {
            Console.WriteLine("---- Audience: " + audience.Id);
            Console.WriteLine("         audienceName: " + audience.AudienceName);
            Console.WriteLine("         description:  " + audience.Description);
            Console.WriteLine("         name:        " + audience.Name);
            Console.WriteLine("         Links:");
            if (audience.Links != null)
            {
                foreach (Link link in audience.Links)
                {
                    DumpLink(link);
                }
            }
        }

        private static void DumpDownload(Download download)
        {
            foreach (Download.Item item in download.Items)
            {
                Console.WriteLine("               - url: " + item.Url);
                Console.WriteLine("               - type:" + item.Type);
            }
            Console.WriteLine("               - messages: ");

            if (download.Messages != null)
            {
                foreach (string msg in download.Messages)
                {
                    Console.WriteLine("                 " + msg);
                }
            }
        }

        private static void DumpLink(Link link)
        {
            Console.WriteLine("               - href:   " + link.Href);
            Console.WriteLine("               - method: " + link.Method);
            Console.WriteLine("               - rel:    " + link.Rel);
        }

        private static async Task<List<AuthorizationHandlerCredentials>> GetApiCreds(string CredentialsOption, string AADAuthenticationOption)
        {
            List<AuthorizationHandlerCredentials> myCreds = null;
            if (CredentialsOption == null)
            {
                CredentialsOption = "AADThenFile";
            }
            CredentialsOption = CredentialsOption.ToLowerInvariant();

            if ((CredentialsOption.CompareTo("aadonly") == 0) || (CredentialsOption.CompareTo("aadthenfile") == 0))
            {
                myCreds = await GetWebApiCreds(AADAuthenticationOption);
            }

            if (myCreds == null)
            {
                if ((CredentialsOption.CompareTo("fileonly") == 0) || (CredentialsOption.CompareTo("aadthenfile") == 0))
                {
                    try
                    {
                        string authconfig = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\authconfig.json");
                        myCreds = JsonConvert.DeserializeObject<List<AuthorizationHandlerCredentials>>(authconfig);
                        if (myCreds.Count == 0)
                        {
                            myCreds = null;
                        }
                        else
                        {
                            if (myCreds[0].ClientId.CompareTo("guid") == 0)
                            {
                                myCreds = null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        myCreds = null;
                    }
                }
            }

            return myCreds;
        }

        private static async Task<List<AuthorizationHandlerCredentials>> GetWebApiCreds(string AADAuthenticationOption)
        {
            List<AuthorizationHandlerCredentials> ReturnList = null;

            string url = ConfigurationManager.AppSettings["url"];

            if (url == null)
            {
                return null;
            }

            Uri WebAPIUri = new Uri(url);

            string clientID = ConfigurationManager.AppSettings["clientID"];
            Uri redirectUri = new Uri(ConfigurationManager.AppSettings["redirectUri"]);
            string resource = ConfigurationManager.AppSettings["resource"];
            string authority = ConfigurationManager.AppSettings["authority"];
            AuthenticationContext authContext = new AuthenticationContext(authority);

            if (AADAuthenticationOption == null)
            {
                AADAuthenticationOption = "Never";
            }
            AADAuthenticationOption.ToLowerInvariant();
            PlatformParameters platformParams = new PlatformParameters(PromptBehavior.Never);

            if (AADAuthenticationOption.CompareTo("prompt") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.Auto);
            }
            else if (AADAuthenticationOption.CompareTo("always") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.Always);
            }

            AuthenticationResult authResult = null;
            bool retryAuth = false;

            try
            {
                authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, platformParams);
            }
            catch (Microsoft.IdentityModel.Clients.ActiveDirectory.AdalException)
            {
                retryAuth = true;
                authResult = null;
            }

            if (retryAuth)
            {

                try
                {
                    authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, new PlatformParameters(PromptBehavior.Auto));
                }
                catch (Microsoft.IdentityModel.Clients.ActiveDirectory.AdalException)
                {
                    authResult = null;
                }
            }

            Uri restApi = new Uri(WebAPIUri, "/api/credentials");

            if (authResult != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authResult.AccessTokenType, authResult.AccessToken);

                    HttpResponseMessage infoResult = await client.GetAsync(restApi);

                    string content = await infoResult.Content.ReadAsStringAsync();

                    if (infoResult.IsSuccessStatusCode)
                    {
                        ReturnList = JsonConvert.DeserializeObject<List<AuthorizationHandlerCredentials>>(content);
                    }
                }
            }
            return ReturnList;
        }

    }
}


