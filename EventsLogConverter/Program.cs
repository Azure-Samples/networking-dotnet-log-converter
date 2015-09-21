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

    public class Program
    {
        private const string BlobStorageConnectionString = "<Blob storage connectiong string>";
        private const string EventContainerName = "<Events log Container Name>";
        private const string EventCSVExportNamePath = "EventsLog.csv";
        private const string SubscriptionID = "<Azure Subscription ID>";
        private const string ResrouceGroupsName = "<Resource group name>";
        private const string ResrouceTypeName = "<Resource type name>";
        private const string ProviderName = "MICROSOFT.NETWORK";
        private const string FilterDatetimeStart = "9/1/2015 1:00 AM";
        private const string FilterDateTimeEnd = "9/2/2015 10:00 PM";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting events log export.");

            DateTime startDateTime = DateTime.Parse(FilterDatetimeStart);

            DateTime endDateTime = DateTime.Parse(FilterDateTimeEnd);

            GetNetworkSecurityGroupEvents(startDateTime, endDateTime);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        private static void GetNetworkSecurityGroupEvents(DateTime logStart, DateTime logEnd)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            Console.WriteLine($"Getting reference to container {EventContainerName}");

            CloudBlobContainer container = blobClient.GetContainerReference(EventContainerName);

            StorageURL storageUrl = new StorageURL(container.Uri, SubscriptionID, ResrouceGroupsName, ProviderName, ResrouceTypeName);

            List<Log> logs = new List<Log>();

            int itemPosition = 0;

            for (DateTime logTimeStamp = logStart; logTimeStamp <= logEnd; logTimeStamp = logTimeStamp.AddHours(1))
            {
                Console.WriteLine(logTimeStamp);

                Uri storageBlogUrl = storageUrl.GetURL(logTimeStamp);

                CloudBlockBlob blockBlob = new CloudBlockBlob(storageBlogUrl, storageAccount.Credentials);

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
                  Console.WriteLine($"{ex.Message} - {storageBlogUrl}");
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(EventCSVExportNamePath))
            {
                file.WriteLine("time,systemId,resourceId,operationName,properties.vnetResourceGuid,properties.subnetPrefix,properties.macAddress,properties.ruleName,properties.direction,properties.priority,properties.type,properties.conditions.destinationPortRange,properties.conditions.sourcePortRange,properties.conditions.sourceIP,properties.conditions.destinationIP");

                foreach (Log log in logs)
                {
                    file.WriteLine($"{DateTime.Parse(log.time).ToUniversalTime()}, {log.systemId}, {log.resourceId}, {log.operationName}, {log.properties.vnetResourceGuid}, {log.properties.subnetPrefix}, {log.properties.macAddress}, {log.properties.ruleName}, {log.properties.direction}, {log.properties.priority}, {log.properties.type}, {log.properties.conditions.destinationPortRange}, {log.properties.conditions.sourcePortRange}, {log.properties.conditions.sourceIP?.Replace(',', ';')}, {log.properties.conditions.destinationIP?.Replace(',', ';')}");
                }
            }
        }
    }
}
