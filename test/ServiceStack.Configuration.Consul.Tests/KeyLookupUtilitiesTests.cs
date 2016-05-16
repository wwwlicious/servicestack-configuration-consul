// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using Testing;
    using Xunit;

    [Collection("KeyLookupUtilitiesTests")]
    public class KeyLookupUtilitiesTests : IDisposable
    {
        private ServiceStackHost appHost;
        private const string Key = "findMe";
        private const string ServiceName = "testService";

        [Fact]
        public void GetPossibleKeys_ValuesInCorrectOrder()
        {
            Init();
            var values = KeyLookupUtilities.GetPossibleKeys(Key).ToList();
            values.Count.Should().Be(3);
            values[0].Should().Be($"ss/{ServiceName}/1.0/{Key}");
            values[1].Should().Be($"ss/{ServiceName}/{Key}");
            values[2].Should().Be($"ss/{Key}");
        }

        [Fact]
        public void GetValue_ReturnsFirstSuccessful()
        {
            Init();
            int count = 0;
            var result = KeyLookupUtilities.GetMostSpecificValue(Key, str =>
                        {
                            count++;
                            return str == $"ss/{ServiceName}/{Key}"
                                       ? Result<string>.Success("nonDefault")
                                       : Result<string>.Fail();
                        });

            count.Should().Be(2);
            result.Value.Should().Be("nonDefault");
        }

        public void Dispose() => appHost?.Dispose();

        private void Init()
        {
            if (ServiceStackHost.Instance == null)
            {
                appHost = new BasicAppHost { TestMode = true, ServiceName = ServiceName };
                appHost.Init();
            }
        }
    }

    [Collection("KeyLookupUtilitiesTests")]
    public class KeyLookupUtilitiesHostlessTests : IDisposable
    {
        private readonly AppDomain noHost;

        public KeyLookupUtilitiesHostlessTests()
        {
            noHost = AppDomain.CreateDomain("NoAppHost", AppDomain.CurrentDomain.Evidence,
                AppDomain.CurrentDomain.SetupInformation);
        }

        [Fact]
        public void GetPossibleKeys_NullAppHost_ReturnsDefault()
        {
            const string key = "findMe";
            noHost.DoCallBack(() =>
            {
                var values = KeyLookupUtilities.GetPossibleKeys(key);
                values.Count().Should().Be(1);
                values.First().Should().Be($"ss/{key}");
            });
        }

        public void Dispose()
        {
            if (noHost != null)
            {
                AppDomain.Unload(noHost);
            }
        }
    }
}
