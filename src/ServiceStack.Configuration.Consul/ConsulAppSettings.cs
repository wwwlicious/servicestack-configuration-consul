// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Runtime.Serialization;
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
        private readonly KeySpecificity setSpecificity;

        /// <summary>
        /// Instantiates new ConsulAppSettings object using consul agent at specified URI.
        /// </summary>
        /// <param name="consulUri">The URI of the Consul agent</param>
        /// <param name="setSpecificity">Logic used when using Set command</param>
        public ConsulAppSettings(string consulUri, KeySpecificity setSpecificity = KeySpecificity.Instance)
        {
            consulUri.ThrowIfNullOrEmpty(nameof(consulUri));

            this.consulUri = consulUri;
            keyValueEndpoint = consulUri.CombineWith("/v1/kv/");

            this.setSpecificity = setSpecificity;
        }

        /// <summary>
        /// Instantiates new ConsulAppSettings object using local consul agent (http://127.0.0.1:8500)
        /// </summary>
        /// <param name="setLevel">Logic used when using Set command</param>
        public ConsulAppSettings(KeySpecificity setLevel = KeySpecificity.Instance)
            : this("http://127.0.0.1:8500", setLevel)
        {
        }

        public virtual Dictionary<string, string> GetAll()
        {
            // GetAll is a call with a null key as all keys live under known subfolder (ss)
            var values = GetKeyValuesFromConsul(null);

            return values.IsSuccess ? values.Value.ToDictionary(k => k.Key, v => v.GetValue<object>().ToString()) : null;
        }

        public virtual List<string> GetAllKeys()
        {
            // GET ?keys []
            try
            {
                return $"{keyValueEndpoint}{KeyUtilities.Prefix}?keys".GetJsonFromUrl().FromJson<List<string>>();
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
            return result.IsSuccess;
        }

        public virtual void Set<T>(string key, T value)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            var setKey = KeyUtilities.GetKeyForSpecificity(key, setSpecificity);
            log.Debug($"Key {setKey} will be used for setting value for provided key: {key}");

            var keyVal = KeyValue.Create(setKey, value);
            string result;
            try
            {
                result = consulUri.CombineWith(keyVal.ToPutUrl()).PutJsonToUrl(keyVal.Value);
            }
            catch (Exception ex)
            {
                var message = $"Error setting value {value} to configuration {key}. SetKey: {setKey}";
                log.Error(message, ex);
                throw new ConfigurationErrorsException($"Error setting configuration key {setKey}", ex);
            }

            // Consul returns true|false to signify success
            bool success;
            if (!bool.TryParse(result, out success) || !success)
            {
                log.Warn($"Error setting value {value} to configuration {key}. SetKey: {setKey}");
                throw new ConfigurationErrorsException($"Error setting configuration key {setKey}");
            }
        }

        public virtual string GetString(string name)
        {
            return GetFromConsul<string>(name, null).Value;
        }

        public virtual IList<string> GetList(string key)
        {
            return GetFromConsul<List<string>>(key, null).Value;
        }

        public virtual IDictionary<string, string> GetDictionary(string key)
        {
            return GetFromConsul<Dictionary<string, string>>(key, null).Value;
        }

        public virtual T Get<T>(string name)
        {
            return GetFromConsul(name, default(T)).Value;
        }

        public virtual T Get<T>(string name, T defaultValue)
        {
            return GetFromConsul(name, defaultValue).Value;
        }

        protected Result<T> GetFromConsul<T>(string name, T defaultValue)
        {
            name.ThrowIfNullOrEmpty(nameof(name));

            var result = GetValue<T>(name);

            log.Debug(result.IsSuccess
                          ? $"Got config value {result} for key {name}"
                          : $"Could not get value for key {name}");

            return result.IsSuccess ? result : Result<T>.Fail(defaultValue);
        }

        private Result<T> GetValue<T>(string name)
        {
            var resultValues = GetKeyValuesFromConsul(name);

            if (!resultValues.IsSuccess) return Result<T>.Fail();

            var kv = KeyUtilities.GetMostSpecificMatch(resultValues.Value, name);
            try
            {
                return Result<T>.Success(kv.GetValue<T>());
            }
            catch (NotSupportedException ex)
            {
                log.Error($"Unable to deserialise config value with key {name}", ex);
                return Result<T>.Fail();
            }
            catch (SerializationException ex)
            {
                log.Error($"Unable to deserialise config value with key {name}", ex);
                return Result<T>.Fail();
            }
        }

        private Result<List<KeyValue>> GetKeyValuesFromConsul(string name)
        {
            // Default lookup key is actual key preceded by default folder prefix
            var key = KeyUtilities.GetDefaultLookupKey(name);

            try
            {
                var keyVal = KeyValue.Create(key);

                // Get the URL to call. Use ?recurse to get any potential matches
                var url = $"{consulUri.CombineWith(keyVal.ToGetUrl())}?recurse";

                log.Debug($"Calling {url} to get values");

                var result = url.SendStringToUrl("GET", accept: "application/json");

                // Consul KV always returns a collection
                var keyValues = result.FromJson<List<KeyValue>>();

                return keyValues.Count == 0 ? Result<List<KeyValue>>.Fail() : Result<List<KeyValue>>.Success(keyValues);
            }
            catch (Exception ex)
            {
                log.Error($"Error getting config value with key {key}", ex);
                return Result<List<KeyValue>>.Fail();
            }
        }
    }
}
