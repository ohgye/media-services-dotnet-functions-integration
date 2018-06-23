//
// Azure Media Services REST API v3 Functions
//
// MonitorMediaJob - This function monitors media job.
//
/*
```c#
Input:
    {
        // Name of the media job
        "jobName": "amsv3function-job-24369d2e-7415-4ff5-ba12-b8a879a15401",
        // Name of the Transform for the media job
        "transformName": "TestTransform"
    }
Output:
    {
        // Status code of the media job
        "jobState": 0
    }

```
*/
//      // https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.mediaservices.client.jobstate?view=azure-dotnet
//      //      Queued      0
//      //      Scheduled   1
//      //      Processing  2
//      //      Finished    3
//      //      Error       4
//      //      Canceled    5
//      //      Canceling   6
//
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
    public static class MonitorMediaJob
    {
        [FunctionName("MonitorMediaJob")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - MonitorMediaJob was triggered!");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Validate input objects
            if (data.jobName == null)
                return new BadRequestObjectResult("Please pass jobName in the input object");
            if (data.transformName == null)
                return new BadRequestObjectResult("Please pass transformName in the input object");
            string jobName = data.jobName;
            string transformName = data.transformName;


            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Job job = null;

            try
            {
                IAzureMediaServicesClient client = MediaServicesHelper.CreateMediaServicesClientAsync(amsconfig);
                job = client.Jobs.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName, jobName);

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
                jobStatus = job.State
            });
        }
    }
}
