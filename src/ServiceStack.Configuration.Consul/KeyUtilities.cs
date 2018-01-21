// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DTO;

    /// <summary>
    /// Utility class for consul specific heirarchical storage concepts
    /// </summary>
    public static class KeyUtilities
    {
        /// <summary>
        /// The default consul setting prefix
        /// </summary>
        public const string Prefix = "ss/";
        
        /// <summary>
        /// The default consul lookup key
        /// </summary>
        /// <param name="key">the key name</param>
        /// <returns>the lookup key</returns>
        public static string GetDefaultLookupKey(string key) => $"{Prefix}{key}";
        
        private static readonly Dictionary<string, IEnumerable<string>> CachedPossibleKeys = new Dictionary<string, IEnumerable<string>>();

        /// <summary>
        /// For specified key gets a list of all possible locations from most -> least specific
        /// </summary>
        /// <param name="key">Default key</param>
        /// <returns>List of keys where value may be found (most to least specific)</returns>
        public static IEnumerable<string> GetPossibleKeys(string key)
        {
            if (CachedPossibleKeys.ContainsKey(key))
                return CachedPossibleKeys[key];

            var appHost = HostContext.AppHost;

            if (appHost == null)
                return new[] { GetKeyForSpecificity(key, KeySpecificity.Global) };

            if (appHost.Config == null)
                return new[]
                {
                    GetKeyForSpecificity(key, KeySpecificity.Service),
                    GetKeyForSpecificity(key, KeySpecificity.Global)
                };

            var possibleKeys = new[]
            {
                GetKeyForSpecificity(key, KeySpecificity.Instance), // instance specific (ss/keyname/service/127.0.0.1:8080)
                GetKeyForSpecificity(key, KeySpecificity.Version), // version specific (ss/keyname/service/v1)
                GetKeyForSpecificity(key, KeySpecificity.Service), // service specific (ss/keyname/service)
                GetKeyForSpecificity(key, KeySpecificity.Global) // global (ss/keyname)
            };
            CachedPossibleKeys.Add(key, possibleKeys);
            return possibleKeys;
        }

        /// <summary>
        /// Finds the most specific match for key from KeyValue candidates
        /// </summary>
        /// <param name="candidates">Collection of candidate results</param>
        /// <param name="key">Key to search for</param>
        /// <returns>Most specific matching KeyValue from collection</returns>
        public static KeyValue GetMostSpecificMatch(IEnumerable<KeyValue> candidates, string key)
        {
            if (candidates == null)
                return null;

            var keyValues = candidates as IList<KeyValue> ?? candidates.ToList();
            if (keyValues.Count == 0)
                return null;

            var possibleKeys = GetPossibleKeys(key).ToList();

            return keyValues.Reverse().FirstOrDefault(candidate => possibleKeys.Contains(candidate.Key));
        }

        /// <summary>
        /// Creates a formatted lookup for the hierarchical specificity
        /// </summary>
        /// <param name="key">the key name</param>
        /// <param name="specificity">the key specificity</param>
        /// <returns>the formatted key</returns>
        /// <exception cref="InvalidOperationException">Thrown if a version specific key is requested but no version has been set in the <see cref="HostConfig.ApiVersion"/></exception>
        public static string GetKeyForSpecificity(string key, KeySpecificity specificity)
        {
            if (specificity == KeySpecificity.LiteralKey)
                return key;

            var globalKey = !key.StartsWith(Prefix) ? GetDefaultLookupKey(key) : key;

            if (specificity == KeySpecificity.Global)
                return globalKey; // global (ss/keyname)

            var appHost = HostContext.AppHost;

            var serviceName = appHost.ServiceName;
            var serviceKey = $"{globalKey}/{serviceName}";

            if (specificity == KeySpecificity.Service)
                return serviceKey; // service specific (ss/keyname/service)

            if (specificity == KeySpecificity.Instance)
            {
                var instanceId = GetInstanceId(appHost.Config);
                return $"{serviceKey}/i/{instanceId}"; // instance specific (ss/keyname/service/127.0.0.1:8080)
            }

            if (string .IsNullOrWhiteSpace(appHost.Config?.ApiVersion))
                throw new InvalidOperationException("Unable to get Version specific key when Version not set");

            var version = appHost.Config.ApiVersion.Replace("/", ".");
            return $"{serviceKey}/{version}"; // version specific (ss/keyname/service/v1)
        }

        private static string GetInstanceId(HostConfig config)
        {
            var hostUrl = string.IsNullOrEmpty(config.WebHostUrl)
                              ? string.Empty
                              : config.WebHostUrl
                                      .WithTrailingSlash()
                                      .Replace("http://", string.Empty)
                                      .Replace("https://", string.Empty)
                                      .Replace("/", "|");

            var factoryPath = config.HandlerFactoryPath.Replace("/", "|");

            return $"{hostUrl}{factoryPath}";
        }
    }
}
