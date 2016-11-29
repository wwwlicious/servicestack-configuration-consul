# ServiceStack.Configuration.Consul
[![Build status](https://ci.appveyor.com/api/projects/status/i3q9d4rymo00pcp8/branch/master?svg=true)](https://ci.appveyor.com/project/wwwlicious/servicestack-configuration-consul/branch/master)
[![NuGet version](https://badge.fury.io/nu/ServiceStack.Configuration.Consul.svg)](https://badge.fury.io/nu/ServiceStack.Configuration.Consul)

An implementation of [ServiceStack](https://servicestack.net/) [IAppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings) interface that uses [Consul.io key/value store](https://www.consul.io/docs/agent/http/kv.html) as backing storage.

## Requirements
An accessible running consul agent.

### Local Agent
To get consul.io running locally follow the [Install Consul](https://www.consul.io/intro/getting-started/install.html) guide on the main website. Once this has been installed you can run it with:

```bash
consul.exe agent -dev -advertise="127.0.0.1"
```

This will start Consul running and accessible on http://127.0.0.1:8500.  You should now be able to view the [Consul UI](http://127.0.0.1:8500/ui) to verify. This has a [Key/Value tab](http://127.0.0.1:8500/ui/#/dc1/kv/) where stored keys can be managed.


## Quick Start

Install the package [https://www.nuget.org/packages/ServiceStack.Configuration.Consul](https://www.nuget.org/packages/ServiceStack.Configuration.Consul/)
```bash
PM> Install-Package ServiceStack.Configuration.Consul
```

The `ConsulAppSetting` is setup like any other implementation of AppSettings. To set `ConsulAppSetting` as the default `IAppSettings` implementation for an AppHost add the following line while configuring an AppHost:

```csharp
public override void Configure(Container container)
{
    // ..standard setup... 
	AppSettings = new ConsulAppSettings();
}
```
`ConsulAppSettings` work as part of a cascading configuration setup using `MultiAppSettings`. The following will check Consul first, then local appSetting (app.config/web.config) before finally checking Environment variables.

```csharp
AppSettings = new MultiAppSettings(
    new ConsulAppSettings(),
    new AppSettings(), 
    new EnvironmentVariableSettings());
```

The `IAppSetting` instances can be auto-wired into services like all other dependencies:

```csharp
public class MyService : Service
{
    public IAppSettings AppSettings { get; set; }
	
	public object Get(KeyRequest key) { ... }
}
```

### Caching AppSettings
`CachedAppSettings` is a thin wrapper around `IAppSetting` that caches all fetched requests for 2000ms (by default. The time, in ms, can be specified as a constructor argument). If a repeat call is made for the same key within the caching time the result will be served from the cache rather than the K/V store.

```csharp
AppSettings = new ConsulAppSettings().WithCache();
```

Calls to Consul are made via a loopback address so will be quick, however caching results avoids the potential to overload Consul by making too many requests in a short period of time.

The default `ICacheClient` implementation used is `MemoryCacheClient`. A different implementation of `ICacheClient` can be specified using the `WithCacheClient(ICacheClient cacheClient)` method, however the recommendation is to use a local memory cache. The goal of the `CachedAppSettings` is to avoid many repeated loopback http requests in a small period of time so there is little to gain in replacing these calls with many requests to a remote caching solution.

Although this has been written with Consul in mind the only dependency the `CachedAppSettings` has is on `IAppSetting` and as such can be used to add caching to any implementation.

```csharp
// Cache Consul appSetting requests  for 5000ms in new instance of MyCacheClient.
AppSettings = new ConsulAppSettings.WithCache(5000).WithCacheClient(new MyCacheClient());
```

### Multi Level Keys
Consul K/V store supports the concept of folders. Any element of a Key that precedes a '/' is treated as a folder. This allows the same key to be represented at different levels of specificity. 

#### Get Operations
For `Get` operations this plugin looks at the following levels (from most to least specific):

| Name | Layout | Example |
| --- | --- | --- |
| instance specific | ss/{key}/{servicename}/i/{instance} | ss/myKey/productService/i/127.0.0.1:8095 |
| version specific | ss/{key}/{servicename}/{version} | ss/myKey/productService/1.2 |
| service specific | ss/{key}/{servicename} | ss/myKey/productService |
| default | ss/{key} | ss/myKey |

This would allow an appSetting with key "cacheTimeout" to differ for a specific version of a service, differ per service or have a default value for all services. `ConsulAppSettings` will transparently find the most specific match following the above pattern.

#### Set Operations
The constructor for the ConsulAppSettings takes an optional `KeySpecificity` parameter which controls which specificity level keys are set at. The possible values for `KeySpecificity` are:

| Value | Description | Example |
| --- | --- | --- |
| LiteralKey | no modifications are made when setting value, the specified key is used as-is | foo-bar -> foo-bar |
| Instance | any Set operations are made for this instance only. **default** | foo-bar -> ss/foo-bar/productService/i/127.0.0.1:8095 |
| Version | updates are made for all instances of this service with same version number | foo-bar -> ss/foo-bar/productService/1.0 |
| Service | updates made for all instance of this service | foo-bar -> ss/foo-bar/productService |
| Global | updates made for any instance of any service | foo-bar -> ss/foo-bar |

#### Key Makeup
In the above example the fields used to check for different keys are as follows:

* ss/ - this is a default folder to separate all values used by ServiceStack.
* {key} - the key supplied to the `IAppSetting` method.
* {servicename} - `AppHost.ServiceName`
* {version} - `AppHost.Config.ApiVersion`
* {instance} - `AppHost.Config.WebHostUrl` + "|" + `AppHost.Config.HandlerFactoryPath`

## Demo
ServiceStack.Configuration.Consul.Demo is a console app that starts a self hosted application that runs as [http://127.0.0.1:8093/](http://127.0.0.1:8093/). This contains a simple service that takes a GET and PUT request:

* GET http://127.0.0.1:8093/keys/all - get all key names
* GET http://127.0.0.1:8093/keys/{key} - get config value with specified key
* PUT http://127.0.0.1:8093/keys/{key}, body: {Body:testtest}, header: content-type=application/jsv - create new config value with specified name and content.

The "Postman Samples" folder contains a sample [Postman](https://www.getpostman.com/) collection containing sample calls. Use the "Import" function in Postman to import this collection, this contains sample PUT and GET requests that can be run against the demo service.

## Why?
When implementing distributed systems it makes life easier to decouple configuration from code and manage it as an external concern. This allows a central place for all shared configuration values which can then be access by a number of systems. It then becomes faster and easier to make configuration changes; an update is made once and can be used everywhere.

By managing configuration as an external concern the way in which it is consumed needs to slightly change. Rather than reading in all AppSettings on startup and caching them we now need to get AppSettings on demand, every time they are required as the value may have been updated since it was last called.

For example, if a range of systems need to use the same connection string this can be updated in the central Consul K/V store and is then available to all applications without needing to make any changes to them (e.g. redeploy or bouncing appPools etc):

![Configuration Management](assets/CentralConfiguration.png)
