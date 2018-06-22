//
// Azure Media Services REST API v3 - Functions
//
// StartBlobContainerCopyToAsset - This function starts copying blob container to the asset.
//
//  Input:
//      {
//          "assetName":                                // Name of the asset for copy destination
//              "testasset-4ba0b3ed-29e2-4e7c-aa5a-6e027eccac20",
//          "sourceStorageAccountName": "mediaimports", // Name of the storage account for copy source
//          "sourceStorageAccountKey": "xxxkey==",      // Key of the storage account for copy source
//          "sourceContainer": "movie-trailer",         // Blob container name of the storage account for copy source
//          "fileNames":                                // (Optional) File names of copy target contents
//              [ "filename.mp4" , "filename2.mp4" ]
//      }
//  Output:
//      {
//          "destinationContainer":                     // Container Name of the asset for copy destination
//              "asset-2e26fd08-1436-44b1-8b92-882a757071dd"
//      }
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using advanced_vod_functions_v3.SharedLibs;


namespace advanced_vod_functions
{
    public static class StartBlobContainerCopyToAsset
    {
        [FunctionName("StartBlobContainerCopyToAsset")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - StartBlobContainerCopyToAsset was triggered!");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Validate input objects
            if (data.assetName == null)
                return new BadRequestObjectResult("Please pass assetName in the input object");
            if (data.sourceStorageAccountName == null)
                return new BadRequestObjectResult("Please pass sourceStorageAccountName in the input object");
            if (data.sourceStorageAccountKey == null)
                return new BadRequestObjectResult("Please pass sourceStorageAccountKey in the input object");
            if (data.sourceContainer == null)
                return new BadRequestObjectResult("Please pass sourceContainer in the input object");
            string assetName = data.assetName;
            string sourceStorageAccountName = data.sourceStorageAccountName;
            string sourceStorageAccountKey = data.sourceStorageAccountKey;
            string sourceContainerName = data.sourceContainer;
            List<string> fileNames = null;
            if (data.fileNames != null)
            {
                fileNames = ((JArray)data.fileNames).ToObject<List<string>>();
            }

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset asset = null;

            try
            {
                IAzureMediaServicesClient client = MediaServicesHelper.CreateMediaServicesClientAsync(amsconfig);

                // Get the Asset
                asset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, assetName);
                if (asset == null)
                {
                    return new BadRequestObjectResult("Asset not found");
                }

                // Setup blob container
                CloudBlobContainer sourceBlobContainer = BlobStorageHelper.GetCloudBlobContainer(sourceStorageAccountName, sourceStorageAccountKey, sourceContainerName);
                var response = client.Assets.ListContainerSas(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, permissions: AssetContainerPermission.ReadWrite, expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());
                var sasUri = new Uri(response.AssetContainerSasUrls.First());
                CloudBlobContainer destinationBlobContainer = new CloudBlobContainer(sasUri);

                // Copy Source Blob container into Destination Blob container that is associated with the asset.
                BlobStorageHelper.CopyBlobsAsync(sourceBlobContainer, destinationBlobContainer, fileNames);
            }
            catch (ApiErrorException e)
            {
                log.Info($"ERROR: AMS API call failed with error code: {e.Body.Error.Code} and message: {e.Body.Error.Message}");
                return new BadRequestObjectResult("AMS API call error: " + e.Message);
            }
            catch (Exception e)
            {
                log.Info($"ERROR: Exception with message: {e.Message}");
                return new BadRequestObjectResult("Error: " + e.Message);
            }

            return (ActionResult)new OkObjectResult(new
            {
                destinationContainer = $"asset-{data.assetId}"
            });
        }
    }
}