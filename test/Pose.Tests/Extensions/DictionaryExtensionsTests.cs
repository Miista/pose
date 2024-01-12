using System.Collections.Generic;
using FluentAssertions;
// ReSharper disable once RedundantUsingDirective
using Pose.Extensions;
using Xunit;

namespace Pose.Tests
{
    public class DictionaryExtensionsTests
    {
        [Fact]
        public void TestTryAdd()
        {
            var dictionary = new Dictionary<int, string>();

            var addZeroResult = dictionary.TryAdd(0, "0");
            var addZeroAgainResult = dictionary.TryAdd(0, "1");

            addZeroResult.Should().BeTrue();
            addZeroAgainResult.Should().BeFalse(because: "the key already exists");

            var zeroKey = dictionary[0];
            zeroKey.Should().Be("0", because: "that is the value for the key");
        }
    }
}
