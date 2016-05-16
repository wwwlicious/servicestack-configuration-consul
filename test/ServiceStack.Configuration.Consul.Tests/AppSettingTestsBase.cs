namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using FluentAssertions;
    using Text;

    public class AppSettingTestsBase
    {
        // NOTE This is a sample result that just returns "testString"
        protected const string SampleString = "testString";
        protected const string ConsulResultString = "[{\"Key\":\"Key1212\",\"Value\":\"dGVzdFN0cmluZw==\"}]";

        // NOTE This is a sample result 
        protected const string ConsulResultComplex = "[{\"Key\":\"Key1212\",\"Value\":\"e0FnZTo5OSxOYW1lOlRlc3QgUGVyc29ufQ==\"}]";

        protected const string DefaultUrl = "http://127.0.0.1:8500/v1/kv/";

        protected const string SampleKey = "Key1212";

        protected static void VerifyEndpoint(Action callEndpoint, string verb = "GET", string result = ConsulResultString, string key = ConsulAppSettingsTests.SampleKey)
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

                var expected = new Uri($"{DefaultUrl}{key}");

                webRequest.RequestUri.Should().Be(expected);
                webRequest.Method.Should().Be(verb);
            }
        }

        protected static HttpResultsFilter GetErrorHttpResultsFilter()
        {
            return new HttpResultsFilter { StringResultFn = (request, s) => { throw new WebException(); } };
        }

        protected static HttpResultsFilter GetStandardHttpResultsFilter(string keysJson = ConsulResultString)
        {
            return new HttpResultsFilter { StringResult = keysJson };
        }

        protected static Dictionary<string, string> GenerateDictionaryResponse(out string dictResult)
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
}