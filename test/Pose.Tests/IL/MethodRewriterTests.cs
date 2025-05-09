// ReSharper disable PossibleNullReferenceException

namespace Pose.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;
    using FluentAssertions;
    using IL;
    using Xunit;
    
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
            var methodInfo = typeof(Exception).GetMethod("get_Message");
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);

            // Act
            var dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;
            var func = dynamicMethod.CreateDelegate(typeof(Func<Exception, string>));

            // Assert
            var exception = new Exception();
            func.DynamicInvoke(exception).As<string>().Should().BeEquivalentTo(exception.Message);
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
            
            // A shim is necessary for the entry point to be rewritten
            var shim = Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(delegate(string s) { Console.WriteLine(s); });
            
            Action act = () => PoseContext.Isolate(
                () =>
                {
                    try { called = true; }
                    catch (Exception) { enteredCatchBlock = true; }
                }, shim);

            act.Should().NotThrow();
            called.Should().BeTrue();
            enteredCatchBlock.Should().BeFalse();
        }

        private int Switch(int value)
        {
            return value switch
            {
                0 => 1,
                1 => 2,
                _ => -1
            };
        }
        
        [Fact]
        public void Can_handle_switch_statements()
        {
            var value = 1;
            var result = default(int);

            // A shim is necessary for the entry point to be rewritten
            var shim = Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(delegate(string s) { Console.WriteLine(s); });

            Action act = () => PoseContext.Isolate(
                () =>
                {
                    result = Switch(value);
                }, shim);

            act.Should().NotThrow();
            result.Should().Be(2, because: "that is the value assigned in the given switch branch");
        }
        
#if NET47 || NET48
        [Fact(Skip = "Not supported on .NET Framework 4.7+")]
#else
        [Fact]
#endif
        public void Can_handle_exception_filters()
        {
            var value = 1;
            var result = default(int);

            // A shim is necessary for the entry point to be rewritten
            var shim = Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(delegate(string s) { Console.WriteLine(s); });

            Action act = () => PoseContext.Isolate(
                () =>
                {
                    try
                    {
                        throw new Exception("Hello");
                    }
                    catch (Exception e) when (e.Message == "Hello")
                    {
                        result = 1;
                    }
                    catch (Exception)
                    {
                        result = -1;
                    }
                }, shim);

            act.Should().NotThrow();
            result.Should().Be(1, because: "that is the value assigned in the matched catch block");
        }

        public class OpCodes
        {
            private static readonly Shim DummyShim = Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(delegate(string s) { Console.WriteLine(s); });

            [Fact]
            public void Can_handle_InlineI8()
            {
                var value = default(long);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        value = long.MaxValue;
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(long.MaxValue, because: "that is the value assigned");
            }
        
            [Fact]
            public void Can_handle_InlineI()
            {
                var value = default(int);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        value = int.MaxValue;
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(int.MaxValue, because: "that is the value assigned");
            }
            
            [Fact]
            public void Can_handle_ShortInlineI()
            {
                var value = default(sbyte);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        value = sbyte.MaxValue;
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(sbyte.MaxValue, because: "that is the value assigned");
            }
            
            [Fact]
            public void Can_handle_ShortInlineR()
            {
                var value = default(Single);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        value = Single.MaxValue;
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(Single.MaxValue, because: "that is the value assigned");
            }
            
            [Fact]
            public void Can_handle_InlineR()
            {
                var value = default(double);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        value = double.MaxValue;
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(double.MaxValue, because: "that is the value assigned");
            }
            
            [Fact]
            public void Can_handle_Switch()
            {
                var value = default(int);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        var a = int.MaxValue;
                        switch(a)
                        {
                            case 1:  value = 1; break;
                            case 2:  value = 2; break;
                            case 3:  value = 3; break;
                            default: value = int.MinValue; break;
                        }
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(int.MinValue, because: "that is the value assigned");
            }
        }
    }
}