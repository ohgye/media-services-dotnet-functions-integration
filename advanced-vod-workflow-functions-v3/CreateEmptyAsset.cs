//
// Azure Media Services REST API v3 Functions
//
// CreateEmptyAsset - This function creates an empty asset.
//
//  Input:
//      {
//          "assetNamePrefix": "testasset",         // Name of the asset
//          "assetCreationOption": "None",          // (Optional) Name of asset creation option
//          "assetStorageAccount":  "storage01"     // (Optional) Name of attached storage account where to create the asset
//      }
//      // https://docs.microsoft.com/en-us/rest/api/media/operations/asset#asset_entity_properties
//      //      None                            Normal asset type (no encryption)
//      //      StorageEncrypted                Storage Encryption encrypted asset type
//      //      CommonEncryptionProtected       Common Encryption encrypted asset type
//      //      EnvelopeEncryptionProtected     Envelope Encryption encrypted asset type
//  Output:
//      {
//          "assetName":                            // Name of the asset created
//              "testasset-4ba0b3ed-29e2-4e7c-aa5a-6e027eccac20",
//          "assetId":                              // Id of the asset created
//              "nb:cid:UUID:68adb036-43b7-45e6-81bd-8cf32013c810"
//          "destinationContainer":                 // Name of the destination container name for the asset created
//              "asset-68adb036-43b7-45e6-81bd-8cf32013c810"
//      }
//

using System;
using System.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

using Newtonsoft.Json;

using advanced_vod_functions_v3.SharedLibs;


namespace advanced_vod_functions_v3
{
    public static class CreateEmptyAsset
    {
        [FunctionName("CreateEmptyAsset")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - CreateEmptyAsset was triggered!");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data.assetNamePrefix == null)
                return new BadRequestObjectResult("Please pass assetNamePrefix in the input object" );
            string assetStorageAccount = null;
            if (data.assetStorageAccount != null)
                assetStorageAccount = data.assetStorageAccount;
            Guid assetGuid = Guid.NewGuid();
            string assetName = data.assetNamePrefix + "-" + assetGuid.ToString();

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset asset = null;

            try
            {
                IAzureMediaServicesClient client = MediaServicesHelper.CreateMediaServicesClientAsync(amsconfig);

                Asset assetParams = new Asset(null, assetName, null, assetGuid, DateTime.Now, DateTime.Now, null, assetName, null, assetStorageAccount, AssetStorageEncryptionFormat.None);
                asset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, assetParams);
                //asset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, new Asset());
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

            // compatible with AMS V2 API
            string assetId = "nb:cid:UUID:" + asset.AssetId;
            string destinationContainer = "asset-" + asset.AssetId;

            return (ActionResult)new OkObjectResult(new
            {
                assetName = assetName,
                assetId = assetId,
                destinationContainer = destinationContainer
            });
        }
    }
}
