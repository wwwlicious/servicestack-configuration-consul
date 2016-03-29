namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Net;
    using FluentAssertions;
    using Xunit;

    public class ConsulAppSettingsTests
    {
        private readonly ConsulAppSettings appSettings;

        // NOTE This is a sample result that just returns "testString"
        private const string SampleConsulResult = "[{\"Key\":\"string1\",\"Value\":\"dGVzdFN0cmluZw==\"}]";
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
                    return SampleConsulResult;
                }
            })
            {
                appSettings.GetAllKeys();

                var expected = new Uri($"{DefaultUrl}?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsNullIfErrorThrown()
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

        [Fact]
        public void Exists_CallsGetEndpoint()
        {
            HttpWebRequest webRequest = null;

            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    webRequest = request;
                    return SampleConsulResult;
                }
            })
            {
                appSettings.Exists(SampleKey);

                var expected = new Uri($"{DefaultUrl}{SampleKey}");

                webRequest.RequestUri.Should().Be(expected);
            }
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

        private static HttpResultsFilter GetErrorHttpResultsFilter()
        {
            return new HttpResultsFilter { StringResultFn = (request, s) => { throw new WebException(); } };
        }

        private static HttpResultsFilter GetStandardHttpResultsFilter(string keysJson = SampleConsulResult)
        {
            return new HttpResultsFilter { StringResult = keysJson };
        }
    }
}
