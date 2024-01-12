using System;
using FluentAssertions;
using Pose.Extensions;
using Xunit;

namespace Pose.Tests
{
    public class MethodBaseExtensionsTests
    {
        [Fact]
        public void InCore_library()
        {
            // Arrange
            var sut = typeof(Exception).GetMethod(nameof(Exception.ToString));

            // Act
            var result = sut.InCoreLibrary();

            // Assert
            result.Should().BeTrue(because: $"{nameof(Exception.ToString)} is in the core library");
        }
    }
}
