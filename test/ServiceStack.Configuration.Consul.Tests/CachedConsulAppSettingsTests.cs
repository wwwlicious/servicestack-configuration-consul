// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Net;
    using System.Text;
    using Caching;
    using FakeItEasy;
    using FluentAssertions;
    using Text;
    using Xunit;

    public class CachedConsulAppSettingsTests : AppSettingTestsBase
    {
        private const string AllKeys = "__allKeys";
        private const string All = "__all";
        private readonly CachedConsulAppSettings appSettings;
        private readonly ICacheClient cacheClient;
        private readonly TimeSpan defaultTtl;

        public CachedConsulAppSettingsTests()
        {
            cacheClient = A.Fake<ICacheClient>();
            appSettings =
                new CachedConsulAppSettings().WithCacheClient(cacheClient);
            defaultTtl = TimeSpan.FromMilliseconds(1500);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string uri)
        {
            Action action = () => new CachedConsulAppSettings(uri);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void WithCacheClient_ThrowsArgumentNullException_IfNullCacheClient()
        {
            var appSettings = new CachedConsulAppSettings();
            Action action = () => appSettings.WithCacheClient(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void WithCacheClient_ReturnsAppSettings() =>
            appSettings.WithCacheClient(new MemoryCacheClient()).Should().Be(appSettings);

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
        public void GetAllKeys_CallsEndpoint_IfNotInCache()
        {
            HttpWebRequest webRequest = null;
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    webRequest = request;
                    return "";
                }
            })
            {
                appSettings.GetAllKeys();

                var expected = new Uri($"{DefaultUrl}ss/?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void GetAllKeys_CachesKeysFromConsul_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);
            const string keysJson = "[ \"mates\", \"place\"]";
            List<string> result;

            using (GetStandardHttpResultsFilter(keysJson))
            {
                result = appSettings.GetAllKeys();

                result.Count.Should().Be(2);
                result[0].Should().Be("mates");
                result[1].Should().Be("place");
            }

            A.CallTo(() => cacheClient.Add(AllKeys, result, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetAllKeys_ReturnsKeysFromConsul_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);
            const string keysJson = "[ \"mates\", \"place\"]";

            using (GetStandardHttpResultsFilter(keysJson))
            {
                var result = appSettings.GetAllKeys();

                result.Count.Should().Be(2);
                result[0].Should().Be("mates");
                result[1].Should().Be("place");
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsNull_OnError()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);

            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetAllKeys().Should().BeNull();
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Exists_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string name)
        {
            Action action = () => appSettings.Exists(name);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Exists_ReturnsTrue_IfFoundInCache() => appSettings.Exists(SampleKey).Should().BeTrue();

        [Fact]
        public void Exists_CallsGetEndpoint_IfNotInCache()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            VerifyEndpoint(() => appSettings.Exists(SampleKey));
        }

        [Fact]
        public void Exists_ReturnsTrue_IfKeyFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeTrue();
            }
        }

        [Fact]
        public void Exists_AddsToCache_IfKeyFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeTrue();
            }

            A.CallTo(() => cacheClient.Add<object>(SampleKey, SampleString, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Exists_ReturnsFalse_IfKeyNotFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeFalse();
            }
        }

        [Fact]
        public void Exists_DoesNotAddToCache_IfKeyNotFound()
        {
            A.CallTo(() => cacheClient.Get<object>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeFalse();
            }

            A.CallTo(() => cacheClient.Add(SampleKey, ConsulResultString, defaultTtl)).MustNotHaveHappened();
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
        public void GetString_CallsGetEndpoint()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            VerifyEndpoint(() => appSettings.GetString(SampleKey));
        }

        [Fact]
        public void GetString_ReturnsString_IfFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter())
            {
                appSettings.GetString(SampleKey).Should().Be(SampleString);
            }
        }

        [Fact]
        public void GetString_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter())
            {
                appSettings.GetString(SampleKey);
            }

            A.CallTo(() => cacheClient.Add(SampleKey, SampleString, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetString_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetString(SampleKey).Should().BeNull();
            }

            A.CallTo(() => cacheClient.Add(SampleKey, SampleString, defaultTtl)).MustNotHaveHappened();
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
        public void Get_CallsGetEndpoint()
        {
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(null);
            VerifyEndpoint(() => appSettings.Get<string>(SampleKey));
        }
        
        [Fact]
        public void Get_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                var human = appSettings.Get<Human>(SampleKey);
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }
        }

        [Fact]
        public void Get_AddsToCache_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            Human human;
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                human = appSettings.Get<Human>(SampleKey);
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }

            A.CallTo(() => cacheClient.Add<Human>(SampleKey, human, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Get_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Get<Human>(SampleKey).Should().BeNull();
            }
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
            const string value = "Sosos";
            A.CallTo(() => cacheClient.Get<string>(SampleKey)).Returns(value);
            appSettings.Get(SampleKey, "Boo!").Should().Be(value);
        }

        [Fact]
        public void GetWithFallback_CallsGetEndpoint()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            VerifyEndpoint(() => appSettings.Get(SampleKey, new Human()));
        }

        [Fact]
        public void GetWithFallback_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                var human = appSettings.Get(SampleKey, new Human());
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }
        }

        [Fact]
        public void GetWithFallback_CachesValue_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            Human human;
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                human = appSettings.Get(SampleKey, new Human());
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }

            A.CallTo(() => cacheClient.Add(SampleKey, human, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetWithFallback_ReturnsFallback_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<Human>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                var human = new Human { Age = 200, Name = "Yoda" };
                var result = appSettings.Get(SampleKey, human);

                result.Should().Be(human);
            }
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
            var dict = new Dictionary<string, string>
            {
                { "One", "ValOne" },
                { "Two", "ValTwo" }
            };

            A.CallTo(() => cacheClient.Get<Dictionary<string,string>>(SampleKey)).Returns(dict);

            appSettings.GetDictionary(SampleKey).ShouldBeEquivalentTo(dict);
        }

        [Fact]
        public void GetDictionary_CallsGetEndpoint()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(SampleKey)).Returns(null);
            string dictResult;
            GenerateDictionaryResponse(out dictResult);

            VerifyEndpoint(() => appSettings.GetDictionary(SampleKey), result: dictResult);
        }

        [Fact]
        public void GetDictionary_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(SampleKey)).Returns(null);
            string dictResult;
            Dictionary<string, string> dict = GenerateDictionaryResponse(out dictResult);

            using (GetStandardHttpResultsFilter(dictResult))
            {
                var result = appSettings.GetDictionary(SampleKey);
                result.ShouldBeEquivalentTo(dict);
            }
        }

        [Fact]
        public void GetDictionary_CachesValue_IfFound()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(SampleKey)).Returns(null);
            string dictResult;
            Dictionary<string, string> dict = GenerateDictionaryResponse(out dictResult);

            IDictionary<string, string> result;
            using (GetStandardHttpResultsFilter(dictResult))
            {
                result = appSettings.GetDictionary(SampleKey);
                result.ShouldBeEquivalentTo(dict);
            }

            A.CallTo(() => cacheClient.Add(SampleKey, result as Dictionary<string,string>, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void GetDictionary_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetDictionary(SampleKey).Should().BeNull();
            }
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
            var list = new List<string> { "All", "The", "Wine" };

            A.CallTo(() => cacheClient.Get<List<string>>(SampleKey)).Returns(list);

            appSettings.GetList(SampleKey).ShouldBeEquivalentTo(list);
        }


        [Fact]
        public void GetList_CallsGetEndpoint()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(SampleKey)).Returns(null);
            VerifyEndpoint(() => appSettings.GetList(SampleKey));
        }

        [Fact]
        public void GetList_Returns_IfFound()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(SampleKey)).Returns(null);
            var list = new List<string> { "Rolles", "Rickson", "Royler", "Royce" };

            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(list)));
            string dictResult = $"[{{\"Key\":\"ss/Key1212\",\"Value\":\"{base64String}\"}}]";

            using (GetStandardHttpResultsFilter(dictResult))
            {
                var result = appSettings.GetList(SampleKey);
                result.ShouldBeEquivalentTo(list);
            }
        }

        [Fact]
        public void GetList_ReturnsNull_IfNotFound()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(SampleKey)).Returns(null);
            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetList(SampleKey).Should().BeNull();
            }
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
        public void Set_CallsSetEndpoint()
        {
            VerifyEndpoint(() => appSettings.Set("ss/" + SampleKey, 12345), "PUT", "true");
        }

        [Fact]
        public void Set_CallsGetEndpoint_WithSlashes()
        {
            VerifyEndpoint(() => appSettings.Set("ss/" + SlashKey, 22), "PUT", "true", SlashKey);
        }

        [Fact]
        public void Set_AddsToCache()
        {
            appSettings.Set(SampleKey, 22);
            A.CallTo(() => cacheClient.Set(SampleKey, 22, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Set_RemovesAllKeysCache()
        {
            appSettings.Set(SampleKey, 22);
            A.CallTo(() => cacheClient.Remove(AllKeys)).MustHaveHappened();
        }

        [Fact]
        public void Set_RemovesAllCache()
        {
            appSettings.Set(SampleKey, 22);
            A.CallTo(() => cacheClient.Remove(All)).MustHaveHappened();
        }

        [Fact]
        public void Set_DoesNotThrow_IfAdded()
        {
            HttpWebRequest webRequest = null;
            var human = new Human { Age = 2, Name = "Toddler" };

            using (GetStandardHttpResultsFilter("true"))
            {
                appSettings.Set(SampleKey, human);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("false")]
        [InlineData("")]
        public void Set_ThrowsException_IfNotAdded(string result)
        {
            HttpWebRequest webRequest = null;
            var human = new Human { Age = 2, Name = "Toddler" };

            using (GetStandardHttpResultsFilter(result))
            {
                Assert.Throws<ConfigurationErrorsException>(() => appSettings.Set(SampleKey, human));
            }
        }

        [Fact]
        public void GetAll_Returns_IfInCache()
        {
            var dictionary = new Dictionary<string, string> { { "test", "alligator" } };
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(dictionary);

            var actual = appSettings.GetAll();

            actual.ShouldBeEquivalentTo(dictionary);

        }

        [Fact]
        public void GetAll_GetsAllKeys()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);
            
            VerifyEndpoint(() => appSettings.GetAll(), key: null);
        }

        [Fact]
        public void GetAll_AddsToCache()
        {
            A.CallTo(() => cacheClient.Get<Dictionary<string, string>>(All)).Returns(null);

            string dictResponse;
            GenerateDictionaryResponse(out dictResponse);

            Dictionary<string, string> results;

            using (new HttpResultsFilter { StringResult = dictResponse })
                results = appSettings.GetAll();

            A.CallTo(() => cacheClient.Add(All, results, defaultTtl)).MustHaveHappened();
        }

        [Fact]
        public void Ctor_WithConsulUri_UsesUriForCalls()
        {
            A.CallTo(() => cacheClient.Get<List<string>>(AllKeys)).Returns(null);
            // NOTE Only testing 1 single call here
            HttpWebRequest webRequest = null;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    webRequest = request;
                    return ConsulResultString;
                }
            })
            {
                const string consulUri = "http://8.8.8.8:1212";
                new CachedConsulAppSettings(consulUri).GetAllKeys();

                var expected = new Uri($"{consulUri}/v1/kv/ss/?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void Ctor_WithTimeout_UsesTimeoutForCalls()
        {
            const int ttl = 9000;
            var settings = new CachedConsulAppSettings(ttl).WithCacheClient(cacheClient);
            settings.Set(SampleKey, 22);
            A.CallTo(() => cacheClient.Set(SampleKey, 22, TimeSpan.FromMilliseconds(ttl))).MustHaveHappened();
        }
    }
}
