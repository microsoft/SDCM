/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager
{
    internal enum ErrorCodes
    {
        SUCCESS = 0,
        UNSPECIFIED = -1,
        UNHANDLED_EXCEPTION = -2,
        COMMAND_LINE_OPTION_PARSING_FAILED = -3,
        NO_DEV_CENTER_CREDENTIALS_FOUND = -4,
        OVERRIDE_SERVER_INVALID = -5,
        CREATE_INPUT_FILE_DOES_NOT_EXIST = -6,
        NEW_PRODUCT_API_FAILED = -7,
        NEW_SUBMISSION_PRODUCT_ID_MISSING = -8,
        NEW_SUBMISSION_API_FAILED = -9,
        NEW_SHIPPING_LABEL_PRODUCT_ID_MISSING = -10,
        NEW_SHIPPING_LABEL_SUBMISSION_ID_MISSING = -11,
        NEW_SHIPPING_LABEL_CREATE_API_FAILED = -12,
        NEW_SHIPPING_LABEL_GET_SUBMISSION_API_FAILED = -13,
        COMMIT_PRODUCT_ID_MISSING = -14,
        COMMIT_SUBMISSION_ID_MISSING = -15,
        COMMIT_API_FAILED = -16,
        LIST_GET_PRODUCTS_API_FAILED = -17,
        LIST_GET_SUBMISSION_API_FAILED = -18,
        LIST_GET_SHIPPING_LABEL_API_FAILED = -19,
        DOWNLOAD_OUTPUT_PATH_NOT_EXIST = -20,
        DOWNLOAD_OUTPUT_FILE_ALREADY_EXISTS = -21,
        DOWNLOAD_PRODUCT_ID_MISSING = -22,
        DOWNLOAD_SUBMISSION_ID_MISSING = -23,
        DOWNLOAD_GET_SUBMISSION_API_FAILED = -24,
        METADATA_SUBMISSION_ID_MISSING = -25,
        METADATA_PRODUCT_ID_MISSING = -26,
        METADATA_GET_SUBMISSION_API_FAILED = -27,
        UPLOAD_PRODUCT_ID_MISSING = -28,
        UPLOAD_GET_SUBMISSION_API_FAILED = -29,
        UPLOAD_SUBMISSION_ID_MISSING = -30,
        WAIT_PRODUCT_ID_MISSING = -31,
        WAIT_SUBMISSION_ID_MISSING = -32,
        WAIT_GET_SUBMISSION_API_FAILED = -33,
        WAIT_SUBMISSION_FAILED_IN_HWDC = -34,
        WAIT_GET_SHIPPING_LABEL_API_FAILED = -35,
        WAIT_SHIPPING_LABEL_FAILED_IN_HWDC = -36,
        AUIDENCE_GET_AUDIENCE_API_FAILED = -37,
        LIST_GET_PARTNER_SUBMISSION_API_FAILED = -38,
        LIST_INVALID_OPTION = -39,
        CREATEMETADATA_PRODUCT_ID_MISSING = -40,
        CREATEMETADATA_SUBMISSION_ID_MISSING = -41,
        CREATEMETADATA_API_FAILED = -42,
        TRANSLATE_PRODUCT_ID_MISSING = -43,
        TRANSLATE_SUBMISSION_ID_MISSING = -44,
        TRANSLATE_PUBLISHER_ID_MISSING = -45,
        TRANSLATE_API_FAILED = -46,
        SUBMISSION_ENTITY_NOT_FOUND = -47,
        COMMIT_REQUEST_INVALID_FOR_CURRENT_STATE = -48,
        HTTP_429_RATE_LIMIT_EXCEEDED = -429,
        PARTNER_CENTER_HTTP_EXCEPTION = -1000
    }

    // https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/get-product-data#error-codes
    internal static class ErrorCodeConstants
    {
        public const string EntityNotFound = "entityNotFound";
        public const string RequestInvalidForCurrentState = "requestInvalidForCurrentState";
    }

    //
    internal static class ErrorMessageConstants
    {
        public const string OnlyPendingSubmissionsCanBeCommitted = "Only pending submissions can be committed.";
        public const string InitialSubmissionAlreadyExists = "Initial submission already exists";
    }
}
