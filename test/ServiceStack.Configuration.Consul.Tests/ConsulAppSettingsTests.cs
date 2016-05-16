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
    using FluentAssertions;
    using Testing;
    using Text;
    using Xunit;

    public class ConsulAppSettingsTests : AppSettingTestsBase
    {
        private readonly ConsulAppSettings appSettings;

        public ConsulAppSettingsTests()
        {
            // note basic lookup strategy makes testing easier
            appSettings = new ConsulAppSettings().WithLookupStrategy(LookupStrategy.BasicLookup);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string uri)
        {
            Action action = () => new ConsulAppSettings(uri);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetAllKeys_CallsCorrectEndpoint()
        {
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
                appSettings.GetAllKeys();

                var expected = new Uri($"{DefaultUrl}?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsNull_OnError()
        {
            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetAllKeys().Should().BeNull();
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsKeys()
        {
            const string keysJson = "[ \"mates\", \"place\"]";

            using (GetStandardHttpResultsFilter(keysJson))
            {
                var result = appSettings.GetAllKeys();

                result.Count.Should().Be(2);
                result[0].Should().Be("mates");
                result[1].Should().Be("place");
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
        public void Exists_CallsGetEndpoint()
        {
            VerifyEndpoint(() => appSettings.Exists(SampleKey));
        }

        [Fact]
        public void Exists_CallsGetEndpoint_WithSlashes()
        {
            const string key = "foo/bar";
            VerifyEndpoint(() => appSettings.Exists(key), key: key);
        }

        [Fact]
        public void Exists_ReturnsTrue_IfKeyFound()
        {
            using (GetStandardHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeTrue();
            }
        }

        [Fact]
        public void Exists_ReturnsFalse_IfKeyNotFound()
        {
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Exists(SampleKey).Should().BeFalse();
            }
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
        public void GetString_CallsGetEndpoint()
        {
            VerifyEndpoint(() => appSettings.GetString(SampleKey));
        }

        [Fact]
        public void GetString_CallsGetEndpoint_WithSlashes()
        {
            const string key = "foo/bar";
            VerifyEndpoint(() => appSettings.GetString(key), key: key);
        }

        [Fact]
        public void GetString_ReturnsString_IfFound()
        {
            using (GetStandardHttpResultsFilter())
            {
                appSettings.GetString(SampleKey).Should().Be(SampleString);
            }
        }

        [Fact]
        public void GetString_ReturnsNull_IfNotFound()
        {
            using (GetErrorHttpResultsFilter())
            {
                appSettings.GetString(SampleKey).Should().BeNull();
            }
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
        public void Get_CallsGetEndpoint()
        {
            VerifyEndpoint(() => appSettings.Get<string>(SampleKey));
        }

        [Fact]
        public void Get_Returns_IfFound()
        {
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                var human = appSettings.Get<Human>(SampleKey);
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }
        }

        [Fact]
        public void Get_ReturnsNull_IfNotFound_ReferenceType()
        {
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Get<Human>(SampleKey).Should().BeNull();
            }
        }

        [Fact]
        public void Get_ReturnsDefault_IfNotFound_ValueType()
        {
            using (GetErrorHttpResultsFilter())
            {
                appSettings.Get<int>(SampleKey).Should().Be(0);
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
        public void GetWithFallback_CallsGetEndpoint()
        {
            VerifyEndpoint(() => appSettings.Get(SampleKey, new Human()));
        }

        [Fact]
        public void GetWithFallback_CallsGetEndpointMultiple_IfUseBasicKeyLookupTrue()
        {
            var serviceName = "sylvia plath";
            if (ServiceStackHost.Instance == null)
            {
                var appHost = new BasicAppHost { TestMode = true, ServiceName = serviceName };
                appHost.Init();
            }

            appSettings.WithLookupStrategy(LookupStrategy.Fallthrough);

            var calls = new List<HttpWebRequest>();
            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    calls.Add(request);
                    return null;
                }
            })
            {
                appSettings.Get(SampleKey, new Human());

                calls.Count.Should().Be(3);
            }
        }

        [Fact]
        public void GetWithFallback_Returns_IfFound()
        {
            using (GetStandardHttpResultsFilter(ConsulResultComplex))
            {
                var human = appSettings.Get(SampleKey, new Human());
                human.Age.Should().Be(99);
                human.Name.Should().Be("Test Person");
            }
        }

        [Fact]
        public void GetWithFallback_ReturnsFallback_IfNotFound()
        {
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
        public void GetDictionary_CallsGetEndpoint()
        {
            string dictResult;
            GenerateDictionaryResponse(out dictResult);

            VerifyEndpoint(() => appSettings.GetDictionary(SampleKey), result: dictResult);
        }

        [Fact]
        public void GetDictionary_Returns_IfFound()
        {
            string dictResult;
            Dictionary<string, string> dict = GenerateDictionaryResponse(out dictResult);

            using (GetStandardHttpResultsFilter(dictResult))
            {
                var result = appSettings.GetDictionary(SampleKey);
                result.ShouldBeEquivalentTo(dict);
            }
        }

        [Fact]
        public void GetDictionary_ReturnsNull_IfNotFound()
        {
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
        public void GetList_CallsGetEndpoint()
        {
            VerifyEndpoint(() => appSettings.GetList(SampleKey));
        }

        [Fact]
        public void GetList_Returns_IfFound()
        {
            var list = new List<string> { "Rolles", "Rickson", "Royler", "Royce" };

            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(list)));
            string dictResult = $"[{{\"Key\":\"Key1212\",\"Value\":\"{base64String}\"}}]";

            using (GetStandardHttpResultsFilter(dictResult))
            {
                var result = appSettings.GetList(SampleKey);
                result.ShouldBeEquivalentTo(list);
            }
        }

        [Fact]
        public void GetList_ReturnsNull_IfNotFound()
        {
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
            VerifyEndpoint(() => appSettings.Set(SampleKey, 12345), "PUT", "true");
        }

        [Fact]
        public void Set_CallsGetEndpoint_WithSlashes()
        {
            const string key = "foo/bar";
            VerifyEndpoint(() => appSettings.Set(key, 22), "PUT", "true", key);
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
        public void GetAll_GetsAllKeys()
        {
            Uri firstUri = null;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    if (firstUri == null)
                        firstUri = request.RequestUri;
                    return ConsulResultString;
                }
            })
            {
                appSettings.GetAll();

                var expected = new Uri($"{DefaultUrl}?keys");

                firstUri.Should().Be(expected);
            }
        }

        [Fact]
        public void GetAll_CallsGet_ForEveryFoundKey()
        {
            const string keysJson = "[ \"mates\", \"place\"]";

            var callList = new List<Uri>();
            int count = 0;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    if (count++ > 0)
                    {
                        callList.Add(request.RequestUri);
                        return ConsulResultString;
                    }
                    return keysJson;
                }
            })
            {
                appSettings.GetAll();

                callList[0].Should().Be(new Uri($"{DefaultUrl}mates"));
                callList[1].Should().Be(new Uri($"{DefaultUrl}place"));
            }
        }

        [Fact]
        public void GetAll_ReturnsCorrectKeys()
        {
            const string keysJson = "[ \"mates\", \"place\"]";

            int count = 0;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    return count++ > 0 ? ConsulResultString : keysJson;
                }
            })
            {
                var results = appSettings.GetAll();

                results.Count.Should().Be(2);
                results.Should().ContainKeys("mates", "place");
            }
        }

        [Fact]
        public void Ctor_WithConsulUri_UsesUriForCalls()
        {
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
                new ConsulAppSettings(consulUri).GetAllKeys();

                var expected = new Uri($"{consulUri}/v1/kv/?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void WithUseBasicKeyLookup_ReturnsConsulAppSettings()
            =>
                appSettings.WithLookupStrategy(LookupStrategy.Fallthrough).Should()
                           .BeOfType<ConsulAppSettings>();
    }
}
