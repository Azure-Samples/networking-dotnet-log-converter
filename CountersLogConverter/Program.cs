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

namespace CountersLogConverterSample
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using PowerBIConnector;
    using PowerBIConnector.NetworkSecurityGroupRuleCounter;
    using Newtonsoft.Json;

    /// <summary>
    /// Sample code to download counters log and export it to a .CSV file.
    /// </summary>
    public class Program
    {
        /*************************************************************************************
         ** Below constants needs to be populated by user.
         ** Arguments can be retrieved from URL to the JSON log stored in the blob storage,
         ** it should look similar to below URL.
         ** https://shoebox.blob.core.windows.net/insights-logs-networksecuritygrouprulecounter/resourceId=
         ** /SUBSCRIPTIONS/D763EE4A-XXX-XXXX-XXXX-876035455EC4/RESOURCEGROUPS/INSIGHTOBONRPFOO
         ** /PROVIDERS/MICROSOFT.NETWORK/NETWORKSECURITYGROUPS/NSGINSIGHTOBONRPFOO
         ** /y=2015/m=08/d=26/h=00/m=00/PT1H.json
         *************************************************************************************/

        // Connection string to the blob storage.
        private const string BlobStorageConnectionString = "<Blob storage connectiong string>";
        // Container name in the above URL is : insights-logs-networksecuritygrouprulecounter
        private const string CounterContainerName = "<Counter log Container Name>";
        // This is path and name of the exported .CSV file.
        private const string CounterCSVExportNamePath = "CountsLog.csv";
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


        static void Main()
        {
            Console.WriteLine("Starting counters log export.");

            DateTime startDateTime = DateTime.Parse(FilterDatetimeStart);

            DateTime endDateTime = DateTime.Parse(FilterDateTimeEnd);

            GetNetworkSecurityGroupRuleCounters(startDateTime, endDateTime);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        /// <summary>
        /// Down loads JSON log files between 2 dates from the blob storage 
        /// and exports them into one .CSV file.
        /// </summary>
        /// <param name="logStart">Begin date and time of the log.</param>
        /// <param name="logEnd">End date and time of the log.</param>
        private static void GetNetworkSecurityGroupRuleCounters(DateTime logStart, DateTime logEnd)
        {
            // Creates client.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            Console.WriteLine($"Getting reference to container {CounterContainerName}");

            CloudBlobContainer container = blobClient.GetContainerReference(CounterContainerName);

            // Instantiate the URL generator.
            StorageURL storageUrl = new StorageURL(container.Uri, SubscriptionID, ResrouceGroupsName, ProviderName, ResrouceTypeName, ResourceType.NETWORKSECURITYGROUPS);

            List<Log> logs = new List<Log>();

            int itemPosition = 0;

            // Using the date and time as arguments download all logs from the storage blob.
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
                        // Deserialize JSON.
                        LogRecords logRecords = serializer.Deserialize<LogRecords>(jsonTextReader);

                        itemPosition = 0;

                        foreach (Log logItem in logRecords.records)
                        {
                            // Add deserialized logs.
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

            // Dump everything in the logs list into a file.
            using (StreamWriter file = new StreamWriter(CounterCSVExportNamePath))
            {
                file.WriteLine("time,systemId,resourceId,operationName,properties.vnetResourceGuid,properties.subnetPrefix"
                              + ",properties.macAddress,properties.ruleName,properties.direction,properties.type,properties.matchedConnections");

                foreach (Log log in logs)
                {
                    file.WriteLine($"{DateTime.Parse(log.time).ToUniversalTime()}, {log.systemId}, {log.resourceId}, {log.operationName}"
                                  + $", {log.properties.vnetResourceGuid}, {log.properties.subnetPrefix}, {log.properties.macAddress}"
                                  + $", {log.properties.ruleName}, {log.properties.direction}, {log.properties.type}, {log.properties.matchedConnections}");
                }
            }
        }

    }
}
