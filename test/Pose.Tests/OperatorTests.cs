using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Pose.Tests
{
    public class OperatorTests
    {
        [Fact]
        public void Can_shim_addition_operator()
        {
            // Arrange
            var shimmedValue = new DateTime(2004, 01, 01);
            var dateTimeAddShim = Shim.Replace(() => Is.A<DateTime>() + Is.A<TimeSpan>())
                .With(delegate(DateTime dt, TimeSpan ts) { return shimmedValue; });

            // Act
            var result = default(DateTime);
            PoseContext.Isolate(
                () =>
                {
                    var now = DateTime.Now;
                    var zeroSeconds = TimeSpan.Zero;

                    result = now + zeroSeconds;
                }, dateTimeAddShim);
            
            // Assert
            result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
        }
        
        [Fact]
        public void Can_shim_subtraction_operator()
        {
            // Arrange
            var shimmedValue = new DateTime(2004, 01, 01);
            var dateTimeAddShim = Shim.Replace(() => Is.A<DateTime>() - Is.A<TimeSpan>())
                .With(delegate(DateTime dt, TimeSpan ts) { return shimmedValue; });

            // Act
            var result = default(DateTime);
            PoseContext.Isolate(
                () =>
                {
                    var now = DateTime.Now;
                    var zeroSeconds = TimeSpan.Zero;

                    result = now - zeroSeconds;
                }, dateTimeAddShim);
            
            // Assert
            result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
        }
        
        [Fact]
        public void Can_shim_multiplication_operator()
        {
            // Arrange
            var shimmedValue = int.MaxValue;
            var dateTimeAddShim = Shim.Replace(() => Is.A<int>() * Is.A<int>())
                .With(delegate(int l, int r) { return shimmedValue; });

            // Act
            var result = default(int);
            PoseContext.Isolate(
                () =>
                {
                    result = 1 * 1;
                }, dateTimeAddShim);
            
            // Assert
            result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
        }
    }
}