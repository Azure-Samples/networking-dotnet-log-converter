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

namespace OperationsLogConverterSample
{
    using Microsoft.Azure.Common.OData;
    using Microsoft.Azure;
    using Microsoft.Azure.Insights;
    using Microsoft.Azure.Insights.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Azure.Management.Resources;
    using Microsoft.Azure.Management.Resources.Models;
    using System.Text;

    public class Program
    {
        private const string SubscriptionID = "<Azure Subscription ID>";
        private const string TenantID = "<Azure Active Directory Tenant ID>";
        private const string ClientID = "<Client ID or Application ID>";
        private const string CSVExportNamePath = "OpsLog.csv";
        private static readonly Uri RedirectURI = new Uri("http://www.microsoft.com");

        static void Main(string[] args)
        {
            Console.WriteLine("Starting operations log export.");

            string token = GetAuthorizationHeader();

            TokenCloudCredentials credentials = new TokenCloudCredentials(SubscriptionID, token);

            InsightsClient client = new InsightsClient(credentials);

            DateTime endDateTime = DateTime.Now;

            DateTime startDateTime = endDateTime.AddDays(-90);
            
            string filterString = FilterString.Generate<ListEventsForResourceProviderParameters>(eventData => (eventData.EventTimestamp >= startDateTime) && (eventData.EventTimestamp <= endDateTime));
                     

            EventDataListResponse response = client.EventOperations.ListEvents(filterString, selectedProperties: null);

            ResourceManagementClient resClient = new ResourceManagementClient(credentials);

            IList<EventData> logList = response.EventDataCollection.Value;

            ExportOpsLogToCSV(logList, resClient);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        private static string GetAuthorizationHeader()
        {
            AuthenticationContext context = new AuthenticationContext($"https://login.windows.net/{TenantID}");

            AuthenticationResult result = context.AcquireToken("https://management.core.windows.net/", ClientID, RedirectURI);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the token.");
            }

            return result.AccessToken;          
        }

        private static void ExportOpsLogToCSV(IList<EventData> eventDataList, ResourceManagementClient resclient)
        {
            using (StreamWriter file = new StreamWriter(CSVExportNamePath))
            {
                file.WriteLine("SubscriptionId,EventTimeStamp,EventDate,EventDataId,CorrelationId,EventName,Level,"
                               + "ResourceGroupName,ResourceProviderName,ResourceUri,ResourceName,ResourceLocation,"
                               + "Status,Caller,OperationId,OperationName,OperationRP,OperationResType,OperationType,"
                               + "Description,Title,Service,Region,Transcript,IncidentId,IncidentType");

                foreach(EventData eventEntry in eventDataList)
                {
                    Tuple<string, string> resourceNameUriPair = GetAllResourceNameAndLocation(eventEntry, eventEntry.ResourceGroupName, eventEntry.ResourceUri, resclient);
                    Tuple<string, string, string> operationNameTrio = ParseOperationName(eventEntry);
                    Tuple<string, string, string, string, string, string> resourceProviderSextet = GetResourceProviderName(eventEntry);

                    DateTime convertedTimeStamp = eventEntry.EventTimestamp.ToUniversalTime();

                    file.WriteLine($"{eventEntry.SubscriptionId},{convertedTimeStamp},{convertedTimeStamp.Date},{eventEntry.EventDataId?.Replace(',', ';')}"
                                   + $",{eventEntry.CorrelationId?.Replace(',', ';')},{eventEntry.CorrelationId?.Replace(',', ';')},{eventEntry.EventName.Value?.Replace(',', ';')}"
                                   + $",{eventEntry.Level.ToString()},{eventEntry.ResourceGroupName?.Replace(',', ';')},{eventEntry.ResourceProviderName.Value?.Replace(',', ';')}"
                                   + $",{eventEntry.ResourceUri?.Replace(',', ';')},{resourceNameUriPair.Item1},{resourceNameUriPair.Item2},{eventEntry.Status.Value?.Replace(',', ';')}"
                                   + $",{eventEntry.Status.Value?.Replace(',', ';')},{eventEntry.Caller?.Replace(',', ';')},{eventEntry.OperationId},{eventEntry.OperationName.Value?.Replace(',', ';')}"
                                   + $",{operationNameTrio.Item1},{operationNameTrio.Item2},{operationNameTrio.Item3},{eventEntry.Description?.Replace(',', ';').Replace(System.Environment.NewLine, string.Empty)}"
                                   + $",{resourceProviderSextet.Item1},{resourceProviderSextet.Item2},{resourceProviderSextet.Item3},{resourceProviderSextet.Item4}"
                                   + $",{resourceProviderSextet.Item5},{resourceProviderSextet.Item6}");
                }
            }
        }

