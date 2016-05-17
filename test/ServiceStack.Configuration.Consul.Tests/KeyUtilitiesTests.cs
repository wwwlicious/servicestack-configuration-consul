﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Consul.DTO;
    using FluentAssertions;
    using Testing;
    using Xunit;

    [Collection("KeyUtilitiesTests")]
    public class KeyUtilitiesTests : IDisposable
    {
        private ServiceStackHost appHost;
        private const string Key = "findMe";
        private const string ServiceName = "testService";

        public KeyUtilitiesTests()
        {
            if (ServiceStackHost.Instance == null)
            {
                appHost = new BasicAppHost { TestMode = true, ServiceName = ServiceName };
                appHost.Init();
            }
        }

        [Fact]
        public void GetPossibleKeys_AddsPrefixIfNotSupplied()
        {
            var prefixedKey = $"ss/{Key}";
            var values = KeyUtilities.GetPossibleKeys(Key).ToList();
            values.Count.Should().Be(3);
            values[0].Should().Be($"{prefixedKey}/{ServiceName}/1.0");
            values[1].Should().Be($"{prefixedKey}/{ServiceName}");
            values[2].Should().Be($"{prefixedKey}");
        }

        [Fact]
        public void GetPossibleKeys_ValuesInCorrectOrder()
        {
            var prefixedKey = $"ss/{Key}";
            var values = KeyUtilities.GetPossibleKeys(prefixedKey).ToList();
            values.Count.Should().Be(3);
            values[0].Should().Be($"{prefixedKey}/{ServiceName}/1.0");
            values[1].Should().Be($"{prefixedKey}/{ServiceName}");
            values[2].Should().Be($"{prefixedKey}");
        }

        [Fact]
        public void GetDefaultLookupKey_ReturnsCorrect()
            => KeyUtilities.GetDefaultLookupKey(Key).Should().Be("ss/findMe");

        [Fact]
        public void GetMostSpecificMatch_ReturnsNull_IfCandidatesNull()
            => KeyUtilities.GetMostSpecificMatch(null, Key).Should().BeNull();

        [Fact]
        public void GetMostSpecificMatch_ReturnsNull_IfCandidatesEmpty()
            => KeyUtilities.GetMostSpecificMatch(new List<KeyValue>(), Key).Should().BeNull();

        [Fact]
        public void GetMostSpecificMatch_ReturnsNull_IfNoMatches()
        {
            var list = new List<KeyValue> { new KeyValue { Key = "ss/notfound" } };

            KeyUtilities.GetMostSpecificMatch(list, Key).Should().BeNull();
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsReturnsVersionFirst()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}/1.0" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{ServiceName}/1.0");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsService_IfNonMatchingVersion()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}/12.0" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{ServiceName}");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsService_IfNoVersion()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{ServiceName}");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsKey_IfNoMatchingService()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{ServiceName}spsps" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsKey_IfNoService()
        {
            var list = new List<KeyValue>{new KeyValue { Key = $"ss/{Key}" } };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}");
        }

        public void Dispose() => appHost?.Dispose();
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
