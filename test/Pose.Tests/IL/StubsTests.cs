using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Pose.Helpers;
using Pose.IL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class StubsTests
    {
        [TestMethod]
        public void TestGenerateStubForStaticMethod()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(methodInfo.GetParameters()[0].ParameterType, dynamicMethod.GetParameters()[0].ParameterType);
        }
        
        [TestMethod]
        public void Can_generate_stub_for_static_method()
        {
            // Arrange
            var methodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });
            
            // Act
            var dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            
            // Assert
            var dynamicParameters = dynamicMethod.GetParameters();
            var methodParameters = methodInfo.GetParameters();
            dynamicParameters.Should().HaveSameCount(methodParameters);
            
            var firstDynamicParameter = dynamicParameters.FirstOrDefault()?.ParameterType;
            var firstMethodParameter = methodParameters.FirstOrDefault()?.ParameterType;
            firstDynamicParameter.Should().Be(firstMethodParameter);
        }

        [TestMethod]
        public void TestGenerateStubForInstanceMethod()
        {
            Type thisType = typeof(List<string>);
            MethodInfo methodInfo = thisType.GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 1);
            Assert.AreEqual(thisType, dynamicMethod.GetParameters()[0].ParameterType);
        }
        
        [TestMethod]
        public void Can_generate_stub_for_instance_method()
        {
            // Arrange
            var thisType = typeof(List<string>);
            var methodInfo = thisType.GetMethod(nameof(List<string>.Add));
            
            // Act
            var dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);

            // Assert
            var dynamicParameters = dynamicMethod.GetParameters();
            dynamicParameters.Should().HaveCount(2, because: "the dynamic method takes both the instance parameter and the value to be added");

            var instanceParameter = dynamicParameters[0];
            instanceParameter.ParameterType.Should().Be(thisType, because: "the first parameter is the instance");
            
            var valueParameter = dynamicParameters[1];
            valueParameter.ParameterType.Should().Be(typeof(string), because: "the second parameter is the value to be added");
        }

        [TestMethod]
        public void TestGenerateStubForVirtualMethod()
        {
            Type thisType = typeof(List<string>);
            MethodInfo methodInfo = thisType.GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForVirtualCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 1);
            Assert.AreEqual(thisType, dynamicMethod.GetParameters()[0].ParameterType);
        }
        
        [TestMethod]
        public void Can_generate_stub_for_virtual_call()
        {
            // Arrange
            var thisType = typeof(List<string>);
            var methodInfo = thisType.GetMethod(nameof(List<string>.Add));
            
            // Act
            var dynamicMethod = Stubs.GenerateStubForVirtualCall(methodInfo);
            
            // Assert
            var dynamicParameters = dynamicMethod.GetParameters();
            dynamicParameters.Should().HaveCount(2, because: "the dynamic method takes both the instance parameter and the value to be added");

            var instanceParameter = dynamicParameters[0];
            instanceParameter.ParameterType.Should().Be(thisType, because: "the first parameter is the instance");
            
            var valueParameter = dynamicParameters[1];
            valueParameter.ParameterType.Should().Be(typeof(string), because: "the second parameter is the value to be added");
        }

        [TestMethod]
        public void TestGenerateStubForReferenceTypeConstructor()
        {
            Type thisType = typeof(List<string>);
            ConstructorInfo constructorInfo = thisType.GetConstructor(Type.EmptyTypes);
            DynamicMethod dynamicMethod = Stubs.GenerateStubForObjectInitialization(constructorInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(constructorInfo.GetParameters().Length, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(thisType, dynamicMethod.ReturnType);
        }
        
        [TestMethod]
        public void Can_generate_stub_for_reference_type_constructor()
        {
            // Arrange
            var thisType = typeof(List<string>);
            var constructorInfo = thisType.GetConstructor(Type.EmptyTypes);
            
            // Act
            var dynamicMethod = Stubs.GenerateStubForObjectInitialization(constructorInfo);
            
            // Assert
            constructorInfo.GetParameters().Should().HaveSameCount(dynamicMethod.GetParameters());
            dynamicMethod.ReturnType.Should().Be(thisType);
        }

        [TestMethod]
        public void TestGenerateStubForMethodPointer()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectLoad(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(0, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(typeof(IntPtr), dynamicMethod.ReturnType);
        }
        
        [TestMethod]
        public void Can_generate_stub_for_method_pointer()
        {
            // Arrange
            var methodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });
            
            // Act
            var dynamicMethod = Stubs.GenerateStubForDirectLoad(methodInfo);
            
            // Assert
            dynamicMethod.GetParameters().Should().HaveCount(0);
            dynamicMethod.ReturnType.Should().Be<IntPtr>();
        }
    }
}