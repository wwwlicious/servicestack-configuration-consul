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

There are 2 implementations of `IAppSettings`: `ConsulAppSetting` and `CachedConsulAppSetting`. These are setup like any other implementation of AppSettings. To set either as the default `IAppSettings` implementation for an AppHost add the following line while configuring an AppHost:

```csharp
public override void Configure(Container container)
{
    // ..standard setup... 
	
	AppSettings = new CachedConsulAppSettings();
	// OR
    AppSettings = new ConsulAppSettings();
}
```
Both `CachedConsulAppSettings` and `ConsulAppSettings` work as part of a cascading configuration setup using `MultiAppSettings`. The following will check Consul first, then local appSetting (app.config/web.config) before finally checking Environment variables.

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

## Overview
The recommendation is to use `CachedConsulAppSetting` as this provides some protection against spikes in traffic by caching responses for a short period of time.

### `ConsulAppSetting`
`ConsulAppSetting` is a basic implementation of `IAppSettings` that makes calls directly to Consul K/V store on every request. The URL of the consul instance to use can be specified as an optional constructor argument.

### `CachedConsulAppSetting`
`CachedConsulAppSetting` is a thin wrapper around `ConsulAppSetting` that caches all fetched requests for 2000ms (by default. The time, in ms, can be specified as a constructor argument). If a repeat call is made for the same key within the caching time the result will be served from the cache rather than the K/V store.

Calls to Consul are made via a loopback address so will be quick, however caching results avoids the potential to overload Consul by making too many requests in a short period of time.

The default `ICacheClient` implementation used is `MemoryCacheClient`. A different implementation of `ICacheClient` can be specified using the `WithCacheClient(ICacheClient cacheClient)` method, however the recommendation is to use a local memory cache. The goal of the `CachedConsulAppSetting` is to avoid many repeated loopback http requests in a small period of time so there is little to gain in replacing these calls with many requests to a remote caching solution.

```charp
// Cache results for 5000ms in new instance of MyCacheClient.
AppSettings = new CachedConsulAppSettings(5000).WithCacheClient(new MyCacheClient());
```

### Multi Level Keys
Consul K/V store supports the concept of folders. Any element of a Key that precedes a '/' is treated as a folder. This allows the same key to be represented at different levels of specificity. This plugin looks at the following levels (from most to least specific):

* ss/{key}/{servicename}/{version} (version specific)
* ss/{key}/{servicename} (service specific)
* ss/{key} (default key)

This would allow an appSetting with key "cacheTimeout" to differ for a specific version of a service, differ per service or have a default value for all services. The both implementations of `ConsulAppSettings` will transparently try and find the most specific match following the above pattern.

#### Key Makeup
In the above example the fields used to check for different keys are as follows:

* ss/ - this is a default folder to separate all values used by ServiceStack.
* {key} - the key supplied to the `IAppSetting` method.
* {servicename} - `AppHost.ServiceName`
* {version} - 'AppHost.Config.ApiVersion`


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