        private static Tuple<string, string, string, string, string, string> GetResourceProviderName(EventData eventEntry)
        {
            Tuple<string, string, string, string, string, string> resultSet = 
                new Tuple<string, string, string, string, string, string>(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            if (eventEntry.ResourceProviderName == null)
            {
                return resultSet;
            }

            if (eventEntry.ResourceProviderName.Value == "Azure.Health")
            {
                string titleProp = eventEntry.Properties.ContainsKey("Title") ? eventEntry.Properties["Title"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;
                string serviceProp = eventEntry.Properties.ContainsKey("Service") ? eventEntry.Properties["Service"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;
                string regionProp = eventEntry.Properties.ContainsKey("Region") ? eventEntry.Properties["Region"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;
                string tranCommProp = eventEntry.Properties.ContainsKey("Transcript Of Communication") ? eventEntry.Properties["Transcript Of Communication"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;
                string incidentIDProp = eventEntry.Properties.ContainsKey("IncidentId") ? eventEntry.Properties["IncidentId"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;
                string incidentTypeProp = eventEntry.Properties.ContainsKey("IncidentType") ? eventEntry.Properties["IncidentType"].Replace(',', ';').Replace(System.Environment.NewLine, string.Empty) : string.Empty;

                resultSet = new Tuple<string, string, string, string, string, string>(titleProp, serviceProp, regionProp, tranCommProp, incidentIDProp, incidentTypeProp);
            }

            return resultSet;
        }

        private static Tuple<string, string> GetAllResourceNameAndLocation(EventData eventEntry, string resourceGroupName, string resourceUri, ResourceManagementClient resclient)
        {
            Tuple<string, string> resultSet = new Tuple<string, string>(string.Empty, string.Empty);

            if (!string.IsNullOrWhiteSpace(resourceGroupName) && !string.IsNullOrWhiteSpace(resourceUri))
            {
                try
                {
                    ResourceListParameters reslist = new ResourceListParameters();

                    reslist.ResourceGroupName = eventEntry.ResourceGroupName;

                    ResourceListResult resresult = resclient.Resources.List(reslist);

                    foreach (GenericResourceExtended resource in resresult.Resources)
                    {
                        if (resource.Id == eventEntry.ResourceUri)
                        {
                            resultSet = new Tuple<string, string>(resource.Name, resource.Location);
                            break;
                        }
                    }
                }
                catch
                {
                    return resultSet;
                }
            }

            return resultSet;
        }

        private static Tuple<string, string, string> ParseOperationName(EventData eventEntry)
        {
            Tuple<string, string, string> resultSet = new Tuple<string, string, string>(string.Empty, string.Empty, string.Empty);

            if (eventEntry.ResourceProviderName == null)
            {
                return resultSet;
            }

            if (string.IsNullOrWhiteSpace(eventEntry.ResourceProviderName.Value))
            {
                if (eventEntry.ResourceProviderName.Value != "Azure.Health")
                {
                    if (eventEntry.OperationName.Value.Contains("/"))
                    {
                        string[] entry = eventEntry.OperationName.Value.ToLower().Replace("/action", "").Split('/');

                        string operationResProvider = entry[0].Replace("microsoft.", "");
                        string operationResType = entry[1];

                        StringBuilder operationTypeName = new StringBuilder();

                        if (entry.Length > 2)
                        {
                            for(int i = 2; i < entry.Length; i++)
                            {
                                operationTypeName.Append(entry[i]);
                                if (i < entry.Length)
                                {
                                    operationTypeName.Append("/");
                                }
                            }
                        }

                        resultSet = new Tuple<string, string, string>(operationResProvider, operationResType, operationTypeName.ToString());
                    }
                }
            }

            return resultSet;
        }
    }
}
