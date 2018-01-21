// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Extensions
{
    /// <summary>
    /// Extension methofs for cached app settings
    /// </summary>
    public static class AppSettingsExtensions
    {
        /// <summary>
        /// Wrap IAppSettings in thin caching layer that caches all values for default 2000ms
        /// </summary>
        /// <param name="appSettings">IAppSettings implementatino to wrap</param>
        /// <returns>CachedAppSettings instance</returns>
        public static CachedAppSettings WithCache(this IAppSettings appSettings)
            => new CachedAppSettings(appSettings);

        /// <summary>
        /// Wrap IAppSettings in thin caching layer that caches all values for specified time
        /// </summary>
        /// <param name="appSettings">IAppSettings implementatino to wrap</param>
        /// <param name="timeoutms">Number of ms to cache values for</param>
        /// <returns>CachedAppSettings instance</returns>
        public static CachedAppSettings WithCache(this IAppSettings appSettings, int timeoutms)
            => new CachedAppSettings(appSettings, timeoutms);
    }
}