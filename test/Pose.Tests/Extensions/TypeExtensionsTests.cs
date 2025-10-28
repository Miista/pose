using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Pose.Extensions;
using Xunit;

namespace Pose.Tests
{
    public class TypeExtensionsTests
    {
        private static async Task<int> GetIntAsync() => await Task.FromResult(1);

        [Fact]
        public void Can_get_explicitly_implemented_MoveNext_method_on_state_machine()
        {
            // Arrange
            var stateMachineType = typeof(TypeExtensionsTests).GetMethod(nameof(GetIntAsync), BindingFlags.Static | BindingFlags.NonPublic)?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            
            // Act
            Func<MethodInfo> func = () => stateMachineType.GetExplicitlyImplementedMethod<IAsyncStateMachine>(nameof(IAsyncStateMachine.MoveNext));
            
            // Assert
            func.Should().NotThrow(because: "it is possible to get the MoveNext method on the state machine");

            var moveNextMethod = func();
            moveNextMethod.Should().NotBeNull(because: "the method exists");
            moveNextMethod.ReturnType.Should().Be(typeof(void));

            var parameters = moveNextMethod.GetParameters();
            parameters.Should().BeEmpty(because: "the method does not take any parameters");
        }
    }
}