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

namespace EventsLogConverterSample
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using PowerBIConnector;
    using PowerBIConnector.NetworkSecurityGroupRuleEvent;
    using Newtonsoft.Json;

    /// <summary>
    /// Sample code to download events log and export it to a .CSV file.
    /// </summary>
    public class Program
    {
        /*************************************************************************************
         ** Below constants needs to be populated by user.
         ** Arguments can be retrieved from URL to the JSON log stored in the blob storage,
         ** it should look similar to below URL.
         ** https://shoebox.blob.core.windows.net/insights-logs-networksecuritygroupevent
         ** /resourceId=/SUBSCRIPTIONS/D763EE4A-XXX-XXXX-XXXX-876035455EC4/RESOURCEGROUPS/INSIGHTOBONRPFOO
         ** /PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/NSGINSIGHTOBONRPFOO/y=2015/m=08/d=26/h=00/m=00/PT1H.json
         *************************************************************************************/

        // Connection string to the blob storage.
        private const string BlobStorageConnectionString = "<Blob storage connectiong string>";
        // Container name in the above URL is : insights-logs-networksecuritygroupevent
        private const string EventContainerName = "<Events log Container Name>";
        // This is path and name of the exported .CSV file.
        private const string EventCSVExportNamePath = "EventsLog.csv";
        // Subscription in the above url is : D763EE4A-XXX-XXXX-XXXX-876035455EC4
        private const string SubscriptionID = "<Azure Subscription ID>";
        // Resource groups : INSIGHTOBONRPFOO
        private const string ResrouceGroupsName = "<Resource group name>";
        // Resource type name : NSGINSIGHTOBONRPFOO
        private const string ResrouceTypeName = "<Resource type name>";
        // Provider name : MICROSOFT.NETWORK
        private const string ProviderName = "MICROSOFT.NETWORK";
        // Beginning date and time of the exporting log segments.
        private const string FilterDatetimeStart = "9/1/2015 1:00 AM";
        // End date and time of the exporting log segments.
        private const string FilterDateTimeEnd = "9/2/2015 10:00 PM";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting events log export.");

            DateTime startDateTime = DateTime.Parse(FilterDatetimeStart);

            DateTime endDateTime = DateTime.Parse(FilterDateTimeEnd);

            GetNetworkSecurityGroupEvents(startDateTime, endDateTime);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void GetNetworkSecurityGroupEvents(DateTime logStart, DateTime logEnd)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            Console.WriteLine($"Getting reference to container {EventContainerName}");

            CloudBlobContainer container = blobClient.GetContainerReference(EventContainerName);

            StorageURL storageUrl = new StorageURL(container.Uri, SubscriptionID, ResrouceGroupsName, ProviderName, ResrouceTypeName, ResourceType.NETWORKSECURITYGROUPS);

            List<Log> logs = new List<Log>();

            int itemPosition = 0;

            for (DateTime logTimeStamp = logStart; logTimeStamp <= logEnd; logTimeStamp = logTimeStamp.AddHours(1))
            {
                Console.WriteLine(logTimeStamp);

                Uri storageblobUrl = storageUrl.GetURL(logTimeStamp);

                CloudBlockBlob blockBlob = new CloudBlockBlob(storageblobUrl, storageAccount.Credentials);

                MemoryStream memstream = new MemoryStream();

                try
                {
                    blockBlob.DownloadToStream(memstream);

                    memstream.Position = 0;

                    JsonSerializer serializer = new JsonSerializer();

                    using (StreamReader sr = new StreamReader(memstream))
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
                catch (Exception ex)
                {
                  Console.WriteLine($"{ex.Message} - {storageblobUrl}");
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(EventCSVExportNamePath))
            {
                file.WriteLine("time,systemId,resourceId,operationName,properties.vnetResourceGuid,properties.subnetPrefix"
                              + ",properties.macAddress,properties.ruleName,properties.direction,properties.priority"
                              + ",properties.type,properties.conditions.destinationPortRange,properties.conditions.sourcePortRange"
                              + ",properties.conditions.sourceIP,properties.conditions.destinationIP,properties.conditions.protocols");

                foreach (Log log in logs)
                {
                    file.WriteLine($"{DateTime.Parse(log.time).ToUniversalTime()}, {log.systemId}, {log.resourceId}, {log.operationName}"
                                  + $", {log.properties.vnetResourceGuid}, {log.properties.subnetPrefix}, {log.properties.macAddress}"
                                  + $", {log.properties.ruleName}, {log.properties.direction}, {log.properties.priority}, {log.properties.type}"
                                  + $", {log.properties.conditions.destinationPortRange}, {log.properties.conditions.sourcePortRange}"
                                  + $", {log.properties.conditions.sourceIP?.Replace(',', ';')}, {log.properties.conditions.destinationIP?.Replace(',', ';')}"
                                  + $", {(string.IsNullOrWhiteSpace(log.properties.conditions.protocols) ? "*" : log.properties.conditions.protocols?.Replace(',', ';'))}");
                }


            }
        }
    }
}
