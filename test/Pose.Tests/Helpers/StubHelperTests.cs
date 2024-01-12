using System;
using System.Reflection.Emit;
using FluentAssertions;
using Pose.Helpers;
using Xunit;

namespace Pose.Tests
{
    public class StubHelperTests
    {
        [Fact]
        public void Can_get_method_pointer()
        {
            // Arrange
            var methodInfo = typeof(Console).GetMethod(nameof(Console.Clear));
            var dynamicMethod = new DynamicMethod("Method", typeof(void), Type.EmptyTypes);
            
            // Act
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            // Assert
            StubHelper.GetMethodPointer(methodInfo).Should().NotBe(IntPtr.Zero);
            StubHelper.GetMethodPointer(dynamicMethod).Should().NotBe(IntPtr.Zero);
        }

        [Fact]
        public void Get_get_shim_delegate_target()
        {
            // Arrange
            var action = new Action(Console.Clear);
            var shim = Shim.Replace(() => Console.Clear()).With(action);
            
            // Act
            PoseContext.Isolate(() => { }, shim);

            // Assert
            var shimDelegateTarget = StubHelper.GetShimDelegateTarget(0);
            action.Target.Should().BeEquivalentTo(shimDelegateTarget);
            action.Target.Should().BeSameAs(shimDelegateTarget);
        }
        
        [Fact]
        public void Can_get_shim_replacement_method()
        {
            // Arrange
            var action = new Action(Console.Clear);
            var shim = Shim.Replace(() => Console.Clear()).With(action);
            
            // Act
            PoseContext.Isolate(() => { }, shim);

            // Assert
            var shimReplacementMethod = StubHelper.GetShimReplacementMethod(0);
            action.Method.Should().BeSameAs(shimReplacementMethod);
            action.Method.Should().ReturnVoid();
        }

        [Fact]
        public void Can_get_index_of_matching_shim()
        {
            // Arrange
            var stubHelperTests = new StubHelperTests();
            var staticAction = new Action(() => { });
            var instanceAction = new Action<StubHelperTests>(@this => { });

            var shim = Shim.Replace(() => Console.Clear()).With(staticAction);
            var shim1 = Shim.Replace(() => Is.A<StubHelperTests>().Can_get_index_of_matching_shim()).With(instanceAction);
            var shim2 = Shim.Replace(() => stubHelperTests.Can_get_index_of_matching_shim()).With(instanceAction);
            
            // Act
            PoseContext.Isolate(() => { }, shim, shim1, shim2);

            // Assert
            var consoleMethodInfo = typeof(Console).GetMethod(nameof(Console.Clear));
            var stubMethodInfo = typeof(StubHelperTests).GetMethod(nameof(stubHelperTests.Can_get_index_of_matching_shim));

            var indexOfConsoleMethodShim = StubHelper.GetIndexOfMatchingShim(consoleMethodInfo, null);
            var indexOfMatchingShimForAnyInstance = StubHelper.GetIndexOfMatchingShim(stubMethodInfo, new StubHelperTests());
            var indexOfMatchingShimForSpecificInstance = StubHelper.GetIndexOfMatchingShim(stubMethodInfo, stubHelperTests);

            indexOfConsoleMethodShim.Should().Be(0, because: "this shim is passed in first");
            indexOfMatchingShimForAnyInstance.Should().Be(1, because: "this shim is passed in second");
            indexOfMatchingShimForSpecificInstance.Should().Be(2, because: "this shim is passed in third");
        }

        [Fact]
        public void Can_get_runtime_method_for_virtual_method()
        {
            // Arrange
            var type = typeof(StubHelperTests);
            var methodInfo = type.GetMethod(nameof(StubHelperTests.Can_get_runtime_method_for_virtual_method));
            
            // Act
            var devirtualizedMethodInfo = StubHelper.DeVirtualizeMethod(type, methodInfo);
            
            // Assert
            devirtualizedMethodInfo.Should().BeSameAs(methodInfo);
        }

        [Fact]
        public void Can_get_owning_module()
        {
            StubHelper.GetOwningModule().Should().Be(typeof(StubHelper).Module);
            StubHelper.GetOwningModule().Should().NotBe(typeof(StubHelperTests).Module);
        }
    }
}
