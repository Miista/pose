using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Pose.IL;
using Xunit;

namespace Pose.Tests
{
    public class AsyncMethodRewriterTests
    {
        private const int AsyncMethodReturnValue = 1;

        private static async Task<int> AsyncMethodWithReturnValue()
        {
            await Task.Delay(1000);
            return AsyncMethodReturnValue;
        }
        
        private static readonly MethodInfo AsyncMethodWithReturnValueInfo = typeof(AsyncMethodRewriterTests).GetMethod(nameof(AsyncMethodWithReturnValue), BindingFlags.Static | BindingFlags.NonPublic);
        
        private static async Task AsyncMethodWithoutReturnValue()
        {
            await Task.Delay(0);
        }
        
        private static readonly MethodInfo AsyncMethodWithoutReturnValueInfo = typeof(AsyncMethodRewriterTests).GetMethod(nameof(AsyncMethodWithoutReturnValue), BindingFlags.Static | BindingFlags.NonPublic);

        private static async void AsyncVoidMethod()
        {
            await Task.Delay(0);
        }
        
        private static readonly MethodInfo AsyncVoidMethodInfo = typeof(AsyncMethodRewriterTests).GetMethod(nameof(AsyncVoidMethod), BindingFlags.Static | BindingFlags.NonPublic);
            
        [Fact]
        public void Can_rewrite_async_method_with_return_value()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncMethodWithReturnValueInfo, false);

            // Act
            Action act = () => methodRewriter.RewriteAsync();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Can_run_async_method_with_return_value()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncMethodWithReturnValueInfo, false);
            var rewrittenMethod = (MethodInfo) methodRewriter.RewriteAsync();
            var sut = rewrittenMethod.CreateDelegate(typeof(Func<Task<int>>));
            
            // Act
            Func<Task<int>> runner = () => sut.DynamicInvoke(Array.Empty<object>()) as Task<int>;
            
            // Assert
            runner.Should().NotThrowAsync().Result.Which.Should().Be(AsyncMethodReturnValue, because: "that is the return value of the async method");
        }
        
        [Fact]
        public void Can_rewrite_async_method_without_return_value()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncMethodWithoutReturnValueInfo, false);

            // Act
            Action act = () => methodRewriter.RewriteAsync();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Can_run_async_method_without_return_value()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncMethodWithoutReturnValueInfo, false);
            var rewrittenMethod = (MethodInfo) methodRewriter.RewriteAsync();
            var sut = rewrittenMethod.CreateDelegate(typeof(Func<Task>));
            
            // Act
            Func<Task> runner = () => sut.DynamicInvoke(Array.Empty<object>()) as Task;
            
            // Assert
            runner.Should().NotThrowAsync();
        }
        
        [Fact]
        public void Can_rewrite_async_void_method()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncVoidMethodInfo, false);

            // Act
            Action act = () => methodRewriter.RewriteAsync();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Can_run_async_void_method()
        {
            // Arrange
            var methodRewriter = MethodRewriter.CreateRewriter(AsyncVoidMethodInfo, false);
            var rewrittenMethod = (MethodInfo) methodRewriter.RewriteAsync();
            var sut = rewrittenMethod.CreateDelegate(typeof(Action));
            
            // Act
            Func<Task> runner = () => sut.DynamicInvoke(Array.Empty<object>()) as Task;
            
            // Assert
            runner.Should().NotThrowAsync();
        }
        
    }
}