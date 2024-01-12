using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Pose.IL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Pose.Tests
{
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
        
        [TestMethod]
        public void TestStaticMethodRewrite()
        {
            MethodInfo methodInfo = typeof(DateTime).GetMethod("get_Now");
            MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            DynamicMethod dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            Delegate func = dynamicMethod.CreateDelegate(typeof(Func<DateTime>));
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
    }
}