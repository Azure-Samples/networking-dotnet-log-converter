---
services: virtual-network
platforms: dotnet
author: JayHCho
---

# Azure Networking PowerBI Connector

Code for downloading network monitoring logs and converting them to .CSV files to be uploaded to Power BI for analysis.
## Running this sample
### Prerequisites:

* Visual Studio 2015
* Microsoft Azure SDK - [Latest](https://azure.microsoft.com/en-us/downloads/)
* Cloud Explorer for Visual Studio 2015 - [Visual Studio Extension](https://visualstudiogallery.msdn.microsoft.com/84e83a7c-9606-4f9f-83dd-0f6182f13add)
 
## Solution Contents
The solution contains 3  executable projects CountersLogConverter, EventsLogConverter and OperationsLogConverter.

```
1.  CountersLogConverter

In order to use this code logging must be turned on via SDK or Ibiza portal (soon to be released)
and familiarity with Azure Resource Manager is required.
```
```
2.  EventsLogConverter

In order to use this code logging must be turned on via SDK or Ibiza portal (soon to be released)
and familiarity with Azure Resource Manager is required.
```
```
3.  OperationsLogConverter

This app needs to be authorized to access Azure AD management API.
Fore more information on Azure AD authorization go to: https://msdn.microsoft.com/en-us/library/azure/dn790557.aspx
```

