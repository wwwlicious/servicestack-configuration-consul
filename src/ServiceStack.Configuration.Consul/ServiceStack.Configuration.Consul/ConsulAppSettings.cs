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

    public class ConsulAppSettings : IAppSettings
    {
        private readonly string keyValueEndpoint;
        private readonly string consulUri;
        private readonly ILog log = LogManager.GetLogger(typeof(ConsulAppSettings));

        public ConsulAppSettings(string consulUri)
        {
            consulUri.ThrowIfNullOrEmpty("consulUri");
            
            if (consulUri.EndsWith("/"))
                consulUri = consulUri.Substring(0, consulUri.Length - 1);

            this.consulUri = consulUri;
            keyValueEndpoint = $"{consulUri}/v1/kv/";
        }

        // Default this to localhost/default port
        public ConsulAppSettings() : this("http://127.0.0.1:8500/")
        {
        }

        public Dictionary<string, string> GetAll()
        {
            var allkeys = GetAllKeys();

            var all = new Dictionary<string, string>();
            foreach (var key in allkeys)
            {
                all.Add(key, GetString(key));
            }

            return all;
        }

        public List<string> GetAllKeys()
        {
            // GET ?keys []
            try
            {
                return $"{keyValueEndpoint}?keys".GetJsonFromUrl().FromJson<List<string>>();
            }
            catch (Exception ex)
            {
                log.Error("Error getting all keys from Consul", ex);
                throw;
            }
        }

        public bool Exists(string key)
        {
            // 404 returned if not found
            var result = Get<string>(key, null);
            return result != null;
        }

        public void Set<T>(string key, T value)
        {
            // PUT. Throw if != true
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

            bool success;
            if (!bool.TryParse(result, out success) || !success)
            {
                log.Warn($"Error setting value {value} to configuration {key}");
                throw new ConfigurationErrorsException($"Error setting configuration key {key}");
            }
        }

        public string GetString(string name)
        {
            return Get<string>(name);
        }

        public IList<string> GetList(string key)
        {
            // GET /{name}
            return Get<List<string>>(key);
        }

        public IDictionary<string, string> GetDictionary(string key)
        {
            // NOTE Is this enough?
            return Get<Dictionary<string, string>>(key);
        }

        public T Get<T>(string name)
        {
            name.ThrowIfNullOrEmpty("name");

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
                throw new KeyNotFoundException($"Unable to find value with key {name}", ex);
            }
            catch (NotSupportedException ex)
            {
                log.Error($"Unable to deserialise config value with key {name}", ex);
                throw new KeyNotFoundException($"Unable to deserialise value with key {name}", ex);
            }
            catch (Exception ex)
            {
                log.Error($"Error getting string value for config key {name}", ex);
                throw new KeyNotFoundException($"Error getting value with key {name}", ex);
            }
        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                return Get<T>(name);
            }
            catch (KeyNotFoundException)
            {
                return defaultValue;
            }
        }

        // Catch with invalid type serialize?
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
