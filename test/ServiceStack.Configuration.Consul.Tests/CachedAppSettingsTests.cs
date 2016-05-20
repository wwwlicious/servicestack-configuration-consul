// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Caching;
    using FakeItEasy;
    using FluentAssertions;
    using Xunit;

    public class CachedAppSettingsTests
    {
        private const string AllKeys = "__allKeys";
        private const string All = "__all";
        private const string SampleKey = "keykoo";
        private const string SampleString = "I'm a little value";

        private readonly CachedAppSettings appSettings;
        private readonly ICacheClient cacheClient;
        private readonly IAppSettings internalAppSetting;
        private readonly TimeSpan defaultTtl;
        private readonly Human human;
        private readonly IDictionary<string, string> dict;
        private readonly IList<string> list;

        public CachedAppSettingsTests()
        {
            cacheClient = A.Fake<ICacheClient>();
            internalAppSetting = A.Fake<IAppSettings>();
            appSettings =
                new CachedAppSettings(internalAppSetting).WithCacheClient(cacheClient);
            defaultTtl = TimeSpan.FromMilliseconds(2000);
            human = new Human { Age = 99, Name = "Test Person" };

            dict = new Dictionary<string, string>
            {
                { "One", "ValOne" },
                { "Two", "ValTwo" }
            };
            list = new List<string> { "Rolles", "Rickson", "Royler", "Royce" };
        }

        [Fact]
        public void Ctor_ThrowsArgumentNullException_IfNullAppSetting()
        {
            Action action = () => new CachedAppSettings(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void WithCacheClient_ThrowsArgumentNullException_IfNullCacheClient()
        {
            var appSettings = new CachedAppSettings(internalAppSetting);
            Action action = () => appSettings.WithCacheClient(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void WithCacheClient_ReturnsAppSettings() =>
            appSettings.WithCacheClient(cacheClient).Should().Be(appSettings);

        [Fact]
        public void GetAllKeys_ChecksCacheForValue()
        {
            appSettings.GetAllKeys();
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).MustHaveHappened();
        }

        [Fact]
        public void GetAllKeys_ReturnsCachedValue_IfInCache()
        {
            var keys = new List<string> { "Foo", "Bar" };
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(keys);
            
            var actual = appSettings.GetAllKeys();
            actual.Should().BeEquivalentTo(keys);
        }

        [Fact]
        public void GetAllKeys_CallsUnderlying_IfNotInCache()
        {
            HttpWebRequest webRequest = null;
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);

            appSettings.GetAllKeys();
            A.CallTo(() => internalAppSetting.GetAllKeys()).MustHaveHappened();
        }

        [Fact]
        public void GetAllKeys_CachesKeysFromUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);
            var keys = new List<string> { "Spring", "Summery" };
            A.CallTo(() => internalAppSetting.GetAllKeys()).Returns(keys);
            
            appSettings.GetAllKeys();

            A.CallTo(() => cacheClient.Add(AllKeys, keys, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetAllKeys_ReturnsKeysUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);
            var keys = new List<string> { "Spring", "Summery" };
            A.CallTo(() => internalAppSetting.GetAllKeys()).Returns(keys);

            appSettings.GetAllKeys().Should().BeEquivalentTo(keys);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Exists_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.Exists(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Exists_ReturnsValueFromCache_IfFoundInCache(bool response)
        {
            var sampleKey = $"{SampleKey}_exists";
            A.CallTo(() => cacheClient.Get<bool?>(sampleKey)).Returns(response);
            appSettings.Exists(SampleKey).Should().Be(response);
        }

        [Fact]
        public void Exists_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<bool?>($"{SampleKey}_exists")).Returns(null);
            appSettings.Exists(SampleKey);
            A.CallTo(() => internalAppSetting.Exists(SampleKey)).MustHaveHappened();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Exists_ReturnsUnderlyingValue_IfNotInCache(bool response)
        {
            A.CallTo(() => cacheClient.Get<bool?>($"{SampleKey}_exists")).Returns(null);
            A.CallTo(() => internalAppSetting.Exists(SampleKey)).Returns(response);

            appSettings.Exists(SampleKey).Should().Be(response);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Exists_AddsToCache(bool response)
        {
            var sampleKey = $"{SampleKey}_exists";
            A.CallTo(() => cacheClient.Get<bool?>(sampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Exists(SampleKey)).Returns(response);

            appSettings.Exists(SampleKey);

            A.CallTo(() => cacheClient.Add<bool?>(sampleKey, response, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Exists_ReturnsFalse_IfKeyNotFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            appSettings.Exists(SampleKey).Should().BeFalse();
        }

        [Fact]
        public void Exists_DoesNotAddToCache_IfKeyNotFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);

            appSettings.Exists(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, A<string>.Ignored, defaultTtl)).MustNotHaveHappened();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetString_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.GetString(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetString_ReturnsValue_IfInCache()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(SampleString);
            appSettings.GetString(SampleKey).Should().Be(SampleString);
        }

        [Fact]
        public void GetString_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            appSettings.GetString(SampleKey);

            A.CallTo(() => internalAppSetting.GetString(SampleKey)).MustHaveHappened();
        }

        [Fact]
        public void GetString_ReturnsUnderlyingValue_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetString(SampleKey)).Returns(SampleString);
            
            var result = appSettings.GetString(SampleKey);
            result.Should().Be(SampleString);
        }

        [Fact]
        public void GetString_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetString(SampleKey)).Returns(SampleString);

            appSettings.GetString(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, SampleString, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetString_DoesNotAddToCache_IfKeyNotFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetString(SampleKey)).Returns(null);

            appSettings.GetString(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, SampleString, defaultTtl)).MustNotHaveHappened();
        }

        [Fact]
        public void GetString_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetString(SampleKey)).Returns(null);

            appSettings.GetString(SampleKey).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Get_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.Get<object>(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Get_ReturnsValue_IfInCache()
        {
            const int value = 1324;
            A.CallTo(() => cacheClient.Get<int>(SampleKey)).Returns(value);

            appSettings.Get<int>(SampleKey).Should().Be(value);
        }

        [Fact]
        public void Get_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);

            appSettings.Get<string>(SampleKey);

            A.CallTo(() => internalAppSetting.Get<string>(SampleKey)).MustHaveHappened();
        }
        
        [Fact]
        public void Get_ReturnsUnderlyingValue_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey)).Returns(human);

            var result = appSettings.Get<Human>(SampleKey);
            result.Should().Be(human);
        }

        [Fact]
        public void Get_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey)).Returns(human);

            appSettings.Get<Human>(SampleKey);
            
            A.CallTo(() => cacheClient.Add<Human>(SampleKey, human, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Get_DoesNotAddToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey)).Returns(null);

            appSettings.Get<Human>(SampleKey);

            A.CallTo(() => cacheClient.Add<Human>(SampleKey, A<Human>.Ignored, defaultTtl)).MustNotHaveHappened();
        }


        [Fact]
        public void Get_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey)).Returns(null);

            appSettings.Get<Human>(SampleKey).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetWithFallback_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.Get(name, 22);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetWithFallback_ReturnsValue_IfInCache()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(SampleString);

            appSettings.Get(SampleKey, "Boo!").Should().Be(SampleString);
        }

        [Fact]
        public void GetWithFallback_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);

            appSettings.Get(SampleKey, human);

            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey, human)).MustHaveHappened();
        }

        [Fact]
        public void GetWithFallback_Returns_IfFound()
        {
            var fallbackHuman = new Human { Age = 1, Name = "Fallback" };
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey, fallbackHuman)).Returns(human);

            var result = appSettings.Get(SampleKey, fallbackHuman);
            result.Should().Be(human);
        }

        [Fact]
        public void GetWithFallback_AddsToCache_IfFound()
        {
            var fallbackHuman = new Human { Age = 1, Name = "Fallback" };
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey, fallbackHuman)).Returns(human);

            appSettings.Get(SampleKey, fallbackHuman);

            A.CallTo(() => cacheClient.Add(SampleKey, human, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetWithFallback_DoesNotAddToCache_IfFound()
        {
            var fallbackHuman = new Human { Age = 1, Name = "Fallback" };
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.Get<Human>(SampleKey, fallbackHuman)).Returns(null);

            appSettings.Get(SampleKey, fallbackHuman);

            A.CallTo(() => cacheClient.Add(SampleKey, A<Human>.Ignored, defaultTtl)).MustNotHaveHappened();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetDictionary_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.GetDictionary(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetDictionary_ReturnsValue_IfInCache()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string,string>>(SampleKey)).Returns(dict);

            var result = appSettings.GetDictionary(SampleKey);
            result.ShouldBeEquivalentTo(dict);
        }

        [Fact]
        public void GetDictionary_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string, string>>(SampleKey)).Returns(null);

            appSettings.GetDictionary(SampleKey);

            A.CallTo(() => internalAppSetting.GetDictionary(SampleKey)).MustHaveHappened();
        }

        [Fact]
        public void GetDictionary_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string, string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetDictionary(SampleKey)).Returns(dict);

            var result = appSettings.GetDictionary(SampleKey);
            result.ShouldBeEquivalentTo(dict);
        }

        [Fact]
        public void GetDictionary_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string, string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetDictionary(SampleKey)).Returns(dict);

            appSettings.GetDictionary(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, dict, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetDictionary_DoesNotAddToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string, string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetDictionary(SampleKey)).Returns(dict);

            appSettings.GetDictionary(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, A<IDictionary<string, string>>.Ignored, defaultTtl))
             .MustHaveHappened();
        }

        [Fact]
        public void GetDictionary_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<IDictionary<string, string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetDictionary(SampleKey)).Returns(null);

            appSettings.GetDictionary(SampleKey).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetList_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.GetList(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetList_ReturnsValue_IfInCache()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(list);

            appSettings.GetList(SampleKey).ShouldBeEquivalentTo(list);
        }

        [Fact]
        public void GetList_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(null);
            appSettings.GetList(SampleKey);

            A.CallTo(() => internalAppSetting.GetList(SampleKey)).MustHaveHappened();
        }

        [Fact]
        public void GetList_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetList(SampleKey)).Returns(list);

            var result = appSettings.GetList(SampleKey);
            result.ShouldBeEquivalentTo(list);
        }

        [Fact]
        public void GetList_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetList(SampleKey)).Returns(list);

            appSettings.GetList(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, list, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetList_DoesNotAddToCache_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetList(SampleKey)).Returns(null);

            appSettings.GetList(SampleKey);

            A.CallTo(() => cacheClient.Add(SampleKey, A<IList<string>>.Ignored, defaultTtl)).MustNotHaveHappened();
        }

        [Fact]
        public void GetList_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<IList<string>>(SampleKey)).Returns(null);
            A.CallTo(() => internalAppSetting.GetList(SampleKey)).Returns(null);

            appSettings.GetList(SampleKey).Should().BeNull();
        }

        [Fact]
        public void GetAll_Returns_IfInCache()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(dict as Dictionary<string, string>);

            var actual = appSettings.GetAll();

            actual.ShouldBeEquivalentTo(dict);
        }

        [Fact]
        public void GetAll_CallsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);

            appSettings.GetAll();

            A.CallTo(() => internalAppSetting.GetAll()).MustHaveHappened();
        }

        [Fact]
        public void GetAll_ReturnsUnderlying_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);
            A.CallTo(() => internalAppSetting.GetAll()).Returns(dict as Dictionary<string, string>);

            var actual = appSettings.GetAll();

            actual.ShouldBeEquivalentTo(dict);
        }

        [Fact]
        public void GetAll_AddsToCache()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);
            A.CallTo(() => internalAppSetting.GetAll()).Returns(dict as Dictionary<string, string>);

            appSettings.GetAll();

            A.CallTo(() => cacheClient.Add(All, dict as Dictionary<string, string>, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetAll_DoesNotAddToCache_IfNull()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);
            A.CallTo(() => internalAppSetting.GetAll()).Returns(null);

            appSettings.GetAll();

            A.CallTo(() => cacheClient.Add(All, A<Dictionary<string, string>>.Ignored, defaultTtl)).MustNotHaveHappened();
        }

        [Fact]
        public void GetAll_ReturnsNull_IfNull()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);
            A.CallTo(() => internalAppSetting.GetAll()).Returns(null);

            appSettings.GetAll().Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Set_ThrowsArgumentNullException_IfNullOrEmptyNamePassed(string name)
        {
            Action action = () => appSettings.Set(name, 123);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Set_CallsUnderlying()
        {
            appSettings.Set(SampleKey, SampleString);

            A.CallTo(() => internalAppSetting.Set(SampleKey, SampleString)).MustHaveHappened();
        }

        [Fact]
        public void Set_AddsToCache()
        {
            appSettings.Set(SampleKey, SampleString);
            A.CallTo(() => cacheClient.Set(SampleKey, SampleString, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Set_RemovesAllKeysCache()
        {
            appSettings.Set(SampleKey, SampleString);
            A.CallTo(() => cacheClient.Remove(AllKeys)).MustHaveHappened();
        }

        [Fact]
        public void Set_RemovesAllCache()
        {
            appSettings.Set(SampleKey, SampleString);
            A.CallTo(() => cacheClient.Remove(All)).MustHaveHappened();
        }

        [Fact]
        public void Ctor_WithTimeout_UsesTimeoutForCalls()
        {
            const int ttl = 9000;
            var settings = new CachedAppSettings(internalAppSetting, ttl).WithCacheClient(cacheClient);
            settings.Set(SampleKey, 22);
            A.CallTo(() => cacheClient.Set(SampleKey, 22, TimeSpan.FromMilliseconds(ttl))).MustHaveHappened();
        }
    }
}
