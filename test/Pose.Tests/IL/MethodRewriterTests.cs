using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using FluentAssertions;
using Pose.IL;
using Xunit;
// ReSharper disable PossibleNullReferenceException

namespace Pose.Tests
{
    public class MethodRewriterTests
    {
        private class ClassWithStaticMethod
        {
            public static string Now { get; } = "?";
        }
        
        [Fact]
        public void Can_rewrite_static_method()
        {
            // Arrange
            var methodInfo = typeof(ClassWithStaticMethod).GetMethod("get_Now");
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);

            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Func<string>));

            // Assert
            func.DynamicInvoke().As<string>().Should().BeEquivalentTo("?");
        }
        
        [Fact]
        public void Cannot_rewrite_method_in_CoreLib()
        {
            // Arrange
            var methodInfo = typeof(DateTime).GetMethod("get_UtcNow");
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);

            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Func<DateTime>));

            // Assert
            func.DynamicInvoke().As<DateTime>().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Can_rewrite_instance_method()
        {
            // Arrange
            const string item = "Item 1";
            
            var list = new List<string>();
            var methodInfo = typeof(List<string>).GetMethod(nameof(List<string>.Add));
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Action<List<string>, string>));

            // Act
            func.DynamicInvoke(list, item);

            // Assert
            list.Should().HaveCount(1);
            list[0].Should().BeEquivalentTo(item);
        }

        [Fact]
        public void Can_rewrite_constructor()
        {
            // Arrange
            var constructorInfo = typeof(List<string>).GetConstructor(Type.EmptyTypes);
            var methodRewriter = MethodRewriter.CreateRewriter(constructorInfo, false);
            
            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            // Assert
            dynamicMethod.Should().NotBeNull();
            dynamicMethod.ReturnType.Should().Be(typeof(void));
            
            var firstParameter = dynamicMethod.GetParameters().FirstOrDefault();
            firstParameter.Should().NotBeNull();
            firstParameter.ParameterType.Should().Be<List<string>>(because: "that is the first parameter to the constructor");
        }
        
        [Fact]
        public void Can_rewrite_try_catch_returning_from_try()
        {
            // Arrange
            var methodInfo = typeof(MethodRewriterTests).GetMethod(nameof(TryCatch_ReturnsFromTry));
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            // Act
            var func = dynamicMethod.CreateDelegate(typeof(Func<int>));
            var result = (int) func.DynamicInvoke();

            // Assert
            result.Should().Be(1, because: "that is what the method returns from the try block");
        }

        public static int TryCatch_ReturnsFromTry()
        {
            try
            {
                return 1;
            }
            catch
            {
                return 0;
            }
            finally {}
        }

        [Fact]
        public void Can_rewrite_try_catch_returning_from_catch()
        {
            // Arrange
            var methodInfo = typeof(MethodRewriterTests).GetMethod(nameof(TryCatch_ReturnsFromCatch));
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            // Act
            var func = dynamicMethod.CreateDelegate(typeof(Func<int>));
            var result = (int) func.DynamicInvoke();

            // Assert
            result.Should().Be(0, because: "that is what the method returns from the catch block");
        }

        public static int TryCatch_ReturnsFromCatch()
        {
            try
            {
                throw new Exception();
            }
            catch
            {
                return 0;
            }
            finally {}
        }
        
        [Fact]
        public void Can_rewrite_try_catch_returning_from_finally()
        {
            // Arrange
            var methodInfo = typeof(MethodRewriterTests).GetMethod(nameof(TryCatch_ReturnsFromFinally));
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            // Act
            var func = dynamicMethod.CreateDelegate(typeof(Func<int>));
            var result = (int) func.DynamicInvoke();

            // Assert
            result.Should().Be(3, because: "that is what the method returns from the finally block");
        }

        public static int TryCatch_ReturnsFromFinally()
        {
            int value = 0;
            try
            {
                value = 1;
                throw new Exception();
            }
            catch
            {
                value = 2;
            }
            finally
            {
                value = 3;
            }

            return value;
        }
        
        [Fact]
        public void Can_rewrite_try_catch_blocks()
        {
            var called = false;
            var enteredCatchBlock = false;
            
            Action act = () => PoseContext.Isolate(
                () =>
                {
                    try
                    {
                        // ReSharper disable once Xunit.XunitTestWithConsoleOutput
                        Console.WriteLine("H");
                        called = true;
                    }
                    catch (Exception e)
                    {
                        enteredCatchBlock = true;
                    }
                });

            act.Should().NotThrow();
            called.Should().BeTrue();
            enteredCatchBlock.Should().BeFalse();
        }
    }
}