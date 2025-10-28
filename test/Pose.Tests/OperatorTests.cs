using System;
using FluentAssertions;
using Xunit;

// ReSharper disable EqualExpressionComparison
// ReSharper disable ConvertToLambdaExpression
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Local

namespace Pose.Tests
{
    public partial class OperatorTests
    {
        // ReSharper disable once ClassNeverInstantiated.Global
        public class Shimming
        {
            internal class OperatorsClass
            {
                public string Value { get; set; }
                
                public static OperatorsClass operator +(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator +(OperatorsClass l) => null;
                public static OperatorsClass operator -(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator -(OperatorsClass l) => null;
                public static OperatorsClass operator ~(OperatorsClass l) => null;
                public static OperatorsClass operator !(OperatorsClass l) => null;
                public static OperatorsClass operator *(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator |(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator /(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator %(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator &(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator ^(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator <<(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator >>(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator ==(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator !=(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator <(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator >(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator <=(OperatorsClass l, OperatorsClass r) => null;
                public static bool? operator >=(OperatorsClass l, OperatorsClass r) => null;
                public static explicit operator int(OperatorsClass c) => int.MinValue;
                public static implicit operator double(OperatorsClass c) => 42.0;
                
                // The following operators are overloadable, but they cannot be expressed in an expression tree
                public static bool operator true(OperatorsClass l) => false;
                public static bool operator false(OperatorsClass r) => true;
                public static OperatorsClass operator >>>(OperatorsClass l, OperatorsClass r) => null;
                public static OperatorsClass operator ++(OperatorsClass l) => null;
                public static OperatorsClass operator --(OperatorsClass l) => null;
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

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left + right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left + right, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_addition_operator_for_TimeSpan()
                {
                    // Arrange
                    var shimmedValue = TimeSpan.FromSeconds(2);
                    var shim = Shim.Replace(() => Is.A<TimeSpan>() + Is.A<TimeSpan>())
                        .With(delegate(TimeSpan l, TimeSpan r) { return shimmedValue; });

                    var now = TimeSpan.Zero;
                    var zeroSeconds = TimeSpan.Zero;
                    var result = default(TimeSpan);
                    
                    // Act
                    PoseContext.Isolate(() => result = now + zeroSeconds, shim);
                    
                    // Assert
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(now + zeroSeconds, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_subtraction_operator_for_TimeSpan()
                {
                    // Arrange
                    var shimmedValue = TimeSpan.FromDays(2);
                    var shim = Shim.Replace(() => Is.A<TimeSpan>() - Is.A<TimeSpan>())
                        .With(delegate(TimeSpan dt, TimeSpan ts) { return shimmedValue; });

                    var now = TimeSpan.Zero;
                    var zeroSeconds = TimeSpan.Zero;
                    var result = default(TimeSpan);
                    
                    // Act
                    PoseContext.Isolate(() => result = now - zeroSeconds, shim);
                    
                    // Assert
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(now - zeroSeconds, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_subtraction_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() - Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left - right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left - right, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_multiplication_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() * Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left * right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left * right, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_division_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() / Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left / right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left / right, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_modulus_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() % Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left % right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left % right, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_bitwise_complement_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => ~Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = ~sut, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(~sut, because: "the implementation has been shimmed");
                }
                
                [Fact(Skip = "How to get the operator method from expression?")]
                public void Can_shim_true_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = sut ? shimmedValue : null, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                }
                
                [Fact(Skip = "How to get the operator method from expression?")]
                public void Can_shim_false_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = sut ? null : shimmedValue, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                }
                
                [Fact]
                public void Can_shim_unary_plus_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => +Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = +sut, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(+sut, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_unary_minus_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => -Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = -sut, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(-sut, because: "the implementation has been shimmed");
                }
            }

            public class BitwiseAndShift
            {
                [Fact]
                public void Can_shim_left_shift_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() << Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left << right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left << right, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_right_shift_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() >> Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left >> right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left >> right, because: "the implementation has been shimmed");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left == right).Should().BeNull(because: "that is the actual implementation");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left != right).Should().BeNull(because: "that is the actual implementation");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left < right).Should().BeNull(because: "that is the actual implementation");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left > right).Should().BeNull(because: "that is the actual implementation");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left >= right).Should().BeNull(because: "that is the actual implementation");
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
                    
                    // Verify actual implementation
                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    (left <= right).Should().BeNull(because: "that is the actual implementation");
                }
            }

            public class Conversion
            {
                [Fact]
                public void Can_shim_explicit_cast_operator()
                {
                    // Arrange
                    var shimmedValue = int.MaxValue;
                    var shim = Shim.Replace(() => (int) Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = int.MinValue;
                    
                    // Act
                    PoseContext.Isolate(() => result = (int) sut, shim);
                    
                    // Assert
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe((int)sut, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_implicit_cast_operator()
                {
                    // Arrange
                    var shimmedValue = double.MaxValue;
                    // While this is in fact *NOT* the implicit operator, it does replace the correct method.
                    var shim = Shim.Replace(() => (double) Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = 42.0;
                    
                    // Act
                    PoseContext.Isolate(() => result = sut, shim);

                    // Assert
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe((double)sut, because: "the implementation has been shimmed");
                }
            }

            public class BooleanLogic
            {
                [Fact]
                public void Can_shim_logical_negation_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => !Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l) { return shimmedValue; });

                    var sut = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = !sut, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(~sut, because: "the implementation has been shimmed");
                }
                
                [Fact]
                public void Can_shim_logical_AND_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() & Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left & right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left & result, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_logical_exclusive_OR_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() ^ Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left ^ right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left ^ right, because: "the implementation has been shimmed");
                }

                [Fact]
                public void Can_shim_logical_OR_operator()
                {
                    // Arrange
                    var shimmedValue = new OperatorsClass { Value = "Hello, World" };
                    var shim = Shim.Replace(() => Is.A<OperatorsClass>() | Is.A<OperatorsClass>())
                        .With(delegate(OperatorsClass l, OperatorsClass r) { return shimmedValue; });

                    var left = new OperatorsClass();
                    var right = new OperatorsClass();
                    var result = default(OperatorsClass);
                    
                    // Act
                    PoseContext.Isolate(() => result = left | right, shim);
                    
                    // Assert
                    result.Should().NotBeNull(because: "the shim is configured to return a non-null value");
                    result.Should().Be(shimmedValue, because: "that is the value the shim is configured to return");
                    result.Should().NotBe(left | right, because: "the implementation has been shimmed");
                }
            }
        }
    }
}