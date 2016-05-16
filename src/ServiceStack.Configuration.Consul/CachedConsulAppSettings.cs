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
    public class CachedConsulAppSettings : ConsulAppSettings
    {
        private const string AllKeys = "__allKeys";
        private const string AllValues = "__all";
        private const int DefaultTtl = 1500;

        private TimeSpan ttl;

        private ICacheClient cacheClientValue;
        private ICacheClient CacheClient
        {
            get { return cacheClientValue ?? (cacheClientValue = new MemoryCacheClient()); }
            set { cacheClientValue = value; }
        }

        /// <summary>
        /// Instantiates new CachedConsulAppSettings with optionally specified timeout (default 1500ms) 
        /// and local consul agent (http://127.0.0.1:8500)
        /// </summary>
        /// <param name="cacheTtl">Cache time to live (ms)</param>
        public CachedConsulAppSettings(int cacheTtl = DefaultTtl)
        {
            Init(cacheTtl);
        }

        /// <summary>
        /// Instantiates new CachedConsulAppSettings specified timeout and consul Uri
        /// </summary>
        /// <param name="consulUri">Uri of consul agent to use</param>
        /// /// <param name="cacheTtl">Cache time to live (ms)</param>
        public CachedConsulAppSettings(string consulUri, int cacheTtl = DefaultTtl) : base(consulUri)
        {
            Init(cacheTtl);
        }

        public CachedConsulAppSettings WithCacheClient(ICacheClient cacheClient)
        {
            cacheClient.ThrowIfNull(nameof(cacheClient));
            CacheClient = cacheClient;
            return this;
        }

        public override Dictionary<string, string> GetAll()
        {
            var value = CacheClient.Get<Dictionary<string, string>>(AllValues);
            if (value != null)
                return value;

            value = base.GetAll();

            if (value != null)
                CacheClient.Add(AllValues, value, ttl);

            return value;
        }

        public override List<string> GetAllKeys()
        {
            var value = CacheClient.Get<List<string>>(AllKeys);
            if (value != null)
                return value;

            value = base.GetAllKeys();

            if (value != null)
                CacheClient.Add(AllKeys, value, ttl);

            return value;
        }

        public override bool Exists(string key)
        {
            key.ThrowIfNullOrEmpty(nameof(key));
            

            return Get<object>(key) != null;
        }

        public override T Get<T>(string name)
        {
            return Get(name, default(T));
        }

        public override T Get<T>(string name, T defaultValue)
        {
            name.ThrowIfNullOrEmpty(nameof(name));

            var value = CacheClient.Get<T>(name);
            if (value != null)
                return value;

            value = GetFromConsul(name, defaultValue);

            if (value != null)
                CacheClient.Add(name, value, ttl);

            return value;
        }

        public override IDictionary<string, string> GetDictionary(string key)
        {
            return Get<Dictionary<string, string>>(key, null);
        }

        public override IList<string> GetList(string key)
        {
            return Get<List<string>>(key, null);
        }

        public override string GetString(string name)
        {
            return Get<string>(name, null);
        }

        public override void Set<T>(string key, T value)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            // Add to underlying cache
            base.Set(key, value);

            // Add to memory cache. 
            CacheClient.Set(key, value, ttl);

            // Clear down all keys + values as changed
            CacheClient.Remove(AllKeys);
            CacheClient.Remove(AllValues);
        }

        private void Init(int cacheTtl)
        {
            ttl = TimeSpan.FromMilliseconds(cacheTtl);
        }
    }
}
