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

    [Collection("KeyUtilitiesTests")]
    public class KeyUtilitiesTests : IDisposable
    {
        private ServiceStackHost appHost;
        private const string Key = "findMe";
        private const string ServiceName = "testService";

        [Fact]
        public void GetPossibleKeys_ValuesInCorrectOrder()
        {
            Init();
            var values = KeyUtilities.GetPossibleKeys(Key).ToList();
            values.Count.Should().Be(3);
            values[0].Should().Be($"ss/{Key}/{ServiceName}/1.0");
            values[1].Should().Be($"ss/{Key}/{ServiceName}");
            values[2].Should().Be($"ss/{Key}");
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

    [Collection("KeyUtilitiesTests")]
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
                var values = KeyUtilities.GetPossibleKeys(key);
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
