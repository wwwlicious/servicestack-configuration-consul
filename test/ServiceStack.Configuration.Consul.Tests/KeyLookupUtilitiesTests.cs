// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Testing;
    using Xunit;

    [Collection("KeyFormatterTests")]
    public class KeyLookupUtilitiesTests : IDisposable
    {
        private ServiceStackHost appHost;
        private const string Key = "findMe";
        private const string ServiceName = "testService";

        [Fact]
        public void GetPossibleKeys_NullAppHost_ReturnsDefault()
        {
            var values = KeyLookupUtilities.GetPossibleKeys(Key);
            values.Count().Should().Be(1);
            values.First().Should().Be($"ss/{Key}");
        }

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
        public void GetNonDefault_CallsDelegate_WithKeys()
        {
            var callList = new List<string>(2);

            var keys = new[] { Key, Key + Key };
            KeyLookupUtilities.GetNonDefault(keys, str =>
                        {
                            callList.Add(str);
                            return str;
                        }).ToList();

            keys[0].Should().Be(Key);
            keys[1].Should().Be(Key + Key);
        }

        [Fact]
        public void GetNonDefault_ReturnsNonDefault()
        {
            var keys = new[] { Key, null, Key + Key };
            var results = KeyLookupUtilities.GetNonDefault(keys, str => str).ToList();

            results.Count.Should().Be(2);
            results[0].Should().Be(Key);
            results[1].Should().Be(Key + Key);
        }

        [Fact]
        public void GetValue_ReturnsFirstNonDefault()
        {
            Init();
            int count = 0;
            var result = KeyLookupUtilities.GetMostSpecificValue(Key, str =>
                        {
                            count++;
                            return str == $"ss/{ServiceName}/{Key}" ? "nonDefault" : null;
                        });

            count.Should().Be(2);
            result.Should().Be("nonDefault");
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
}
