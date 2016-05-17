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
        protected const string ConsulResultString = "[{\"Key\":\"ss/Key1212\",\"Value\":\"dGVzdFN0cmluZw==\"}]";

        protected const string ConsulResultStringSlashKey = "[{\"Key\":\"ss/foo/bar\",\"Value\":\"dGVzdFN0cmluZw==\"}]";

        // NOTE This is a sample result 
        protected const string ConsulResultComplex = "[{\"Key\":\"ss/Key1212\",\"Value\":\"e0FnZTo5OSxOYW1lOlRlc3QgUGVyc29ufQ==\"}]";

        protected const string DefaultUrl = "http://127.0.0.1:8500/v1/kv/";

        protected const string SampleKey = "Key1212";

        protected const string SlashKey = "foo/bar";

        protected static void VerifyEndpoint(Action callEndpoint, string verb = "GET", string result = ConsulResultString, string key = SampleKey)
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

                var expectedString = $"{DefaultUrl}ss/{key}";
                if (verb == "GET")
                    expectedString += "?recurse";

                var expected = new Uri(expectedString);
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
            dictResult = $"[{{\"Key\":\"ss/Key1212\",\"Value\":\"{base64String}\"}}]";
            return dict;
        }
    }
}