namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using FluentAssertions;
    using Text;
    using Xunit;

    public class ConsulAppSettingsTests
    {
        private readonly ConsulAppSettings appSettings;

        // NOTE This is a sample result that just returns "testString"
        private const string SampleString = "testString";
        private const string ConsulResultString = "[{\"Key\":\"Key1212\",\"Value\":\"dGVzdFN0cmluZw==\"}]";

        // NOTE This is a sample result 
        private const string ConsulResultComplex = "[{\"Key\":\"Key1212\",\"Value\":\"e0FnZTo5OSxOYW1lOlRlc3QgUGVyc29ufQ==\"}]";

        private const string DefaultUrl = "http://127.0.0.1:8500/v1/kv/";

        private const string SampleKey = "Key1212";

        public ConsulAppSettingsTests()
        {
            appSettings = new ConsulAppSettings();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_ThrowsArgumentNullException_IfNullOrEmptyStringPassed(string uri)
        {
            Assert.Throws<ArgumentNullException>(() => new ConsulAppSettings(uri));
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
        public void GetAllKeys_ReThrows_AnyErrors()
        {
            using (GetErrorHttpResultsFilter())
            {
                Assert.Throws<WebException>(() => appSettings.GetAllKeys());
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

        [Fact]
        public void Exists_CallsGetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.Exists(SampleKey));
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

        [Fact]
        public void GetString_CallsGetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.GetString(SampleKey));
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
        public void GetString_ThrowsKeyNotFoundException_IfNotFound()
        {
            using (GetErrorHttpResultsFilter())
            {
                Assert.Throws<KeyNotFoundException>(() => appSettings.GetString(SampleKey));
            }
        }

        [Fact]
        public void Get_CallsGetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.Get<string>(SampleKey));
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
        public void Get_ThrowsKeyNotFoundException_IfNotFound_ReferenceType()
        {
            using (GetErrorHttpResultsFilter())
            {
                Assert.Throws<KeyNotFoundException>(() => appSettings.Get<Human>(SampleKey));
            }
        }

        [Fact]
        public void GetWithFallback_CallsGetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.Get(SampleKey, new Human()));
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

        [Fact]
        public void GetDictionary_CallsGetEndpoint()
        {
            string dictResult;
            Dictionary<string, string> dict = GenerateDictionaryResponse(out dictResult);

            VerifyGetEndpoint(() => appSettings.GetDictionary(SampleKey), result: dictResult);
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
        public void GetDictionary_ThrowsKeyNotFoundException_IfNotFound()
        {
            using (GetErrorHttpResultsFilter())
            {
                Assert.Throws<KeyNotFoundException>(() => appSettings.GetDictionary(SampleKey));
            }
        }

        [Fact]
        public void GetList_CallsGetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.GetList(SampleKey));
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
        public void GetList_ThrowsKeyNotFoundException_IfNotFound()
        {
            using (GetErrorHttpResultsFilter())
            {
                Assert.Throws<KeyNotFoundException>(() => appSettings.GetList(SampleKey));
            }
        }

        [Fact]
        public void Set_CallsSetEndpoint()
        {
            VerifyGetEndpoint(() => appSettings.Set(SampleKey, 12345), "PUT");
        }

        private static void VerifyGetEndpoint(Action callEndpoint, string verb = "GET", string result = ConsulResultString)
        {
            HttpWebRequest webRequest = null;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    webRequest = request;
                    return result;
                }
            })
            {
                callEndpoint();

                var expected = new Uri($"{DefaultUrl}{SampleKey}");

                webRequest.RequestUri.Should().Be(expected);
                webRequest.Method.Should().Be(verb);
            }
        }

        private static HttpResultsFilter GetErrorHttpResultsFilter()
        {
            return new HttpResultsFilter { StringResultFn = (request, s) => { throw new WebException(); } };
        }

        private static HttpResultsFilter GetStandardHttpResultsFilter(string keysJson = ConsulResultString)
        {
            return new HttpResultsFilter { StringResult = keysJson };
        }

        private static Dictionary<string, string> GenerateDictionaryResponse(out string dictResult)
        {
            var dict = new Dictionary<string, string>
            {
                { "One", "ValOne" },
                { "Two", "ValTwo" }
            };

            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(dict)));
            dictResult = $"[{{\"Key\":\"Key1212\",\"Value\":\"{base64String}\"}}]";
            return dict;
        }
    }

    public class Human
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
