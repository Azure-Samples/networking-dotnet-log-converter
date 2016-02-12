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

namespace PowerBIConnector
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Type definitions for parsing JSON log entries.
    /// NetworkSecurityGroupRuleCounter.Log class encapsulates counters log.
    /// NetworkSecurityGroupRuleEvent.Log class encapsulates events log.
    /// </summary>
    public class LogPropertyBase
    {
        public string macAddress { get; set; }

        public string ruleName { get; set; }

        public string direction { get; set; }

        public string type { get; set; }

        public string vnetResourceGuid { get; set; }

        public string subnetPrefix { get; set; }
    }

    public class LogBase
    {
        public string time { get; set; }

        public string systemId { get; set; }

        public string category { get; set; }

        public string resourceId { get; set; }

        public string operationName { get; set; }
    }

    namespace NetworkSecurityGroupRuleCounter
    {
        public class LogProperty : LogPropertyBase
        {
            public string matchedConnections { get; set; }
        }

        public class Log : LogBase
        {
            public LogProperty properties { get; set; }
        }

        public class LogRecords
        {
            public List<Log> records { get; set; }
        }
    }

    namespace NetworkSecurityGroupRuleEvent
    {
        public class LogPropertyCondition
        {
            public string destinationPortRange { get; set; }

            public string sourcePortRange { get; set; }

            public string sourceIP { get; set; }

            public string destinationIP { get; set; }

            public string protocols { get; set; }
        }

        public class LogProperty : LogPropertyBase
        {
            public string priority { get; set; }
            public LogPropertyCondition conditions { get; set; }
        }

        public class Log : LogBase
        {
            public LogProperty properties { get; set; }
        }

        public class LogRecords
        {
            public List<Log> records { get; set; }
        }
    }

    namespace LoadBalancLoadBalancerProbeHealthStatuserEvent
    {
        public class LogProperty
        {
            public string publicIpAddress { get; set; }

            public string port { get; set; }

            public string totalDipCount { get; set; }

            public string dipDownCount { get; set; }

            public string healthPercentage { get; set; }
        }

        public class Log : LogBase
        {
            public LogProperty properties { get; set; }
        }

        public class LogRecords
        {
            public List<Log> records { get; set; }
        }
    }

    namespace LoadBalancerAlertEvent
    {
        public class EventProperty
        {
            [JsonProperty(propertyName: "public ip address")]
            public string publicIpAddress { get; set; }
        }

        public class LogProperty
        {
            public string eventName { get; set; }

            public string eventDescription { get; set; }

            public EventProperty eventProperties { get; set; }
        }

        public class Log : LogBase
        {
            public LogProperty properties { get; set; }
        }

        public class LogRecords
        {
            public List<Log> records { get; set; }
        }
    }
}
