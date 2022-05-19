/* Copyright (C) 2022 Chuck Noble <chuck@gamergenic.com>
 * This work is free.  You can redistribute it and /or modify it under the
 * terms of the Do What The Fuck You Want To Public License, Version 2,
 * as published by Sam Hocevar.  See http://www.wtfpl.net/ for more details.
 *
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://www.wtfpl.net/ for more details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using Newtonsoft.Json;
#nullable enable

namespace ConjunctionAlerts
{
    public enum RadarCrossSection
    {
        [EnumMember(Value = "SMALL")]
        Small,

        [EnumMember(Value = "MEDIUM")]
        Medium,

        [EnumMember(Value = "LARGE")]
        Large
    };

    public enum ObjectType
    {
        [EnumMember(Value = "PAYLOAD")]
        Payload,

        [EnumMember(Value = "ROCKET BODY")]
        RocketBody,

        [EnumMember(Value = "Debris")]
        Debris,

        [EnumMember(Value = "UKNOWN")]
        Unknown
    };

    public struct Conjunction
    {
        public string CDM_ID;               //"280352437",
        public DateTime CREATED;            //2022-04-29 00:45:12.000000",
        public string EMERGENCY_REPORTABLE; //"Y",
        public DateTime TCA;                // "2022-04-29T22:03:26.595000",
        public int MIN_RNG;                 // "4",
        public double? PC;                   // "0.1663616",
        public int SAT_1_ID;                // "22487",
        public string SAT_1_NAME;           // "COSMOS 2233",
        public ObjectType SAT1_OBJECT_TYPE; // "PAYLOAD",
        public RadarCrossSection? SAT1_RCS; // "LARGE",
        public double SAT_1_EXCL_VOL;       // "5.00",
        public int SAT_2_ID;                // "5758",
        public string SAT_2_NAME;           // "THORAD AGENA D DEB",
        public ObjectType SAT2_OBJECT_TYPE; // "DEBRIS",
        public RadarCrossSection? SAT2_RCS; // "SMALL",
        public double SAT_2_EXCL_VOL;       // "5.00"
    };

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ConjunctionAlerts.exe");
                Console.WriteLine("\nusage:");
                Console.WriteLine("ConjunctionAlerts [-u <username>] [-p <password>] <objectid>");
                Console.WriteLine("\nalternatively, environment vars SPACETRACK_USER and SPACETRACK_PASSWORD can supply credentials.");
                return;
            }

            string? userRef = Environment.GetEnvironmentVariable("SPACETRACK_USER");
            string? passwordRef = Environment.GetEnvironmentVariable("SPACETRACK_PASSWORD");

            for (int i = 0; i < args.Length - 1; ++i)
            {
                switch (args[i])
                {
                    case "-u":
                        userRef = args[i + 1];
                        break;
                    case "-p":
                        passwordRef = args[i + 1];
                        break;
                }
            }

            if (string.IsNullOrEmpty(userRef) || string.IsNullOrEmpty(passwordRef))
            {
                Console.WriteLine(userRef == null ? "Missing user" : "Missing password");
                return;
            }

            Console.WriteLine("Querying SpaceTrack active for Conjunction Alerts...");

            string jsonResponse;
            using (var client = new WebClient())
            {
                string uriBase = "https://www.space-track.org";
                string requestController = "/basicspacedata";
                string requestAction = "/query";
                // https://www.space-track.org/basicspacedata/query/class/cdm_public/TCA/%3E2022-05-19T07:20:26.595000/orderby/TCA%20asc/emptyresult/show
                string predicateValues = "/class/cdm_public/TCA/%3E" + DateTime.UtcNow.ToString("s") + "/orderby/TCA%20asc/emptyresult/show";
                string requestUrl = uriBase + requestController + requestAction + predicateValues;

                // Store the user authentication information
                var data = new NameValueCollection
                {
                    { "identity", userRef },
                    { "password", passwordRef },
                    { "query", requestUrl },
                };

                byte[] requestResponse;
                try
                {
                    // Generate the URL for the API Query and return the response
                    requestResponse = client.UploadValues(uriBase + "/ajaxauth/login", data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return;
                }

                jsonResponse = System.Text.Encoding.Default.GetString(requestResponse);
            }


            Conjunction[] deserializedResponse = null;
            try
            {
                deserializedResponse = JsonConvert.DeserializeObject<Conjunction[]>(jsonResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not parse response: {0}", e.ToString());
                return;
            }

            if(deserializedResponse != null)
            {
                Console.WriteLine("Found {0} Records", deserializedResponse.Length);
                Console.WriteLine("-------------------------");
                foreach (var ConjunctionAlert in deserializedResponse)
                {
                    Console.WriteLine("\nCDM_ID {0} PC {1} TCA {2} EMERGENCY_REPORTABLE {3}", ConjunctionAlert.CDM_ID, ConjunctionAlert.PC, ConjunctionAlert.TCA, ConjunctionAlert.EMERGENCY_REPORTABLE);
                    Console.WriteLine("   SAT_1_NAME {0} ({1}) SAT_2_NAME {2} ({3})", ConjunctionAlert.SAT_1_NAME, ConjunctionAlert.SAT1_OBJECT_TYPE, ConjunctionAlert.SAT_2_NAME, ConjunctionAlert.SAT2_OBJECT_TYPE);
                }
                Console.WriteLine("-------------------------");
                Console.WriteLine("Printed {0} Records", deserializedResponse.Length);
            }
        }
    }
}
