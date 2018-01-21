// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;
    using Caching;

    /// <summary>
    /// Implementation of IAppSettings using Consul K/V as backing store which caches Get requests
    /// </summary>
    public class CachedAppSettings : IAppSettings
    {
        private const string AllKeys = "__allKeys";
        private const string AllValues = "__all";
        private const int DefaultTtl = 2000;

        private TimeSpan ttl;

        private ICacheClient cacheClientValue;
        private readonly IAppSettings wrappedAppSetting;

        private ICacheClient CacheClient
        {
            get => cacheClientValue ?? (cacheClientValue = new MemoryCacheClient());
            set => cacheClientValue = value;
        }

        /// <summary>
        /// A cache wrapper around IAppSettings
        /// </summary>
        /// <param name="appSettings">the appsettings object</param>
        /// <param name="cacheTtl">the cache time to live, defaults to 2 seconds</param>
        public CachedAppSettings(IAppSettings appSettings, int cacheTtl = DefaultTtl)
        {
            appSettings.ThrowIfNull(nameof(appSettings));
            wrappedAppSetting = appSettings;
            ttl = TimeSpan.FromMilliseconds(cacheTtl);
        }

        /// <summary>
        /// Sets the cache client to use for the appsettings
        /// </summary>
        /// <param name="cacheClient">the cached client</param>
        /// <returns>the cached appsettings</returns>
        public CachedAppSettings WithCacheClient(ICacheClient cacheClient)
        {
            cacheClient.ThrowIfNull(nameof(cacheClient));
            CacheClient = cacheClient;
            return this;
        }

        /// <summary>
        /// gets all cached app settings
        /// </summary>
        /// <returns>a dictionary of app settings</returns>
        public Dictionary<string, string> GetAll()
            => TryGetCached(AllValues, wrappedAppSetting.GetAll);

        /// <summary>
        /// get all cached app setting keys
        /// </summary>
        /// <returns>a list of app setting keys</returns>
        public List<string> GetAllKeys()
            => TryGetCached(AllKeys, wrappedAppSetting.GetAllKeys);

        /// <summary>
        /// Checks is a key exists in the cached app settings
        /// </summary>
        /// <param name="key">the key name</param>
        /// <returns>true if the key exists, otherwise false</returns>
        public bool Exists(string key)
        {
            var checkKey = $"{key}_exists";
            key.ThrowIfNullOrEmpty(nameof(key));

            var value = CacheClient.Get<bool?>(checkKey);
            if (value.HasValue)
                return value.Value;

            var fromWrapped = wrappedAppSetting.Exists(key);
            CacheClient.Add<bool?>(checkKey, fromWrapped, ttl);

            return fromWrapped;
        }

        /// <summary>
        /// Gets a cached app setting by key
        /// </summary>
        /// <param name="name">the key name</param>
        /// <typeparam name="T">the app setting value type</typeparam>
        /// <returns>the app setting value</returns>
        public T Get<T>(string name)
            => TryGetCached(name, () => wrappedAppSetting.Get<T>(name));

        /// <summary>
        /// Gets a cached app setting by key with a default is not found
        /// </summary>
        /// <param name="name">the key name</param>
        /// <param name="defaultValue">the default value</param>
        /// <typeparam name="T">the app setting value type</typeparam>
        /// <returns></returns>
        public T Get<T>(string name, T defaultValue)
            => TryGetCached(name, () => wrappedAppSetting.Get(name, defaultValue));

        /// <summary>
        /// Gets a cached app setting dictionary by key
        /// </summary>
        /// <param name="key">the key name</param>
        /// <returns>a string dictionary</returns>
        public IDictionary<string, string> GetDictionary(string key)
            => TryGetCached(key, () => wrappedAppSetting.GetDictionary(key));

        /// <summary>
        /// Gets a cached app setting list by key
        /// </summary>
        /// <param name="key">the key name</param>
        /// <returns>a string list</returns>
        public IList<string> GetList(string key)
            => TryGetCached(key, () => wrappedAppSetting.GetList(key));

        /// <summary>
        /// Gets a cached app setting string value by key
        /// </summary>
        /// <param name="name">the key name</param>
        /// <returns>the string value</returns>
        public string GetString(string name)
            => TryGetCached(name, () => wrappedAppSetting.GetString(name));

        /// <summary>
        /// Sets a cached app setting
        /// </summary>
        /// <param name="key">the app setting key</param>
        /// <param name="value">the app setting value</param>
        /// <typeparam name="T">the value type</typeparam>
        public void Set<T>(string key, T value)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            // Add to underlying setting store
            wrappedAppSetting.Set(key, value);

            // Add to memory cache. 
            CacheClient.Set(key, value, ttl);

            // Clear down all keys + values as changed
            CacheClient.Remove(AllKeys);
            CacheClient.Remove(AllValues);
        }

        private T TryGetCached<T>(string key, Func<T> getFromWrapped)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            var value = CacheClient.Get<T>(key);
            if (value != null)
                return value;

            value = getFromWrapped();

            if (value != null)
                CacheClient.Add(key, value, ttl);

            return value;
        }
    }
}
