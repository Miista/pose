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
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    
    public class MethodRewriterTests
    {
        [Fact]
        public void Can_rewrite_static_method()
        {
            // Arrange
            var methodInfo = typeof(DateTime).GetMethod("get_Now");
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);

            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Func<DateTime>));

            // Assert
            Assert.AreEqual(DateTime.Now.ToString("yyyyMMdd_HHmm"), ((DateTime)func.DynamicInvoke()).ToString("yyyyMMdd_HHmm"));
        }
        
        [Fact]
        public void Cannot_rewrite_method_in_CoreLib()
        {
            // Arrange
            var methodInfo = typeof(DateTime).GetMethod("get_Now");
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);

            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Func<DateTime>));

            // Assert
            Assert.AreEqual(DateTime.Now.ToString("yyyyMMdd_HHmm"), ((DateTime)func.DynamicInvoke()).ToString("yyyyMMdd_HHmm"));
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