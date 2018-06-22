//
// Azure Media Services REST API v3 Functions
//
// CreateEmptyAsset - This function creates an empty asset.
//
//  Input:
//      {
//          "transformName": "TestTransform"        // "Name of the Transform",
//          "builtInStandardEncoderPreset":
//          {
//              "presetName": "string"  // string (default: AdaptiveStreaming)
//          }
//          "videoAnalyzerPreset":
//          {
//              "audioInsightsOnly": true|false,    // boolean: Whether to only extract audio insights when processing a video file
//              "audioLanguage": "en-US"           // string: The language for the audio payload in the input using the BCP-47 format of 'language tag-region' (e.g: 'en-US').
//
//              // The list of supported languages are:
//              // 'en-US', 'en-GB', 'es-ES', 'es-MX', 'fr-FR', 'it-IT', 'ja-JP', 'pt-BR', 'zh-CN'.
//          }
//      }
//  Output:
//      {
//          "transformId":  "Id of the Transform"
//      }
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
    public static class CreateTransform
    {
        [FunctionName("CreateTransform")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - CreateEmptyAsset was triggered!");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data.transformName == null)
                return new BadRequestObjectResult("Please pass transformName in the input object");
            string transformName = data.transformName;
            if (data.transformOutputs == null)
                return new BadRequestObjectResult("Please pass transformOutputs in the input object");
            JArray transformOutputJArray = data.transformOutputs;

            //JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            //{
            //    Converters = { new IsoDateTimeConverter { DateTimeFormat = "PdTHHHmmMssS" } }
            //};

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            string transformId = null;

            try
            {
                IAzureMediaServicesClient client = MediaServicesHelper.CreateMediaServicesClientAsync(amsconfig);

                // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
                // also uses the same recipe or Preset for processing content.
                Transform transform = client.Transforms.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName);
                if (transform == null)
                {
                    // You need to specify what you want it to produce as an output
                    //var transformOutputList = new List<TransformOutput>();
                    List<TransformOutput> transformOutputList = new List<TransformOutput>();
                    if (data.transformOutputs != null)
                    {
                        foreach (var t in transformOutputJArray)
                        {
                            JsonConverter[] jsonConverters = { new PresetConverter<Preset>() };
                            TransformOutput transformOutput = JsonConvert.DeserializeObject<TransformOutput>(t.ToString(), jsonConverters);
                            transformOutputList.Add(transformOutput);
                        }
                    }
                    // You need to specify what you want it to produce as an output
                    TransformOutput[] output = transformOutputList.ToArray();
                    // Create the Transform with the output defined above
                    transform = client.Transforms.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, transformName, output);
                }
                transformId = transform.Id;
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
                transformId = transformId
            });
        }
    }
}
