// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System.Collections.Generic;

    public class KeyUtilities
    {
        public static string GetDefaultLookupKey(string key) => $"ss/{key}";

        /// <summary>
        /// For specified key gets a list of all possible locations from most -> least specific
        /// </summary>
        /// <param name="key">Default key</param>
        /// <returns>List of keys where value may be found (most to least specific)</returns>
        public static IEnumerable<string> GetPossibleKeys(string key)
        {
            var defaultKey = GetDefaultLookupKey(key);

            var appHost = HostContext.AppHost;

            if (appHost == null)
                return new[] { defaultKey };

            var serviceName = appHost.ServiceName;
            var serviceKey = $"ss/{key}/{serviceName}";

            if (appHost.Config == null)
                return new[] { serviceKey, defaultKey };

            var version = appHost.Config.ApiVersion;

            // TODO Instance specific key
            return new[]
            {
                $"{serviceKey}/{version}", // version specific (ss/keyname/service/v1)
                serviceKey, // service specific (ss/keyname/service)
                defaultKey // default (ss/keyname)
            };
        }
    }
}
