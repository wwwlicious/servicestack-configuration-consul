// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Demo
{
    using System;
    using System.Diagnostics;
    using Funq;
    using Text;

    class Program
    {
        static void Main(string[] args)
        {
            var serviceUrl = "http://127.0.0.1:8093/";
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
            SetConfig(new HostConfig { WebHostUrl = serviceUrl });

            AppSettings = new ConsulAppSettings();

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
            if (!string.IsNullOrEmpty(result))
                return result;

            throw HttpError.NotFound($"Could not find config value with key {key.Key}");
        }
        
        public object Put(KeyRequest key)
        {
            AppSettings.Set(key.Key, key.Body);
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
