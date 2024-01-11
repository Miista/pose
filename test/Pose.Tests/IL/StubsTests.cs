using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
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

        public static async Task<int> GetStaticAsync(int n) => await Task.FromResult(1);

        [TestMethod]
        public void TestGenerateStubForStaticAsyncMethod()
        {
            MethodInfo methodInfo = typeof(StubsTests).GetMethod(nameof(GetStaticAsync), new[] { typeof(int) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(methodInfo.GetParameters()[0].ParameterType, dynamicMethod.GetParameters()[0].ParameterType);
        }

        public async Task<int> GetInstanceAsync(int n) => await Task.FromResult(1);

        [TestMethod]
        public void TestGenerateStubForInstanceAsyncMethod()
        {
            Type thisType = typeof(StubsTests);
            MethodInfo methodInfo = thisType.GetMethod(nameof(GetInstanceAsync));
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 1);
            Assert.AreEqual(thisType, dynamicMethod.GetParameters()[0].ParameterType);
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
        public void TestGenerateStubForMethodPointer()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectLoad(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(0, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(typeof(IntPtr), dynamicMethod.ReturnType);
        }
    }
}