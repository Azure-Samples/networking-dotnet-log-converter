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

using System;

namespace PowerBIConnector
{
    public class StorageURL
    {
        private const string RESOURCEID = "resourceId";
        private const string SUBSCRIPTIONS = "SUBSCRIPTIONS";
        private const string RESOURCEGROUPS = "RESOURCEGROUPS";
        private const string PROVIDERS = "PROVIDERS";
        private const string NETWORKSECURITYGROUPS = "NETWORKSECURITYGROUPS";
        private const string DEFAULTLOGBLOB = "PT1H.json";

        private readonly Uri containerUri;
        private readonly string subscriptionId;
        private readonly string resourceGroupsName;
        private readonly string providersName;
        private readonly string resourceType;
        private readonly string resourceTypeName;
        private readonly string blobName;

        public StorageURL(Uri containerUri, string subscriptionId, string resourceGroupsName, string providersName, string resourceTypeName, string resourceType = NETWORKSECURITYGROUPS, string blobName = DEFAULTLOGBLOB)
        {
            if ((containerUri == null)
                || String.IsNullOrWhiteSpace(subscriptionId)
                || String.IsNullOrWhiteSpace(resourceGroupsName)
                || String.IsNullOrWhiteSpace(providersName)
                || String.IsNullOrWhiteSpace(resourceTypeName)
                || String.IsNullOrWhiteSpace(resourceType)
                || String.IsNullOrWhiteSpace(blobName))
            {
                throw new PowerBIConnectorException("Invalid null argument found.");
            }

            this.containerUri = containerUri;
            this.subscriptionId = subscriptionId;
            this.resourceGroupsName = resourceGroupsName;
            this.providersName = providersName;
            this.resourceType = resourceType;
            this.resourceTypeName = resourceTypeName;
            this.blobName = blobName;
        }

        public Uri GetURL(DateTime logDateTimeStamp)
        {
            return GenerateURL(logDateTimeStamp);
        }

        private Uri GenerateURL(DateTime logDateTime)
        {
            string urlSegments = $"{containerUri.ToString()}{GenerateURLConfigSegments()}{GenerateURLDateTimeSegments(logDateTime)}/{blobName}";

            return new Uri(urlSegments);
        }

        private string GenerateURLConfigSegments()
        {
            return $"/{RESOURCEID}=/{SUBSCRIPTIONS}/{subscriptionId}/{RESOURCEGROUPS}/{resourceGroupsName}/{PROVIDERS}/{providersName}/{resourceType}/{resourceTypeName}";
        }

        private string GenerateURLDateTimeSegments(DateTime logDateTime)
        {
            return $"/y={logDateTime.ToString("yyyy")}/m={logDateTime.ToString("MM")}/d={logDateTime.ToString("dd")}/h={logDateTime.ToString("HH")}/m={logDateTime.ToString("mm")}";
        }
        
    }
}
