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
        [Fact]
        public void Throws_InvalidShimSignatureException_if_parameter_types_do_not_match()
        {
            // Arrange
            var sut = Shim.Replace(() => Is.A<List<string>>().Add(Is.A<string>()));
                
            // Act
            Action act = () => sut.With(delegate(List<string> instance, int value) { });
                
            // Assert
            act.Should().Throw<InvalidShimSignatureException>(because: "the parameter type do not match");
        }
        
        [Theory]
        [MemberData(nameof(Throws_NotImplementedException_Data))]
        public void Throws_NotImplementedException<T>(Expression<Func<T>> expression, string reason)
        {
            // Act
            Action act = () => ShimHelper.GetMethodFromExpression(expression.Body, false, out _);
            
            // Assert
            act.Should().Throw<NotImplementedException>(because: reason);
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
    }
}
