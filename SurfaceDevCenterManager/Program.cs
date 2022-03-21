/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.
--*/
using Microsoft.Devices.HardwareDevCenterManager;
using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using Microsoft.Devices.HardwareDevCenterManager.Utility;
using Mono.Options;
using Newtonsoft.Json;
using SurfaceDevCenterManager.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager
{
    public enum DevCenterHWSubmissionType
    {
        ShippingLabel = 0,
        Product = 1,
        Submission = 2,
        PartnerSubmission = 3,
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
        private const uint DEFAULT_TIMEOUT = 5 * 60;
        private static Guid CorrelationId;
        private static DevCenterErrorDetails LastCommand;

        private static int Main(string[] args)
        {
            ErrorCodes result = ErrorCodes.UNSPECIFIED;

            CorrelationId = Guid.NewGuid();

            try
            {
                result = MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled Exception:");
                Console.WriteLine(e.ToString());
                Console.WriteLine("Last Command:");
                DevCenterErrorDetailsDump(LastCommand);
                result = ErrorCodes.UNHANDLED_EXCEPTION;
            }

            Console.WriteLine("Correlation Id: {0}", CorrelationId.ToString());
            Console.WriteLine("Return: {0} ({1})", (int)result, result.ToString());
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //Break debugger to look at command line output before the window disappears
                System.Diagnostics.Debugger.Break();
            }
            return (int)result;
        }

        public static void LastCommandSet(DevCenterErrorDetails error)
        {
            LastCommand = error;
        }

        /// <summary>
        /// Processes command line args and calls into HWDC
        /// </summary>
        /// <returns>Returns 0 success, non-zero on error</returns>
        private static async Task<ErrorCodes> MainAsync(string[] args)
        {
            ErrorCodes retval = ErrorCodes.SUCCESS;
            bool show_help = false;
            string CreateOption = null;
            bool CommitOption = false;
            string ListOption = null;
            string ProductId = null;
            string SubmissionId = null;
            string ShippingLabelId = null;
            string PublisherId = null;
            string DownloadOption = null;
            string MetadataOption = null;
            string SubmissionPackagePath = null;
            bool WaitOption = false;
            bool WaitForMetaData = false;
            bool CreateMetaData = false;
            bool AudienceOption = false;
            int OverrideServer = 0;
            bool OverrideServerPresent = false;
            string CredentialsOption = null;
            string AADAuthenticationOption = null;
            string TimeoutOption = null;
            uint HttpTimeout = DEFAULT_TIMEOUT;
            bool TranslateOption = false;
            string AnotherPartnerId = null;

            OptionSet p = new OptionSet() {
                { "c|create=",         "Path to json file with configuration to create", v => CreateOption = v },
                { "commit",            "Commit submission with given ID", v => CommitOption = true },
                { "l|list=",           "List a shippinglabel, product, submission or partnersubmission", v => ListOption = v },
                { "u|upload=",         "Upload a package to a specific product and submission", v => SubmissionPackagePath = v },
                { "productid=",        "Specify a specific ProductId", v => ProductId = v },
                { "submissionid=",     "Specify a specific SubmissionId", v => SubmissionId = v },
                { "shippinglabelid=",  "Specify a specific ShippingLabelId", v => ShippingLabelId = v },
                { "publisherid=",      "Specify a specific PublisherId", v => PublisherId = v },
                { "partnerid=",        "Specify PublisherId of the Partner to share the submission to via shipping label instead of Windows Update", v => AnotherPartnerId = v },
                { "v",                 "Increase debug message verbosity", v => { if (v != null) {++verbosity; }} },
                { "d|download=",       "Download a submission to current directory or folder specified", v => DownloadOption = v ?? Environment.CurrentDirectory },
                { "m|metadata=",       "Download a submission metadata to current directory or folder specified", v => MetadataOption = v ?? Environment.CurrentDirectory },
                { "h|help",            "Show this message and exit", v => show_help = v != null },
                { "w|wait",            "Wait for submission id to be done", v => WaitOption = true },
                { "waitmetadata",      "Wait for metadata to be done as well in a submission", v => WaitForMetaData = true },
                { "createmetadata",    "Requeset metadata creation for older submissions", v => CreateMetaData = true },
                { "a|audience",        "List Audiences", v => AudienceOption = true },
                { "server=",           "Specify target DevCenter server from CredSelect enum", v => { OverrideServer = int.Parse(v); OverrideServerPresent = true; }    },
                { "creds=",            "Option to specify app credentials.  Options: ENVOnly, FileOnly, AADOnly, AADThenFile (Default)", v => CredentialsOption = v },
                { "aad=",              "Option to specify AAD auth behavior.  Options: Never (Default), Prompt, Always, RefreshSession, SelectAccount", v => AADAuthenticationOption = v },
                { "t|timeout=",        $"Adjust the timeout for HTTP requests to specified seconds.  Default:{DEFAULT_TIMEOUT} seconds", v => TimeoutOption = v  },
                { "translate",         "Translate the given publisherid, productid and submissionid from a partner to the values visible in your HDC account", v => TranslateOption = true},
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
                return ErrorCodes.COMMAND_LINE_OPTION_PARSING_FAILED;
            }

            if (show_help)
            {
                ShowHelp(p);
                return ErrorCodes.SUCCESS;
            }

            List<AuthorizationHandlerCredentials> myCreds = await DevCenterCredentialsHandler.GetApiCreds(CredentialsOption, AADAuthenticationOption);

            if (myCreds == null)
            {
                ErrorParsingOptions("Unable to get Dev Center Credentials");
                return ErrorCodes.NO_DEV_CENTER_CREDENTIALS_FOUND;
            }

            if (OverrideServer < 0 || OverrideServer >= myCreds.Count)
            {
                ErrorParsingOptions("OverrideServer invalid - " + OverrideServer);
                return ErrorCodes.OVERRIDE_SERVER_INVALID;
            }
            else
            {
                if (!OverrideServerPresent)
                {
                    string loopServersString = ConfigurationManager.AppSettings["loopservers"];
                    if (loopServersString != null)
                    {
                        string[] serversList = loopServersString.Split(',');
                        int x = (new Random()).Next(0, serversList.Length);
                        OverrideServer = int.Parse(serversList[x]);
                    }
                }
            }

            if (CreateOption != null && (!File.Exists(CreateOption)))
            {
                ErrorParsingOptions("CreateOption invalid - " + CreateOption);
                return ErrorCodes.CREATE_INPUT_FILE_DOES_NOT_EXIST;
            }

            DevCenterHWSubmissionType ListOptionEnum = DevCenterHWSubmissionTypeCheck(ListOption);
            if (ListOption != null && ListOptionEnum == DevCenterHWSubmissionType.Invalid)
            {
                ErrorParsingOptions("ListOption invalid - " + ListOption);
                return ErrorCodes.LIST_INVALID_OPTION;
            }

            if (TimeoutOption != null)
            {
                if (uint.TryParse(TimeoutOption, out uint inputParse))
                {
                    HttpTimeout = inputParse;
                    Console.WriteLine($"> HttpTimeout: {HttpTimeout} seconds");
                }
                else
                {
                    Console.WriteLine($"> HttpTimeout: Invalid value {TimeoutOption}, using default timeout");
                }
            }

            DevCenterOptions options = new DevCenterOptions() { CorrelationId = CorrelationId, HttpTimeoutSeconds = HttpTimeout, RequestDelayMs = 250, LastCommand = LastCommandSet };
            DevCenterHandler api = new DevCenterHandler(myCreds[OverrideServer], options);

            if (CreateOption != null)
            {
                Console.WriteLine("> Create Option");

                CreateInput createInput = JsonConvert.DeserializeObject<CreateInput>(File.ReadAllText(CreateOption));

                if (DevCenterHWSubmissionType.Product == createInput.CreateType)
                {
                    try
                    {
                        DevCenterResponse<Product> ret = await api.NewProduct(createInput.CreateProduct);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(CreateOption) } { nameof(api.NewProduct) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.NEW_PRODUCT_API_FAILED;
                            }
                        }
                        else
                        {
                            ret.ReturnValue[0].Dump();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(CreateOption), nameof(api.NewProduct));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }

                }
                else if (DevCenterHWSubmissionType.Submission == createInput.CreateType)
                {
                    if (ProductId == null)
                    {
                        Console.WriteLine("> ERROR: productid not specified");
                        retval = ErrorCodes.NEW_SUBMISSION_PRODUCT_ID_MISSING;
                    }

                    if (retval == 0)
                    {
                        Console.WriteLine("> Creating Submission");

                        try
                        {
                            DevCenterResponse<Submission> ret = await api.NewSubmission(ProductId, createInput.CreateSubmission);
                            if (ret.Error != null)
                            {
                                if (ret.Error.HttpErrorCode == 429)
                                {
                                    Console.WriteLine($"{ nameof(CreateOption) } { nameof(api.NewSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                    return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                }
                                else
                                {
                                    DevCenterErrorDetailsDump(ret.Error);
                                    return ErrorCodes.NEW_SUBMISSION_API_FAILED;
                                }
                            }
                            else
                            {
                                ret.ReturnValue[0].Dump();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogExceptionToConsole(ex, nameof(CreateOption), nameof(api.NewSubmission));
                            return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                        }
                    }
                }
                else if (DevCenterHWSubmissionType.ShippingLabel == createInput.CreateType)
                {
                    if (ProductId == null)
                    {
                        Console.WriteLine("> ERROR: productid not specified");
                        retval = ErrorCodes.NEW_SHIPPING_LABEL_PRODUCT_ID_MISSING;
                    }

                    if (SubmissionId == null)
                    {
                        Console.WriteLine("> ERROR: submissionid not specified");
                        retval = ErrorCodes.NEW_SHIPPING_LABEL_SUBMISSION_ID_MISSING;
                    }

                    if (retval == 0)
                    {
                        Console.WriteLine("> Get Driver Metadata");
                        string tmpfile = System.IO.Path.GetTempFileName();

                        var section = string.Empty;
                        try
                        {
                            section = nameof(api.GetSubmission);
                            DevCenterResponse<Submission> retSubmission = await api.GetSubmission(ProductId, SubmissionId);
                            if (retSubmission.Error != null)
                            {
                                if (retSubmission.Error.HttpErrorCode == 429)
                                {
                                    Console.WriteLine($"{ nameof(CreateOption) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                    return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                }
                                else
                                {
                                    DevCenterErrorDetailsDump(retSubmission.Error);
                                    return ErrorCodes.NEW_SHIPPING_LABEL_GET_SUBMISSION_API_FAILED;
                                }
                            }

                            List<Submission> submissions = retSubmission.ReturnValue;
                            List<Download.Item> dls = submissions[0].Downloads.Items;
                            foreach (Download.Item dl in dls)
                            {
                                if (dl.Type.ToLower() == Download.Type.driverMetadata.ToString().ToLower())
                                {
                                    Console.WriteLine("> driverMetadata Url: " + dl.Url);
                                    BlobStorageHandler bsh = new BlobStorageHandler(dl.Url.AbsoluteUri);
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
                                                PnpString = pnpInfo.Key.ToLower()   // Recommendation from HDC team
                                            };
                                            labelHwids.Add(labelHwid);
                                        }
                                    }
                                }
                            }

                            createInput.CreateShippingLabel.Targeting.HardwareIds = labelHwids;
                            createInput.CreateShippingLabel.PublishingSpecifications.GoLiveDate = DateTime.Now.AddDays(7);

                            if (AnotherPartnerId != null)
                            {
                                Console.WriteLine("> Shipping to Partner (not Windows Update): " + AnotherPartnerId);
                                createInput.CreateShippingLabel.Destination = "anotherPartner";
                                createInput.CreateShippingLabel.RecipientSpecifications = new RecipientSpecifications()
                                {
                                    EnforceChidTargeting = false,
                                    ReceiverPublisherId = AnotherPartnerId
                                };
                            }

                            Console.WriteLine("> Creating Shipping Label");
                            section = nameof(api.NewShippingLabel);
                            DevCenterResponse<ShippingLabel> ret = await api.NewShippingLabel(ProductId, SubmissionId, createInput.CreateShippingLabel);
                            if (ret.Error != null)
                            {
                                if (ret.Error.HttpErrorCode == 429)
                                {
                                    Console.WriteLine($"{ nameof(CreateOption) } { nameof(api.NewShippingLabel) } experienced a HTTP 429 Too Many Requests response.");
                                    return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                }
                                else
                                {
                                    DevCenterErrorDetailsDump(ret.Error);
                                    return ErrorCodes.NEW_SHIPPING_LABEL_CREATE_API_FAILED;
                                }
                            }
                            else
                            {
                                ret.ReturnValue[0].Dump();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogExceptionToConsole(ex, nameof(CreateOption), section);
                            return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
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
                    retval = ErrorCodes.COMMIT_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.COMMIT_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Sending Commit");

                    try
                    {
                        DevCenterResponse<bool> ret = await api.CommitSubmission(ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(CommitOption) } { nameof(api.CommitSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            if (ret.Error.Code == ErrorCodeConstants.RequestInvalidForCurrentState && ret.Error.Message == ErrorMessageConstants.OnlyPendingSubmissionsCanBeCommitted)
                            {
                                Console.WriteLine($"{ nameof(CommitOption) } { nameof(api.CommitSubmission) } request invalid for currentState, { ErrorMessageConstants.OnlyPendingSubmissionsCanBeCommitted }");
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.COMMIT_REQUEST_INVALID_FOR_CURRENT_STATE;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.COMMIT_API_FAILED;
                            }
                        }
                        else
                        {
                            if (!ret.ReturnValue[0])
                            {
                                Console.WriteLine("> Commit Failed");
                                retval = ErrorCodes.COMMIT_API_FAILED;
                            }
                            else
                            {
                                Console.WriteLine("> Commit OK");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(CommitOption), nameof(api.CommitSubmission));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            else if (ListOption != null)
            {
                Console.WriteLine("> List Option {0}", ListOption);

                var section = string.Empty;
                try
                {
                    switch (ListOptionEnum)
                    {
                        case DevCenterHWSubmissionType.Product:
                            {
                                section = nameof(api.GetProducts);
                                DevCenterResponse<Product> ret = await api.GetProducts(ProductId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(ListOption) } { nameof(api.GetProducts) } experienced a HTTP 429 Too Many Requests response.");
                                        return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                    }
                                    else
                                    {
                                        DevCenterErrorDetailsDump(ret.Error);
                                        return ErrorCodes.LIST_GET_PRODUCTS_API_FAILED;
                                    }
                                }
                                else
                                {
                                    List<Product> products = ret.ReturnValue;
                                    foreach (Product product in products)
                                    {
                                        product.Dump();
                                    }
                                }
                            }
                            break;
                        case DevCenterHWSubmissionType.Submission:
                            {
                                section = nameof(api.GetSubmission);
                                DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(ListOption) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                        return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                    }
                                    if (ret.Error.Code == ErrorCodeConstants.EntityNotFound)
                                    {
                                        Console.WriteLine($"{ nameof(ListOption) } { nameof(api.GetSubmission) } entity not found, try translate option.");
                                        DevCenterErrorDetailsDump(ret.Error);
                                        return ErrorCodes.SUBMISSION_ENTITY_NOT_FOUND;
                                    }
                                    else
                                    {
                                        DevCenterErrorDetailsDump(ret.Error);
                                        return ErrorCodes.LIST_GET_SUBMISSION_API_FAILED;
                                    }
                                }
                                else
                                {
                                    List<Submission> submissions = ret.ReturnValue;
                                    foreach (Submission submission in submissions)
                                    {
                                        submission.Dump();
                                    }
                                }
                            }
                            break;
                        case DevCenterHWSubmissionType.ShippingLabel:
                            {
                                section = nameof(api.GetShippingLabels);
                                DevCenterResponse<ShippingLabel> ret = await api.GetShippingLabels(ProductId, SubmissionId, ShippingLabelId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(ListOption) } { nameof(api.GetShippingLabels) } experienced a HTTP 429 Too Many Requests response.");
                                        return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                    }
                                    else
                                    {
                                        DevCenterErrorDetailsDump(ret.Error);
                                        return ErrorCodes.LIST_GET_SHIPPING_LABEL_API_FAILED;
                                    }
                                }
                                else
                                {
                                    List<ShippingLabel> shippingLabels = ret.ReturnValue;
                                    foreach (ShippingLabel shippingLabel in shippingLabels)
                                    {
                                        shippingLabel.Dump();
                                    }
                                }
                            }
                            break;
                        case DevCenterHWSubmissionType.PartnerSubmission:
                            {
                                section = nameof(api.GetPartnerSubmission);
                                DevCenterResponse<Submission> ret = await api.GetPartnerSubmission(PublisherId, ProductId, SubmissionId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(ListOption) } { nameof(api.GetPartnerSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                        return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                                    }
                                    else
                                    {
                                        DevCenterErrorDetailsDump(ret.Error);
                                        return ErrorCodes.LIST_GET_PARTNER_SUBMISSION_API_FAILED;
                                    }
                                }
                                else
                                {
                                    List<Submission> submissions = ret.ReturnValue;
                                    foreach (Submission submission in submissions)
                                    {
                                        submission.Dump();
                                    }
                                }
                            }
                            break;
                        default:
                            Console.WriteLine(">  Invalid List Option selected");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionToConsole(ex, nameof(ListOption), section);
                    return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                }
            }
            else if (DownloadOption != null)
            {
                Console.WriteLine("> Download Option {0}", DownloadOption);

                string pathNameFull = System.IO.Path.GetFullPath(DownloadOption);
                string FileNamePart = System.IO.Path.GetFileName(DownloadOption);
                string PathNamePart = System.IO.Path.GetDirectoryName(DownloadOption);

                if (!System.IO.Directory.Exists(PathNamePart))
                {
                    Console.WriteLine("> ERROR: Output path does not exist: " + PathNamePart);
                    retval = ErrorCodes.DOWNLOAD_OUTPUT_PATH_NOT_EXIST;
                }

                if (System.IO.File.Exists(DownloadOption))
                {
                    Console.WriteLine("> ERROR: Output file exists already: " + DownloadOption);
                    retval = ErrorCodes.DOWNLOAD_OUTPUT_FILE_ALREADY_EXISTS;
                }

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.DOWNLOAD_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.DOWNLOAD_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    try
                    {
                        DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(DownloadOption) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.DOWNLOAD_GET_SUBMISSION_API_FAILED;
                            }
                        }
                        List<Submission> submissions = ret.ReturnValue;
                        List<Download.Item> dls = submissions[0].Downloads.Items;
                        foreach (Download.Item dl in dls)
                        {
                            if (dl.Type.ToLower() == Download.Type.signedPackage.ToString().ToLower())
                            {
                                Console.WriteLine("> signedPackage Url: " + dl.Url);
                                BlobStorageHandler bsh = new BlobStorageHandler(dl.Url.AbsoluteUri);
                                await bsh.Download(DownloadOption);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(DownloadOption), nameof(api.GetSubmission));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            else if (MetadataOption != null)
            {
                Console.WriteLine("> Metadata Download Option {0}", MetadataOption);

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.METADATA_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.METADATA_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    try
                    {
                        DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(MetadataOption) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.METADATA_GET_SUBMISSION_API_FAILED;
                            }
                        }
                        List<Submission> submissions = ret.ReturnValue;
                        List<Download.Item> dls = submissions[0].Downloads.Items;
                        bool foundMetaData = false;
                        foreach (Download.Item dl in dls)
                        {
                            if (dl.Type.ToLower() == Download.Type.driverMetadata.ToString().ToLower())
                            {
                                Console.WriteLine("> driverMetadata Url: " + dl.Url);
                                BlobStorageHandler bsh = new BlobStorageHandler(dl.Url.AbsoluteUri);
                                await bsh.Download(MetadataOption);
                                foundMetaData = true;
                            }
                        }

                        if (!foundMetaData)
                        {
                            Console.WriteLine("> ERROR: No Metadata available for this submission");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(MetadataOption), nameof(api.GetSubmission));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            else if (SubmissionPackagePath != null)
            {
                Console.WriteLine("> Upload Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.UPLOAD_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.UPLOAD_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Fetch Submission Info");
                    try
                    {
                        DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(SubmissionPackagePath) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.UPLOAD_GET_SUBMISSION_API_FAILED;
                            }
                        }
                        List<Submission> submissions = ret.ReturnValue;
                        List<Download.Item> dls = submissions[0].Downloads.Items;
                        foreach (Download.Item dl in dls)
                        {
                            if (dl.Type.ToLower() == Download.Type.initialPackage.ToString().ToLower())
                            {
                                Console.WriteLine("> initialPackage Url: " + dl.Url);
                                Console.WriteLine("> Uploading Submission Package");
                                BlobStorageHandler bsh = new BlobStorageHandler(dl.Url.AbsoluteUri);
                                await bsh.Upload(SubmissionPackagePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(SubmissionPackagePath), nameof(api.GetSubmission));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            else if (WaitOption)
            {
                Console.WriteLine("> Wait Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.WAIT_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.WAIT_SUBMISSION_ID_MISSING;
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
                            try
                            {
                                DevCenterResponse<Submission> ret = await api.GetSubmission(ProductId, SubmissionId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(WaitOption) } { nameof(api.GetSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                        await Task.Delay(5000);
                                        continue;
                                    }
                                    else
                                    {
                                        DevCenterErrorDetailsDump(ret.Error);
                                        done = true;
                                        retval = ErrorCodes.WAIT_GET_SUBMISSION_API_FAILED;
                                        break;
                                    }
                                }
                                List<Submission> submissions = ret.ReturnValue;
                                Submission sub = submissions[0];

                                if (!done)
                                {
                                    if (sub.WorkflowStatus == null)
                                    {
                                        Console.WriteLine($"{ nameof(WaitOption) } { sub.Name } { nameof(WorkflowStatus) } was NULL. Will continue to wait...");
                                        continue;
                                    }

                                    if (sub.WorkflowStatus.CurrentStep != lastCurrentStep ||
                                    sub.WorkflowStatus.State != lastState)
                                    {
                                        lastCurrentStep = sub.WorkflowStatus.CurrentStep;
                                        lastState = sub.WorkflowStatus.State;
                                        await sub.WorkflowStatus.Dump();
                                    }

                                    bool haveMetadata = false;
                                    bool haveSignedPackage = false;
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
                                            if (dl.Type.ToLower() == Download.Type.signedPackage.ToString().ToLower())
                                            {
                                                Console.WriteLine("> signedPackage Url: " + dl.Url);
                                                haveSignedPackage = true;
                                            }
                                        }
                                    }

                                    if (lastState == "failed")
                                    {
                                        done = true;
                                        retval = ErrorCodes.WAIT_SUBMISSION_FAILED_IN_HWDC;
                                    }
                                    else if (haveSignedPackage)
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
                            catch (Exception ex)
                            {
                                LogExceptionToConsole(ex, nameof(WaitOption), nameof(api.GetSubmission));
                                return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                            }
                        }
                        else
                        {
                            try
                            {
                                DevCenterResponse<ShippingLabel> ret = await api.GetShippingLabels(ProductId, SubmissionId, ShippingLabelId);
                                if (ret.Error != null)
                                {
                                    if (ret.Error.HttpErrorCode == 429)
                                    {
                                        Console.WriteLine($"{ nameof(WaitOption) } { nameof(api.GetShippingLabels) } experienced a HTTP 429 Too Many Requests response.");
                                        await Task.Delay(5000);
                                        continue;
                                    }
                                    else
                                    {

                                        DevCenterErrorDetailsDump(ret.Error);
                                        done = true;
                                        retval = ErrorCodes.WAIT_GET_SHIPPING_LABEL_API_FAILED;
                                        break;
                                    }
                                }
                                List<ShippingLabel> shippingLabels = ret.ReturnValue;
                                ShippingLabel label = shippingLabels[0];

                                if (label.WorkflowStatus.CurrentStep != lastCurrentStep ||
                                    label.WorkflowStatus.State != lastState)
                                {
                                    lastCurrentStep = label.WorkflowStatus.CurrentStep;
                                    lastState = label.WorkflowStatus.State;
                                    await label.WorkflowStatus.Dump();
                                }

                                if (lastState == "failed")
                                {
                                    done = true;
                                    retval = ErrorCodes.WAIT_SHIPPING_LABEL_FAILED_IN_HWDC;
                                }
                                else if (lastCurrentStep == "microsoftApproval")
                                {
                                    done = true;
                                    Console.WriteLine("> Shipping Label Ready");
                                }
                                else if (lastCurrentStep == "finalizeSharing" && lastState == "completed")
                                {
                                    done = true;
                                    Console.WriteLine("> Shipping Label for Sharing Ready");
                                }
                                else
                                {
                                    await Task.Delay(5000);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogExceptionToConsole(ex, nameof(WaitOption), nameof(api.GetShippingLabels));
                                return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                            }
                        }
                    }
                    Console.WriteLine("> Done");
                }
            }
            else if (AudienceOption)
            {
                Console.WriteLine("> Audience Option");

                try
                {
                    DevCenterResponse<Audience> ret = await api.GetAudiences();
                    if (ret.Error != null)
                    {
                        if (ret.Error.HttpErrorCode == 429)
                        {
                            Console.WriteLine($"{ nameof(AudienceOption) } { nameof(api.GetAudiences) } experienced a HTTP 429 Too Many Requests response.");
                            return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                        }
                        else
                        {
                            DevCenterErrorDetailsDump(ret.Error);
                            return ErrorCodes.AUIDENCE_GET_AUDIENCE_API_FAILED;
                        }
                    }
                    else
                    {
                        List<Audience> audiences = ret.ReturnValue;
                        foreach (Audience audience in audiences)
                        {
                            audience.Dump();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionToConsole(ex, nameof(AudienceOption), nameof(api.GetAudiences));
                    return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                }
            }
            else if (CreateMetaData)
            {
                Console.WriteLine("> Create MetaData Option");

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.CREATEMETADATA_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.CREATEMETADATA_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Sending Create MetaData");
                    try
                    {
                        DevCenterResponse<bool> ret = await api.CreateMetaData(ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(CreateMetaData) } { nameof(api.CreateMetaData) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.CREATEMETADATA_API_FAILED;
                            }
                        }
                        else
                        {
                            if (!ret.ReturnValue[0])
                            {
                                Console.WriteLine("> Create MetaData Failed");
                                retval = ErrorCodes.CREATEMETADATA_API_FAILED;
                            }
                            else
                            {
                                Console.WriteLine("> Create MetaData OK");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(CreateMetaData), nameof(api.CreateMetaData));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            else if (TranslateOption)
            {
                Console.WriteLine("> Translate Option");

                if (PublisherId == null)
                {
                    Console.WriteLine("> ERROR: publisherid not specified");
                    retval = ErrorCodes.TRANSLATE_PUBLISHER_ID_MISSING;
                }

                if (ProductId == null)
                {
                    Console.WriteLine("> ERROR: productid not specified");
                    retval = ErrorCodes.TRANSLATE_PRODUCT_ID_MISSING;
                }

                if (SubmissionId == null)
                {
                    Console.WriteLine("> ERROR: submissionid not specified");
                    retval = ErrorCodes.TRANSLATE_SUBMISSION_ID_MISSING;
                }

                if (retval == 0)
                {
                    Console.WriteLine("> Requesting Translation");
                    try
                    {
                        DevCenterResponse<Submission> ret = await api.GetPartnerSubmission(PublisherId, ProductId, SubmissionId);
                        if (ret.Error != null)
                        {
                            if (ret.Error.HttpErrorCode == 429)
                            {
                                Console.WriteLine($"{ nameof(TranslateOption) } { nameof(api.GetPartnerSubmission) } experienced a HTTP 429 Too Many Requests response.");
                                return ErrorCodes.HTTP_429_RATE_LIMIT_EXCEEDED;
                            }
                            else
                            {
                                DevCenterErrorDetailsDump(ret.Error);
                                return ErrorCodes.TRANSLATE_API_FAILED;
                            }
                        }
                        else
                        {
                            if (ret.ReturnValue.Count == 0)
                            {
                                Console.WriteLine("> Translate Failed");
                                retval = ErrorCodes.TRANSLATE_API_FAILED;
                            }
                            else
                            {
                                Console.WriteLine("> Translate OK");
                                ret.ReturnValue[0].Dump();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogExceptionToConsole(ex, nameof(TranslateOption), nameof(api.GetPartnerSubmission));
                        return ErrorCodes.PARTNER_CENTER_HTTP_EXCEPTION;
                    }
                }
            }
            return retval;
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: sdcm [OPTIONS]+");
            Console.WriteLine("Surface Dev Center Manager - Manage Microsoft Hardware Dev Center content");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
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

        private static void DevCenterErrorDetailsDump(DevCenterErrorDetails error)
        {
            Console.WriteLine("ERROR (DevCenterErrorDetails)");
            if (error == null) return;
            Console.WriteLine("Code:    " + (error.Code ?? ""));
            Console.WriteLine("HttpCode:" + error.HttpErrorCode);
            Console.WriteLine("Message: " + (error.Message ?? ""));
            if (error.ValidationErrors != null)
            {
                Console.WriteLine("ValidationErrors:");
                foreach (DevCenterErrorValidationErrorEntry entry in error.ValidationErrors)
                {
                    Console.WriteLine("  Target: " + entry.Target);
                    Console.WriteLine("  Message:" + entry.Message);
                }
            }

            Console.WriteLine("Correlation Id: {0}", CorrelationId.ToString());
            if (error.Trace != null)
            {
                Console.WriteLine("Request Id:     {0}", error.Trace.RequestId);
                Console.WriteLine("Method:         {0}", error.Trace.Method);
                Console.WriteLine("Url:            {0}", error.Trace.Url);
                Console.WriteLine("Content:        {0}", error.Trace.Content);
            }
        }

        /// <summary>
        /// Log messages to the console for debugging
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="option"></param>
        /// <param name="section"></param>
        private static void LogExceptionToConsole(Exception ex, string option, string section)
        {
            if (ex == null) return;
            Console.WriteLine("");
            Console.WriteLine("============================================================");
            Console.WriteLine("\tSurfaceDevCenterManager Exception Log");
            Console.WriteLine($"Option:          { option ?? "" }");
            Console.WriteLine($"Section:         { section ?? "" }");
            Console.WriteLine($"Type:            { ex.GetType() ?? null }");
            Console.WriteLine($"Message:         { ex.Message ?? "" }");
            Console.WriteLine($"Inner Exception: { ex.InnerException?.Message ?? "" }");
            Console.WriteLine($"Correlation Id:  { CorrelationId }");
            Console.WriteLine("============================================================");
            Console.WriteLine("");
        }
    }
}
