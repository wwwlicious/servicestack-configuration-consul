// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Consul.DTO;
    using Fixtures;
    using FluentAssertions;
    using Xunit;

    [Collection("AppHost")]
    public class KeyUtilitiesTests 
    {
        private const string Key = "findMe";
        private readonly AppHostFixture fixture;

        public KeyUtilitiesTests(AppHostFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void GetPossibleKeys_AddsPrefixIfNotSupplied()
        {
            var prefixedKey = $"ss/{Key}";
            var values = KeyUtilities.GetPossibleKeys(Key).ToList();
            values.Count.Should().Be(4);
            values[0].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|api");
            values[1].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/1.0");
            values[2].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}");
            values[3].Should().Be($"{prefixedKey}");
        }

        [Fact]
        public void GetPossibleKeys_ValuesInCorrectOrder()
        {
            var prefixedKey = $"ss/{Key}";
            var values = KeyUtilities.GetPossibleKeys(prefixedKey).ToList();
            values.Count.Should().Be(4);
            values[0].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|api");
            values[1].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/1.0");
            values[2].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}");
            values[3].Should().Be($"{prefixedKey}");
        }

        [Fact]
        public void GetPossibleKeys_HandlesSlashesInVersionAndHandlerFactory()
        {
            var handler = fixture.AppHost.Config.HandlerFactoryPath;
            var api = fixture.AppHost.Config.ApiVersion;

            fixture.AppHost.Config.HandlerFactoryPath = "api/subpath";
            fixture.AppHost.Config.ApiVersion = "1/0/1";

            var prefixedKey = $"ss/{Key}";
            var values = KeyUtilities.GetPossibleKeys(prefixedKey).ToList();
            values.Count.Should().Be(4);
            values[0].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|api|subpath");
            values[1].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}/1.0.1");
            values[2].Should().Be($"{prefixedKey}/{AppHostFixture.ServiceName}");
            values[3].Should().Be($"{prefixedKey}");

            // Set it back
            fixture.AppHost.Config.HandlerFactoryPath = handler;
            fixture.AppHost.Config.ApiVersion = api;
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
        public void GetMostSpecificMatch_ReturnsReturnsInstanceFirst()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}/1.0" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|api" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|api");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsReturnsVersion_IfNoMatchingInstance()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}/1.0" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}/i/127.0.0.1:8090|waa" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{AppHostFixture.ServiceName}/1.0");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsService_IfNoMatchingVersion()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}/12.0" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{AppHostFixture.ServiceName}");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsService_IfNoVersion()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}" }
            };

            var result = KeyUtilities.GetMostSpecificMatch(list, Key);

            result.Key.Should().Be($"ss/{Key}/{AppHostFixture.ServiceName}");
        }

        [Fact]
        public void GetMostSpecificMatch_ReturnsKey_IfNoMatchingService()
        {
            var list = new List<KeyValue>
            {
                new KeyValue { Key = $"ss/{Key}" },
                new KeyValue { Key = $"ss/{Key}/{AppHostFixture.ServiceName}spsps" }
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
