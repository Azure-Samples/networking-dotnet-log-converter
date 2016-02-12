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

using System.Linq;

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
        /************************************************************************************* 
         ** Read this to configure your subscriptionid, tenantid, client id and uri 
         ** 1.	SubscriptionID: 
         **        a. https://manage.windowsazure.com > Settings > SUBSCRIPTION ID 
         **           OR
         **        b. https://portal.azure.com > Browse > Subscriptions > SUBSCRIPTION ID 
         ** 2.	For TenantID/ClientID/URI follow the "Set up authentication using the Management Portal" step in Authenticating Azure Resource Manager requests 
         **        a. TenantID - found in Step 3.3 
         **        b. ClientID(aka Application ID) - found in Step 3.3 
         **        c. URI(aka Redirect URI) - found in Step 1.4 
         *************************************************************************************/

        private const string SubscriptionID = "<Azure Subscription ID>";
        private const string TenantID = "<Azure Active Directory Tenant ID>";
        private const string ClientID = "<Client ID or Application ID>";
        private const string CSVExportNamePath = "OpsLog.csv";
        private static readonly Uri RedirectURI = new Uri("http://www.microsoft.com");
        private const double days = -10; //max = -90  (90 days of logs is stored by audit logs)

        static void Main(string[] args)
        {
            Console.WriteLine("Starting operations log export.");

            string token = GetAuthorizationHeader();

            TokenCloudCredentials credentials = new TokenCloudCredentials(SubscriptionID, token);
            InsightsClient client = new InsightsClient(credentials);

            DateTime endDateTime = DateTime.Now;
            DateTime startDateTime = endDateTime.AddDays(days);
            
            string filterString = FilterString.Generate<ListEventsForResourceProviderParameters>(eventData => (eventData.EventTimestamp >= startDateTime) && (eventData.EventTimestamp <= endDateTime));
                     
            EventDataListResponse response = client.EventOperations.ListEvents(filterString, selectedProperties: null);
            List<EventData> logList = new List<EventData>(response.EventDataCollection.Value);

            while (!string.IsNullOrEmpty(response.EventDataCollection.NextLink))
            {
                Console.WriteLine($"Retrieving page {response.EventDataCollection.NextLink}");

                response = client.EventOperations.ListEventsNext(response.EventDataCollection.NextLink);
                logList.AddRange(response.EventDataCollection.Value);
            }

            ResourceManagementClient resClient = new ResourceManagementClient(credentials);
          
            Console.WriteLine($"Page retrieval completed, preparing to write to a file {CSVExportNamePath}.");

            ExportOpsLogToCSV(logList, resClient);

            Console.WriteLine("Export completed.");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
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
            using (StreamWriter file = File.AppendText(CSVExportNamePath))
            {
                file.WriteLine("SubscriptionId,EventTimeStamp,EventDate,EventDataId,CorrelationId,EventName,Level"
                               + ",ResourceGroupName,ResourceProviderName,ResourceId,ResourceName,ResourceLocation"
                               + ",Status,Caller,OperationId,OperationName,OperationRP,OperationResType,OperationType"
                               + ",Description,Title,Service,Region,Transcript,IncidentId,IncidentType");

                foreach(EventData eventEntry in eventDataList)
                {
                    Tuple<string, string> resourceNameUriPair = GetAllResourceNameAndLocation(eventEntry, eventEntry.ResourceGroupName, eventEntry.ResourceId, resclient);
                    Tuple<string, string, string> operationNameTrio = ParseOperationName(eventEntry);
                    Tuple<string, string, string, string, string, string> resourceProviderSextet = GetResourceProviderName(eventEntry);

                    DateTime convertedTimeStamp = eventEntry.EventTimestamp.ToUniversalTime();


                    //                           SubscriptionId | EventTimeStamp     | EventDate               | EventDataId  
                    file.WriteLine($"{eventEntry.SubscriptionId},{convertedTimeStamp},{convertedTimeStamp.Date},{eventEntry.EventDataId?.Replace(',', ';')}"
                    //                 | CorrelationId                               | EventName
                                   + $",{eventEntry.CorrelationId?.Replace(',', ';')},{eventEntry.EventName.Value?.Replace(',', ';')}"
                    //                 | Level            | ResourceGroupName                               | ResourceProviderName 
                                   + $",{eventEntry.Level},{eventEntry.ResourceGroupName?.Replace(',', ';')},{eventEntry.ResourceProviderName.Value?.Replace(',', ';')}"
                    //                 | ResourceId                               | ResourceName              | ResourceLocation          | Status
                                   + $",{eventEntry.ResourceId?.Replace(',', ';')},{resourceNameUriPair.Item1},{resourceNameUriPair.Item2},{eventEntry.Status.Value?.Replace(',', ';')}"
                    //                 | Caller                               | OperationId            | OperationName  
                                   + $",{eventEntry.Caller?.Replace(',', ';')},{eventEntry.OperationId},{eventEntry.OperationName.Value?.Replace(',', ';')}"
                    //                 | OperationRP             | OperationResType        | OperationType           | Description                         
                                   + $",{operationNameTrio.Item1},{operationNameTrio.Item2},{operationNameTrio.Item3},{eventEntry.Description?.Replace(',', ';').Replace(System.Environment.NewLine, string.Empty)}"
                    //                 | Title                        | Service                      | Region                       | Transcript  
                                   + $",{resourceProviderSextet.Item1},{resourceProviderSextet.Item2},{resourceProviderSextet.Item3},{resourceProviderSextet.Item4}"
                    //                 | IncidentId                   | IncidentType
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
                string titleProp = eventEntry.Properties.ContainsKey("Title") ? eventEntry.Properties["Title"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;
                string serviceProp = eventEntry.Properties.ContainsKey("Service") ? eventEntry.Properties["Service"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;
                string regionProp = eventEntry.Properties.ContainsKey("Region") ? eventEntry.Properties["Region"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;
                string tranCommProp = eventEntry.Properties.ContainsKey("Transcript Of Communication") ? eventEntry.Properties["Transcript Of Communication"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;
                string incidentIDProp = eventEntry.Properties.ContainsKey("IncidentId") ? eventEntry.Properties["IncidentId"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;
                string incidentTypeProp = eventEntry.Properties.ContainsKey("IncidentType") ? eventEntry.Properties["IncidentType"]?.Replace(',', ';').Replace(Environment.NewLine, string.Empty) : string.Empty;

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
                        if (resource.Id == eventEntry.ResourceId)
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
