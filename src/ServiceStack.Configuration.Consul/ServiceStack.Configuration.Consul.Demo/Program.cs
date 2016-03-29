
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

            Container.Register<IAppSettings>(new ConsulAppSettings());
        }
    }

    public class MyService : Service
    {
        public IAppSettings AppSettings { get; set; }

        public object Get(KeyRequest key)
        {
            if (string.Equals(key.Key, "all", StringComparison.InvariantCultureIgnoreCase))
                return AppSettings.GetAllKeys();

            return AppSettings.GetString(key.Key);
        }
    }

    public class KeyRequest : IReturn<object>
    {
        public string Key { get; set; }
        public string Type { get; set; } = "string";
    }
}
