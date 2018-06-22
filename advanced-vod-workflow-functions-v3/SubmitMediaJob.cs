//
// Azure Media Services REST API v2 Functions
//
// SubmitMediaJob - This function submits media job.
//
//  Input:
//      {
//          "inputAssetName":  "Name of the asset for input",
//          "transformName":  "Name of the Transform",
//          "outputAssetNamePrefix":  "Name of the asset for output"
//          "assetStorageAccount":  "Name of attached storage where to create the asset"  // (optional)  
//      }
//  Output:
//      {
//          "jobName":  "Name of media Job"
//          "encoderOutputAssetName": "string",
//          "videoAnalyzerOutputAssetName": "string"
//      }
//
//

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

using advanced_vod_functions_v3.SharedLibs;


namespace advanced_vod_functions_v3
{
    public static class SubmitMediaJob
    {
        [FunctionName("SubmitMediaJob")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - CreateEmptyAsset was triggered!");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Validate input objects
            if (data.inputAssetName == null)
                return new BadRequestObjectResult("Please pass inputAssetName in the input object");
            if (data.transformName == null)
                return new BadRequestObjectResult("Please pass transformName in the input object");
            if (data.outputAssetNamePrefix == null)
                return new BadRequestObjectResult("Please pass outputAssetNamePrefix in the input object");
            string inputAssetName = data.inputAssetName;
            string transformName = data.transformName;
            string outputAssetNamePrefix = data.outputAssetNamePrefix;
            string assetStorageAccount = null;
            if (data.assetStorageAccount != null)
                assetStorageAccount = data.assetStorageAccount;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset inputAsset = null;

            string guid = Guid.NewGuid().ToString();
            string jobName = "amsv3function-job-" + guid;
            string encoderOutputAssetName = null;
            string videoAnalyzerOutputAssetName = null;

            try
            {
                IAzureMediaServicesClient client = MediaServicesHelper.CreateMediaServicesClientAsync(amsconfig);

                inputAsset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, inputAssetName);
                if (inputAsset == null)
                    return new BadRequestObjectResult("Asset for input not found");
                Transform transform = client.Transforms.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName);
                if (transform == null)
                    return new BadRequestObjectResult("Transform not found");

                var jobOutputList = new List<JobOutput>();
                for (int i = 0; i < transform.Outputs.Count; i++)
                {
                    Guid assetGuid = Guid.NewGuid();
                    string outputAssetName = outputAssetNamePrefix + "-" + assetGuid.ToString();
                    Preset p = transform.Outputs[i].Preset;
                    if (p is BuiltInStandardEncoderPreset || p is StandardEncoderPreset)
                        encoderOutputAssetName = outputAssetName;
                    else if (p is VideoAnalyzerPreset)
                        videoAnalyzerOutputAssetName = outputAssetName;
                    Asset assetParams = new Asset(null, outputAssetName, null, assetGuid, DateTime.Now, DateTime.Now, null, outputAssetName, null, assetStorageAccount, AssetStorageEncryptionFormat.None);
                    Asset outputAsset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, outputAssetName, assetParams);
                    jobOutputList.Add(new JobOutputAsset(outputAssetName));
                }

                // Use the name of the created input asset to create the job input.
                JobInput jobInput = new JobInputAsset(assetName: inputAssetName);
                JobOutput[] jobOutputs = jobOutputList.ToArray();
                Job job = client.Jobs.Create(
                    amsconfig.ResourceGroup,
                    amsconfig.AccountName,
                    transformName,
                    jobName,
                    new Job
                    {
                        Input = jobInput,
                        Outputs = jobOutputs,
                    }
                );
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
                jobName = jobName,
                encoderOutputAssetName = encoderOutputAssetName,
                videoAnalyzerOutputAssetName = videoAnalyzerOutputAssetName
            });
        }
    }
}
