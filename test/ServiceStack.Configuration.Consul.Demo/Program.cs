// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Demo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Extensions;
    using Funq;
    using Text;

    class Program
    {
        static void Main(string[] args)
        {
            var serviceUrl = "http://127.0.0.1:8093";
            try
            {
                new AppHost(serviceUrl).Init().Start("http://*:8093/");
                $"Start selfhost on {serviceUrl}".Print();
            }
            catch (Exception ex)
            {
                ex.Message.Print();
                ex.StackTrace.Print();
            }

            Process.Start(serviceUrl);
            Console.ReadLine();
        }
    }

    public class AppHost : AppSelfHostBase
    {
        private readonly string serviceUrl;

        public AppHost(string serviceUrl) : base("ConfigDemoService", typeof(MyService).Assembly)
        {
            this.serviceUrl = serviceUrl;
        }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig { WebHostUrl = serviceUrl, ApiVersion = "2.3", HandlerFactoryPath = "api" });

            AppSettings = new ConsulAppSettings(KeySpecificity.Global).WithCache(10000);

            // Uncomment the following line to use Multi cascading IAppSetting providers
            /*AppSettings = new MultiAppSettings(
                new ConsulAppSettings(),
                new AppSettings(), 
                new EnvironmentVariableSettings());*/
        }
    }

    public class MyService : Service
    {
        public IAppSettings AppSettings { get; set; }

        public object Get(KeyRequest key)
        {
            if (string.Equals(key.Key, "all", StringComparison.InvariantCultureIgnoreCase))
                return AppSettings.GetAllKeys();

            var result = AppSettings.GetString(key.Key);

            var recurse = AppSettings.GetString("testKey");

            var exists = AppSettings.Exists(key.Key); // True
            var existsno = AppSettings.Exists($"no{key.Key}"); // False

            var list = AppSettings.GetList($"{key.Key}List"); // default
            var listno = AppSettings.GetList($"no{key.Key}List"); // not found

            var dict = AppSettings.GetDictionary($"{key.Key}Dict"); // service specifc
            var dictno = AppSettings.GetDictionary($"no{key.Key}Dict"); // not found

            var str = AppSettings.GetString($"{key.Key}Str"); // version specific "version-specific"
            var strno = AppSettings.GetString($"no{key.Key}Str"); // not found

            var instance = AppSettings.GetString($"{key.Key}ii"); // version specific "instance-specific"

            var strLower = AppSettings.GetString($"{key.Key}Str/lower"); // default "extra"
            var strLowerno = AppSettings.GetString($"no{key.Key}Str/lower"); // not found

            var type = AppSettings.Get<KeyRequest>($"{key.Key}request"); // default
            var typeno = AppSettings.Get<KeyRequest>($"no{key.Key}request"); // not found
            var typedef = AppSettings.Get($"no{key.Key}request", new KeyRequest { Body = "Chirpy cheep", Key = "Fallback value" }); // not found, returns fallback

            throw HttpError.NotFound($"Could not find config value with key {key.Key}");
        }
        
        public object Put(KeyRequest key)
        {
            AppSettings.Set($"{key.Key}/ConfigDemoService", $"{key.Body} service");
            AppSettings.Set($"{key.Key}/ConfigDemoService/2.3", $"{key.Body} version");
            AppSettings.Set($"{key.Key}/ConfigDemoService/1.0", $"{key.Body} not found");

            AppSettings.Set($"{key.Key}List", new List<string> { "Ho", "He", "Ha" });
            AppSettings.Set($"{key.Key}Dict/ConfigDemoService", new Dictionary<string, string> { { "One", "V1" }, { "Two", "V2" } });
            AppSettings.Set($"{key.Key}Str", "Default string");
            AppSettings.Set($"{key.Key}Str/ConfigDemoService/2.3", "version-specific");
            AppSettings.Set($"{key.Key}Str/ConfigDemoService/i/127.0.0.1:8095", "version-specific");

            AppSettings.Set($"{key.Key}Str/lower", "extra");

            AppSettings.Set($"{key.Key}request", new KeyRequest { Body = "Chirp", Key = "Consul" });

            AppSettings.Set($"{key.Key}ii/ConfigDemoService/2.3", "version-specific");
            AppSettings.Set($"{key.Key}ii/ConfigDemoService/i/127.0.0.1:8093|api", "instance-specific");

            return key;
        }
    }

    [DebuggerDisplay("{Key} - {Body}")]
    [Route("/keys/{Key}")]
    public class KeyRequest : IReturn<object>
    {
        public string Key { get; set; }

        public object Body { get; set; }
    }
}
