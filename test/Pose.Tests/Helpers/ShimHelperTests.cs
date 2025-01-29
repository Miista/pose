using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Pose.Exceptions;
using Pose.Helpers;
using Xunit;
// ReSharper disable PossibleNullReferenceException

namespace Pose.Tests
{
    public class ShimHelperTests
    {
        [Theory]
        [MemberData(nameof(Throws_NotImplementedException_Data))]
        public void Throws_NotImplementedException<T>(Expression<Func<T>> expression, string reason)
        {
            // Act
            Action act = () => ShimHelper.GetMethodFromExpression(expression.Body, false, out _);
            
            // Assert
            act.Should().Throw<UnsupportedExpressionException>(because: reason);
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object[]> Throws_NotImplementedException_Data
        {
            get
            {
                yield return TestCase(() => true, "Constant expressions are not supported");
                yield return TestCase(() => DateTime.MaxValue, "Field access is not supported in general");
                yield return TestCase(() => string.Empty, "Field access is not supported in general");
                
                object[] TestCase<T>(Expression<Func<T>> expression, string reason)
                {
                    return new object[] { expression, reason };
                }
            }
        }
        [Theory]
        [MemberData(nameof(Can_get_method_from_valid_expression_Data))]
        public void Can_get_method_from_valid_expression<T>(Expression<Func<T>> expression, MethodInfo expectedMethod)
        {
            // Act
            var methodFromExpression = ShimHelper.GetMethodFromExpression(expression.Body, false, out _);
            
            // Assert
            methodFromExpression.Should().BeEquivalentTo(expectedMethod);
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object[]> Can_get_method_from_valid_expression_Data
        {
            get
            {
                yield return TestCase(() => DateTime.Now, typeof(DateTime).GetMethod("get_Now"));
                yield return TestCase(() => Console.ReadLine(), typeof(Console).GetMethod(nameof(Console.ReadLine)));
                object[] TestCase<T>(Expression<Func<T>> expression, MethodInfo expectedMethod)
                {
                    return new object[] { expression, expectedMethod };
                }
            }
        }
        [Fact]
        public void Throws_when_getting_object_instance_for_value_type()
        {
            // Arrange
            var dateTime = new DateTime();
            Expression<Func<DateTime>> expression = () => dateTime.AddDays(2);

            // Act
            Action act = () => ShimHelper.GetObjectInstanceOrType((expression.Body as MethodCallExpression).Object);
            
            // Assert
            act.Should().Throw<NotSupportedException>(because: "value types are not supported");
        }

        [Fact]
        public void Can_get_object_instance_from_expression()
        {
            // Arrange
            var shimHelperTests = new ShimHelperTests();
            Expression<Action> expression = () => shimHelperTests.Can_get_object_instance_from_expression();
            
            // Act
            var instance = ShimHelper.GetObjectInstanceOrType((expression.Body as MethodCallExpression).Object);

            // Assert
            instance.Should().NotBeNull();
            instance.Should().BeOfType<ShimHelperTests>();
            instance.Should().BeSameAs(shimHelperTests);
            instance.Should().NotBeSameAs(new ShimHelperTests());
        }

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

        internal class Object
        {
            public readonly string _name;
            
            public string Name { get; set; }
            
            public Object(string name)
            {
                Name = name;
            }

            public string ResolveName()
            {
                return Name;
            }

            public int DoSomething(string name, int age)
            {
                return 1;
            }
        }
        
        [Theory]
        [MemberData(nameof(Can_validate_expression_Data))]
        public void Can_validate_expression(Func<Shim> shimCreator)
        {
            shimCreator.Should().NotThrow(because: "the expression is valid");
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object[]> Can_validate_expression_Data
        {
            get
            {
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l, OperatorsClass r) => l + r)
                };
                
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => +Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l) => l)
                };
                
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() - Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l, OperatorsClass r) => l - r)
                };
                
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => new Object(Is.A<string>()))
                        .WithExpression((string s) => new Object("Hello"))
                };
                
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => new Object(Is.A<string>()))
                        .WithExpression((string s) => new Object("Hello"))
                };

                yield return new object[]
                {
                    () => Shim
                        .Replace(() => Is.A<Object>().Name)
                        .WithExpression((string s) => nameof(Object.Name))
                };
                
                yield return new object[]
                {
                    () => Shim
                        .Replace(() => Is.A<Object>().ResolveName())
                        .WithExpression((string s) => nameof(Object.Name))
                };
            }
        }

        [Theory]
        [MemberData(nameof(Throws_on_invalid_expression_Data))]
        public void Throws_on_invalid_expression(Func<Shim> shimCreator, string expectedMessage)
        {
            shimCreator.Should().Throw<InvalidShimSignatureException>(because: "the expression is invalid").WithMessage(expectedMessage);
        }
        
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object[]> Throws_on_invalid_expression_Data
        {
            get
            {
                yield return TestCase(
                    "Parameters count do not match* Expected 2* Got 1",
                    () => Shim
                        .Replace(() => Is.A<Object>().DoSomething(Is.A<string>(), Is.A<int>()))
                        .WithExpression((Object o, string s) => 1)
                );
                yield return TestCase(
                    "Mismatched return types* Expected *OperatorsClass* got *String*",
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l, OperatorsClass r) => "Hello")
                );
                yield return TestCase(
                    "Parameter types * do not match. Expected *OperatorsClass* but found *TimeSpan*",
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l, TimeSpan r) => l)
                );
                yield return TestCase(
                    "Parameter types * do not match. Expected *OperatorsClass* but found *TimeSpan*",
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                        .WithExpression((TimeSpan l, OperatorsClass r) => r)
                );
                yield return TestCase(
                    "Parameter types * do not match. Expected *OperatorsClass* but found *TimeSpan*",
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() + Is.A<OperatorsClass>())
                        .WithExpression((TimeSpan l, TimeSpan r) => new OperatorsClass())
                );
                
                object[] TestCase(string expectedMessage, Func<Shim> shimCreator)
                {
                    return new object[] { shimCreator, expectedMessage };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Throws_UnsupportedExpressionException_Data))]
        public void Throws_UnsupportedExpressionException(Func<Shim> shimCreator, string expectedMessage)
        {
            shimCreator.Should()
                .Throw<UnsupportedExpressionException>(because: "the expression is not supported")
                .WithMessage(expectedMessage);
        }
        
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object[]> Throws_UnsupportedExpressionException_Data
        {
            get
            {
                yield return TestCase(
                    "Expression * with NodeType 'AndAlso' is not supported",
                    () => Shim
                        .Replace(() => Is.A<OperatorsClass>() && Is.A<OperatorsClass>())
                        .WithExpression((OperatorsClass l, OperatorsClass r) => l)
                );
                yield return TestCase(
                    "Expression *FieldExpression* with NodeType 'MemberAccess' is not supported",
                    () => Shim
                        .Replace(() => Is.A<Object>()._name)
                        .WithExpression((Object o) => "Hello")
                );
                
                object[] TestCase(string expectedMessage, Func<Shim> shimCreator)
                {
                    return new object[] { shimCreator, expectedMessage };
                }
            }
        }
    }
}
