// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Tests
{
    using FluentAssertions;
    using Xunit;

    public class ResultTests
    {
        [Fact]
        public void Fail_IsSuccess_False() => Result<string>.Fail().IsSuccess.Should().BeFalse();

        [Fact]
        public void Fail_Value_DefaultT() => Result<string>.Fail().Value.Should().Be(default(string));

        [Fact]
        public void Fail_WithValue_IsSuccess() => Result<string>.Fail("dance").IsSuccess.Should().BeFalse();

        [Fact]
        public void Fail_WithValue_Value() => Result<string>.Fail("dance").Value.Should().Be("dance");

        [Fact]
        public void Success_IsSuccess_True() => Result<string>.Success("wallop").IsSuccess.Should().BeTrue();

        [Fact]
        public void Success_Value() => Result<string>.Success("wallop").Value.Should().Be("wallop");
    }
}
