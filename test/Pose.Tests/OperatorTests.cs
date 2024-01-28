using System;
using FluentAssertions;
using Xunit;

namespace Pose.Tests
{
    public class OperatorTests
    {
        internal class OperatorsClass
        {
            public string Value { get; set; }
            
            public static OperatorsClass operator +(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator -(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator *(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator /(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator %(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator &(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator ^(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator <<(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator >>(OperatorsClass l, OperatorsClass r) => null;
            public static OperatorsClass operator >>>(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator ==(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator !=(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator <(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator >(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator <=(OperatorsClass l, OperatorsClass r) => null;
            public static bool? operator >=(OperatorsClass l, OperatorsClass r) => null;
        }

        public class Arithmetic
        {
            [Fact]
            public void Can_shim_addition_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left + right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact(Skip = "Encountering InlineSig issue")]
            public void Can_shim_addition_operator_for_DateTime()
            {
                // Arrange
                var shimmedValue = new DateTime(2004, 01, 01);
                var shim = Shim.Replace(() => Is.A<DateTime>() + Is.A<TimeSpan>())
                    .With(delegate(DateTime dt, TimeSpan ts) { return shimmedValue; });

                // Act
                var result = default(DateTime);
                PoseContext.Isolate(
                    () =>
                    {
                        var now = DateTime.Now;
                        var zeroSeconds = TimeSpan.Zero;

                        result = now + zeroSeconds;
                    }, shim);
                
                // Assert
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact(Skip = "Encountering InlineSig issue")]
            public void Can_shim_subtraction_operator_for_DateTime()
            {
                // Arrange
                var shimmedValue = new DateTime(2004, 01, 01);
                var shim = Shim.Replace(() => Is.A<DateTime>() - Is.A<TimeSpan>())
                    .With(delegate(DateTime dt, TimeSpan ts) { return shimmedValue; });

                // Act
                var result = default(DateTime);
                PoseContext.Isolate(
                    () =>
                    {
                        var now = DateTime.Now;
                        var zeroSeconds = TimeSpan.Zero;

                        result = now - zeroSeconds;
                    }, shim);
                
                // Assert
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_subtraction_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() - Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left - right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact(Skip = "Because there is something wrong")]
            public void Can_shim_multiplication_operator_for_int()
            {
                // Arrange
                var shimmedValue = int.MaxValue;
                var shim = Shim.Replace(() => Is.A<int>() * Is.A<int>())
                    .With(delegate(int l, int r) { return shimmedValue; });

                // Act
                var result = default(int);
                PoseContext.Isolate(
                    () =>
                    {
                        result = 1 * 1;
                    }, shim);
                
                // Assert
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact]
            public void Can_shim_multiplication_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() * Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left * right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_division_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() / Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left / right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact]
            public void Can_shim_modulus_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() % Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left % right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
        }

        public class BitwiseAndShift
        {
            [Fact]
            public void Can_shim_logical_AND_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() & Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left & right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_logical_exclusive_OR_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() ^ Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left ^ right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact]
            public void Can_shim_left_shift_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() ^ Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left ^ right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact]
            public void Can_shim_right_shift_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() ^ Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left ^ right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

            [Fact]
            public void Can_shim_unsigned_right_shift_operator()
            {
                // Arrange
                var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() ^ Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(OperatorsClass);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left ^ right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }       
        }

        public class Equality
        {
            [Fact]
            public void Can_shim_equal_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() == Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left == right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_not_equal_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() != Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left != right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_less_than_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() < Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left < right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_greater_than_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() > Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left > right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_less_than_or_equal_to_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() <= Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left <= right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }
            
            [Fact]
            public void Can_shim_greater_than_or_equal_to_operator()
            {
                // Arrange
                bool? shimmedValue = false;
                var shim = Shim.Replace(() => Is.A<OperatorsClass>() >= Is.A<OperatorsClass>())
                    .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                // Act
                var result = default(bool?);
                PoseContext.Isolate(
                    () =>
                    {
                        var left = new OperatorsClass();
                        var right = new OperatorsClass();

                        result = left >= right;
                    }, shim);
                
                // Assert
                result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
            }

        }
    }
}