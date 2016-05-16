// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;

    public class KeyLookupUtilities
    {
        /// <summary>
        /// For specified key gets a list of all possible locations from most -> least specific
        /// </summary>
        /// <param name="key">Default key</param>
        /// <returns>List of keys where value may be found (most to least specific)</returns>
        public static IEnumerable<string> GetPossibleKeys(string key)
        {
            var defaultKey = $"ss/{key}";

            // TODO Null checks etc
            var appHost = HostContext.AppHost;

            if (appHost == null)
                return new[] { defaultKey };

            var serviceName = appHost.ServiceName;
            var serviceKey = $"ss/{serviceName}/{key}";

            if (appHost.Config == null)
                return new[] { serviceKey, defaultKey };

            // TODO Where do we get this?
            var serviceId = appHost.Config.ApiVersion;

            return new []
            {
                $"ss/{serviceName}/{serviceId}/{key}", // instance specific TODO version specific???
                serviceKey, // service specific
                defaultKey // default
            };
        }

        /// <summary>
        /// Looks up the most specific value for the given config key 
        /// (instance specific - service specific - default)
        /// </summary>
        /// <typeparam name="T">Type of key</typeparam>
        /// <param name="key">The base key to lookup</param>
        /// <param name="getValue">Function to call to get values</param>
        /// <returns>First non-default value for key</returns>
        public static Result<T> GetMostSpecificValue<T>(string key, Func<string, Result<T>> getValue)
        {
            foreach (var currentKey in GetPossibleKeys(key))
            {
                var value = getValue(currentKey);

                if (value.IsSuccess)
                    return value;
            }

            return Result<T>.Fail();
        }
    }
}
