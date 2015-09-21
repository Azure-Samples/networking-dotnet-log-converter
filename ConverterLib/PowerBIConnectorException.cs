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
    class PowerBIConnectorException : Exception
    {
        public PowerBIConnectorException(string message) : base(message)
        { }
    }
}
