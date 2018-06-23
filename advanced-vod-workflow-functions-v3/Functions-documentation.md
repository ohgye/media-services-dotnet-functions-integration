---
services: media-services,functions,logic-app
platforms: dotnet
author: shigeyf
---

# Functions documentation

This section lists the functions available and describes the input and output parameters.
This Functions example is based on AMS REST API v3 and pre-compiled functions.

# Functions List

- [CreateEmptyAsset](#createemptyasset)
- [CreateTransform](#createtransform)
- [MonitorBlobContainerCopyStatus](#monitorblobcontainercopystatus)
- [MonitorMediaJob](#monitormediajob)
- [StartBlobContainerCopyToAsset](#startblobcontainercopytoasset)
- [SubmitMediaJob](#submitmediajob)


## CreateEmptyAsset
This function creates an empty asset.

```c#
Input:
    {
        // Name of the asset
        "assetNamePrefix": "TestAssetName",
        // (Optional) Name of attached storage account where to create the asset
        "assetStorageAccount":  "storage01"
    }
Output:
    {
        // Name of the asset created
        "assetName": "TestAssetName-180c777b-cd3c-4e02-b362-39b8d94d7a85",
        // Id of the asset created
        "assetId": "nb:cid:UUID:68adb036-43b7-45e6-81bd-8cf32013c810",
        // Name of the destination container name for the asset created
        "destinationContainer": "destinationContainer": "asset-4a5f429c-686c-4f6f-ae86-4078a4e6139e"
    }

```
- [List](#functions-list)


## CreateTransform
This function creates a new transform.

```c#
Input:
    {
        // Name of the Transform
        "transformName": "TestTransform",
        // Array of presets for the Transform
        "transformOutputs": [
            {
                "onError": "StopProcessingJob",
                "relativePriority": "Normal",
                "preset": {
                    "@odata.type": "#Microsoft.Media.BuiltInStandardEncoderPreset",
                    "presetName": "AdaptiveStreaming"
                }
            },
            {
                "onError": "StopProcessingJob",
                "relativePriority": "Normal",
                "preset": {
                    "@odata.type": "#Microsoft.Media.VideoAnalyzerPreset",
                    "audioLanguage": "en-US",
                    "audioInsightsOnly": false
                }
            }
        ]
    }
Output:
    {
        // Id of the created Transform
        "transformId": "/subscriptions/694d5930-8ee4-4e50-917b-9dcfeceb6179/resourceGroups/AMSdemo/providers/Microsoft.Media/mediaservices/amsdemojapaneast/transforms/TestTransform"
    }

```
- [List](#functions-list)


## MonitorBlobContainerCopyStatus
This function monitors blob copy.
[blobCopyStatus](https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage.blob.copystatus?view=azure-dotnet) is returned with the following values:
* 0 : Invalid - The copy status is invalid.
* 1 : Pending - The copy operation is pending.
* 2 : Success - The copy operation succeeded.
* 3 : Aborted - The copy operation has been aborted.
* 4 : Failed - The copy operation encountered an error.

```c#
Input:
	{
		// Name of the asset for copy destination
		"assetName": "TestAssetName-180c777b-cd3c-4e02-b362-39b8d94d7a85",
		// (Optional) File names for monitoring 
		//      all blobs in the destination container will be monitored if no fileNames
		"fileNames": [ "filename.mp4" , "filename2.mp4" ]
	}
Output:
	{
        // BlobCopy Action Status: true or false
        "copyStatus": true|false,
        // CopyStatus for each blob
        "blobCopyStatusList": [
            {
                // Name of blob
                "blobName": "filename.mp4",
                // Blob CopyStatus code (see below)
                "blobCopyStatus": 2
            }
        ]
	}

```
- [List](#functions-list)


## MonitorMediaJob
This function monitors media job.
[jobState](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.media.models.jobstate?view=azure-dotnet) is returned with the following values:

| JobState Name | Description   |
| ------------- | ------------- |
| Canceled | The job was canceled. This is a final state for the job. |
| Canceling | The job is in the process of being canceled. This is a transient state for the job. |
| Error | The job has encountered an error. This is a final state for the job. |
| Finished | The job is finished. This is a final state for the job. |
| Processing | The job is processing. This is a transient state for the job. |
| Queued | The job is in a queued state, waiting for resources to become available. This is a transient state. |
| Scheduled | The job is being scheduled to run on an available resource. This is a transient state, between queued and processing states. |
If jobState is "Error", the output will contain the job error information: [ErrorCode](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.media.models.joberrorcode?view=azure-dotnet) and ErrorMessage.

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
        // Status name of the media job
        "jobStatus": "Finished",
        // Status of each task/output asset in the media job
        "jobOutputStateList": [
            {
                // Name of the Output Asset
                "AssetName": "out-testasset-efbf71e8-3f80-480d-9b92-f02bef6ad4d2",
                // Status of the media task for the Output Asset
                "State": "Finished",
                // Progress of the media task for the Output Asset
                "Progress": 100
            },
            ...
        ]
    }

```
- [List](#functions-list)


## StartBlobContainerCopyToAsset
This function starts copying blob container to the asset.

```c#
Input:
	{
		// Name of the asset for copy destination
		"assetName": "TestAssetName-180c777b-cd3c-4e02-b362-39b8d94d7a85",
		// Id of the asset for copy destination
		"assetId": "nb:cid:UUID:4a5f429c-686c-4f6f-ae86-4078a4e6139e",
		// Name of the storage account for copy source
		"sourceStorageAccountName": "mediaimports",
		// Key of the storage account for copy source
		"sourceStorageAccountKey": "keyxxx==",
		// Blob container name of the storage account for copy source
		"sourceContainer":  "movie-trailer"
		// (Optional) File names of source contents
		//      all blobs in the source container will be copied if no fileNames
		"fileNames": [ "filename.mp4" , "filename2.mp4" ]
	}
Output:
	{
		// Container Name of the asset for copy destination
		"destinationContainer": "asset-4a5f429c-686c-4f6f-ae86-4078a4e6139e"
	}

```
- [List](#functions-list)


## SubmitMediaJob
This function submits media job.

```c#
Input:
    {
        // Name of the Asset for media job input
        "inputAssetName": "TestAssetName-180c777b-cd3c-4e02-b362-39b8d94d7a85",
        // Name of the Transform for media job
        "transformName": "TestTransform",
        // Name of the Assets for media job outputs
        "outputAssetNamePrefix": "TestOutputAssetName",
        // (Optional) Name of attached storage account where to create the Output Assets
        "assetStorageAccount": "storage01"
    }
Output:
    {
        // Name of media Job
        "jobName": "amsv3function-job-24369d2e-7415-4ff5-ba12-b8a879a15401",
        // Name of Encdoer Output Asset
        "encoderOutputAssetName": "out-testasset-e389de79-3aa5-4a5a-a9ca-2a6fd8c53968",
        // Name of Video Analyzer Output Asset
        "videoAnalyzerOutputAssetName": "out-testasset-00cd363b-5fe0-4da1-acf8-ebd66ef14504"
    }

```
- [List](#functions-list)
