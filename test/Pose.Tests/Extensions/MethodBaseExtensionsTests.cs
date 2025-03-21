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

        private class NotInCoreLibrary
        {
            public override string ToString() => "This is a string representation";
        }
        
        [Fact]
        public void Not_InCore_library()
        {
            // Arrange
            var sut = typeof(NotInCoreLibrary).GetMethod(nameof(NotInCoreLibrary.ToString), Type.EmptyTypes);

            // Act
            var result = sut.InCoreLibrary();

            // Assert
            result.Should().BeFalse(because: $"{nameof(NotInCoreLibrary.ToString)} is not in the core library");
        }
    }
}
