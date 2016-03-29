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

        private const string defaultUrl = "http://127.0.0.1:8500/v1/kv/";

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

                var expected = new Uri($"{defaultUrl}?keys");

                webRequest.RequestUri.Should().Be(expected);
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsNullIfErrorThrown()
        {
            using (new HttpResultsFilter
            {
                StringResultFn = (request, s) =>
                {
                    throw new WebException();
                }
            })
            {
                appSettings.GetAllKeys().Should().BeNull();
            }
        }

        [Fact]
        public void GetAllKeys_ReturnsKeys()
        {
            const string keysJson = "[ \"mates\", \"place\"]";

            using (new HttpResultsFilter { StringResult = keysJson })
            {
                var result = appSettings.GetAllKeys();

                result.Count.Should().Be(2);
                result[0].Should().Be("mates");
                result[1].Should().Be("place");
            }
        }
    }
}
