using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Pose.IL;
using Xunit;

// ReSharper disable PossibleNullReferenceException

namespace Pose.Tests
{
    public class StubsTests
    {
        [Fact]
        public void TestGenerateStubForStaticMethod()
        {
            // Arrange
            var methodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });
            var dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            
            var dynamicMethodParameters = dynamicMethod.GetParameters();
            var methodParameters = methodInfo.GetParameters();

            // Assert
            dynamicMethodParameters.Should().HaveSameCount(methodParameters);
            dynamicMethodParameters.FirstOrDefault()?.ParameterType.Should().Be(methodParameters.FirstOrDefault()?.ParameterType);
        }
        
        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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