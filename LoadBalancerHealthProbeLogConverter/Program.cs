//----------------------------------------------------------------------------------
// Microsoft Azure Networking
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// This code is licensed under the MIT License (MIT).  THIS CODE IS PROVIDED *AS IS* 
// WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED 
// WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, 
// OR NON-INFRINGEMENT. 
//----------------------------------------------------------------------------------

namespace LoadBalancerHealthProbeLogConverterSample
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Newtonsoft.Json;
    using PowerBIConnector;
    using PowerBIConnector.LoadBalancLoadBalancerProbeHealthStatuserEvent;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Sample code to download health status logs and export it to a .CSV file.
    /// </summary>
    class Program
    {
        /*************************************************************************************
        ** Below constants needs to be populated by user.
        ** Arguments can be retrieved from URL to the JSON log stored in the blob storage,
        ** it should look similar to below URL.
        ** https://shoebox.blob.core.windows.net/insights-logs-loadbalancerprobehealthstatus/resourceId=
        ** /SUBSCRIPTIONS/<subscription id>/RESOURCEGROUPS/SLBDEMO/PROVIDERS/MICROSOFT.NETWORK/LOADBALANCERS/MYLB/y=2016/m=01/d=27/h=04/m=00/PT1H.json
        *************************************************************************************/

        // Connection string to the blob storage.
        private const string BlobStorageConnectionString = "<Blob storage connectiong string>";
        // Container name in the above URL is : insights-logs-loadbalancerprobehealthstatus
        private const string EventContainerName = "insights-logs-loadbalancerprobehealthstatus";
        // This is path and name of the exported .CSV file.
        private const string EventCSVExportNamePath = "LoadBalancerHealthStatusLog.csv";
        // Subscription in the above url is : D763EE4A-XXX-XXXX-XXXX-876035455EC4
        // All alphabets must be in uppercase.
        private const string SubscriptionID = "<Azure Subscription ID>";
        // Resource groups : SLBDEMO
        private const string ResrouceGroupsName = "<Resource group name>";
        // Resource type name : MYLB
        private const string ResrouceTypeName = "<Resource type name>";
        // Provider name : MICROSOFT.NETWORK
        private const string ProviderName = "MICROSOFT.NETWORK";
        // Beginning date and time of the exporting log segments.
        private const string FilterDatetimeStart = "1/1/2016 00:00 AM";
        // End date and time of the exporting log segments.
        private const string FilterDateTimeEnd = "1/1/2016 03:00 AM";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting load balancer health status log export.");

            DateTime startDateTime = DateTime.Parse(FilterDatetimeStart);

            DateTime endDateTime = DateTime.Parse(FilterDateTimeEnd);

            GetLoadBalancerEvents(startDateTime, endDateTime);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void GetLoadBalancerEvents(DateTime logStart, DateTime logEnd)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            Console.WriteLine($"Getting reference to container {EventContainerName}");

            CloudBlobContainer container = blobClient.GetContainerReference(EventContainerName);

            StorageURL storageUrl = new StorageURL(container.Uri, SubscriptionID, ResrouceGroupsName, ProviderName, ResrouceTypeName, ResourceType.LOADBALANCERS);

            List<Log> logs = new List<Log>();

            int itemPosition = 0;

            // Using the date and time as arguments download all logs from the storage blob.
            for (DateTime logTimeStamp = logStart; logTimeStamp <= logEnd; logTimeStamp = logTimeStamp.AddHours(1))
            {
                Console.WriteLine(logTimeStamp);

                Uri storageBlobUrl = storageUrl.GetURL(logTimeStamp);

                CloudBlockBlob blockBlob = new CloudBlockBlob(storageBlobUrl, storageAccount.Credentials);

                MemoryStream memstream = new MemoryStream();

                try
                {
                    blockBlob.DownloadToStream(memstream);

                    memstream.Position = 0;

                    JsonSerializer serializer = new JsonSerializer();

                    using (StreamReader sr = new StreamReader(memstream))
                    {
                        using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                        {
                            LogRecords logRecords = serializer.Deserialize<LogRecords>(jsonTextReader);

                            itemPosition = 0;

                            foreach (Log logItem in logRecords.records)
                            {
                                logs.Add(logItem);
                                itemPosition++;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message} - {storageBlobUrl}");
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(EventCSVExportNamePath))
            {
                file.WriteLine("time,systemId,category,resourceId,operationName,properties.publicIpAddress"
                              + ",properties.port,properties.totalDipCount,properties.dipDownCount,properties.healthPercentage");

                foreach (Log log in logs)
                {
                    file.WriteLine($"{DateTime.Parse(log.time).ToUniversalTime()}, {log.systemId}, {log.category}, {log.resourceId}, {log.operationName}"
                                  + $", {log.properties.publicIpAddress }, {log.properties.port}, {log.properties.totalDipCount}"
                                  + $", {log.properties.dipDownCount}, {log.properties.healthPercentage}");
                }
            }
        }
    }
}
