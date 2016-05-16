// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Configuration;
    using DTO;
    using Logging;

    /// <summary>
    /// Implementation of IAppSettings using Consul K/V as backing store
    /// </summary>
    public class ConsulAppSettings : IAppSettings
    {
        private readonly string keyValueEndpoint;
        private readonly string consulUri;
        private readonly ILog log = LogManager.GetLogger(typeof(ConsulAppSettings));

        public ConsulAppSettings(string consulUri)
        {
            consulUri.ThrowIfNullOrEmpty(nameof(consulUri));
            
            this.consulUri = consulUri;
            keyValueEndpoint = consulUri.CombineWith("/v1/kv/");
        }

        /// <summary>
        /// Instantiates new ConsulAppSettings object using local consul agent (http://127.0.0.1:8500)
        /// </summary>
        public ConsulAppSettings() : this("http://127.0.0.1:8500")
        {
        }

        public virtual Dictionary<string, string> GetAll()
        {
            var allkeys = GetAllKeys();

            var all = new Dictionary<string, string>();
            foreach (var key in allkeys)
            {
                var value = Get<object>(key);
                if (value != null)
                {
                    all.Add(key, value.ToString());
                }
            }

            return all;
        }

        public virtual List<string> GetAllKeys()
        {
            // GET ?keys []
            try
            {
                return $"{keyValueEndpoint}?keys".GetJsonFromUrl().FromJson<List<string>>();
            }
            catch (Exception ex)
            {
                log.Error("Error getting all keys from Consul", ex);
                return null;
            }
        }

        public virtual bool Exists(string key)
        {
            var result = GetFromConsul<string>(key, null);
            return result != null;
        }

        public virtual void Set<T>(string key, T value)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            var keyVal = KeyValue.Create(key, value);
            string result;
            try
            {
                result = consulUri.CombineWith(keyVal.ToPutUrl()).PutJsonToUrl(keyVal.Value);
            }
            catch (Exception ex)
            {
                var message = $"Error setting value {value} to configuration {key}";
                log.Error(message, ex);
                throw new ConfigurationErrorsException($"Error setting configuration key {key}", ex);
            }

            // Consul returns true|false to signify success
            bool success;
            if (!bool.TryParse(result, out success) || !success)
            {
                log.Warn($"Error setting value {value} to configuration {key}");
                throw new ConfigurationErrorsException($"Error setting configuration key {key}");
            }
        }

        public virtual string GetString(string name)
        {
            return GetFromConsul<string>(name, null);
        }

        public virtual IList<string> GetList(string key)
        {
            return GetFromConsul<List<string>>(key, null);
        }

        public virtual IDictionary<string, string> GetDictionary(string key)
        {
            return GetFromConsul<Dictionary<string, string>>(key, null);
        }

        public virtual T Get<T>(string name)
        {
            return GetFromConsul(name, default(T));
        }

        public virtual T Get<T>(string name, T defaultValue)
        {
            return GetFromConsul(name, defaultValue);
        }

        protected T GetFromConsul<T>(string name, T defaultValue)
        {
            name.ThrowIfNullOrEmpty(nameof(name));

            try
            {
                var keyValues = GetKeyValue(name);
                var value = keyValues.GetValue<T>();

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Got config value {value} for key {name}");
                }

                return value;
            }
            catch (WebException ex) when (ex.ToStatusCode() == 404)
            {
                log.Error($"Unable to find config value with key {name}", ex);
                return defaultValue;
            }
            catch (NotSupportedException ex)
            {
                log.Error($"Unable to deserialise config value with key {name}", ex);
                return defaultValue;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting string value for config key {name}", ex);
                return defaultValue;
            }
        }

        private KeyValue GetKeyValue(string name)
        {
            var keyVal = KeyValue.Create(name);
            var jsonFromUrl = consulUri.CombineWith(keyVal.ToGetUrl()).GetJsonFromUrl();
            var keyValues = jsonFromUrl.FromJson<List<KeyValue>>();
            var value = keyValues.First();
            return value;
        }
    }
}
