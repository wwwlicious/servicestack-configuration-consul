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
            get { return cacheClientValue ?? (cacheClientValue = new MemoryCacheClient()); }
            set { cacheClientValue = value; }
        }

        public CachedAppSettings(IAppSettings appSettings, int cacheTtl = DefaultTtl)
        {
            appSettings.ThrowIfNull(nameof(appSettings));
            wrappedAppSetting = appSettings;
            ttl = TimeSpan.FromMilliseconds(cacheTtl);
        }

        public CachedAppSettings WithCacheClient(ICacheClient cacheClient)
        {
            cacheClient.ThrowIfNull(nameof(cacheClient));
            CacheClient = cacheClient;
            return this;
        }

        public Dictionary<string, string> GetAll()
            => TryGetCached(AllValues, wrappedAppSetting.GetAll);

        public List<string> GetAllKeys()
            => TryGetCached(AllKeys, wrappedAppSetting.GetAllKeys);

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

        public T Get<T>(string name)
            => TryGetCached(name, () => wrappedAppSetting.Get<T>(name));

        public T Get<T>(string name, T defaultValue)
            => TryGetCached(name, () => wrappedAppSetting.Get(name, defaultValue));

        public IDictionary<string, string> GetDictionary(string key)
            => TryGetCached(key, () => wrappedAppSetting.GetDictionary(key));

        public IList<string> GetList(string key)
            => TryGetCached(key, () => wrappedAppSetting.GetList(key));

        public string GetString(string name)
            => TryGetCached(name, () => wrappedAppSetting.GetString(name));

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
