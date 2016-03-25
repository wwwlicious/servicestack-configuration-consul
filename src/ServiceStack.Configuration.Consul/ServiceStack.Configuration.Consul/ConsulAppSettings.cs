namespace ServiceStack.Configuration.Consul
{
    using System;
    using System.Collections.Generic;
    using Configuration;

    public class ConsulAppSettings : IAppSettings
    {
        private readonly string keyValueEndpoint;

        // TODO What's the best way to get a config value for a config provider?
        public ConsulAppSettings(string consulUri)
        {
            keyValueEndpoint = $"{consulUri}/v1/kv/";
        }

        public Dictionary<string, string> GetAll()
        {
            throw new NotImplementedException();
        }

        public List<string> GetAllKeys()
        {
            // GET ?keys []
            return $"{keyValueEndpoint}?keys".GetJsonFromUrl().FromJson<List<string>>();
        }

        public bool Exists(string key)
        {
            // 404 returned if not found
            throw new NotImplementedException();
        }

        public void Set<T>(string key, T value)
        {
            // PUT
            throw new NotImplementedException();
        }

        public string GetString(string name)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetList(string key)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string> GetDictionary(string key)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string name)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string name, T defaultValue)
        {
            throw new NotImplementedException();
        }
    }
}
