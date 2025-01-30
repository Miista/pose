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
    
    using EmitCode = System.Reflection.Emit.OpCodes;
    
    using static TestHelpers;
    
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

        internal class DummyClass
        {
            public int DummyMethod()
            {
                return default(int);
            }
        }
        
        public class OpCodes
        {
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
            
            [Theory]
            [InlineData(int.MaxValue, int.MinValue)]
            [InlineData(2, 2)]
            [InlineData(3, 3)]
            [InlineData(4, 4)]
            public void Can_handle_Switch_theory(int input, int expected)
            {
                var value = default(int);
                Action act = () => PoseContext.Isolate(
                    () =>
                    {
                        switch(input)
                        {
                            case 1:  value = 1; break;
                            case 2:  value = 2; break;
                            case 3:  value = 3; break;
                            case 4:  value = 4; break;
                            default: value = int.MinValue; break;
                        }
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(expected, because: "that is the value assigned");
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
                            case 4:  value = 4; break;
                            default: value = int.MinValue; break;
                        }
                    }, DummyShim);

                act.Should().NotThrow();
                value.Should().Be(int.MinValue, because: "that is the value assigned");
            }

            [Theory]
            [MemberData(nameof(Handles_all_opcodes_Data))]
            public void Handles_all_opcodes(OpCode code, Action body)
            {
                // Act
                Action act = () => PoseContext.Isolate(body, DummyShim);
                
                // Assert
                act.Should().NotThrow(because: "opcode '{0}' is supported", code);
            }

            // ReSharper disable once InconsistentNaming
            public static IEnumerable<object[]> Handles_all_opcodes_Data
            {
                get
                {
                    yield return TestCase(
                        EmitCode.Ldc_I4_M1,
                        () =>
                        {
                            var i = -1;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_1,
                        () =>
                        {
                            var i = 1;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_2,
                        () =>
                        {
                            var i = 2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_3,
                        () =>
                        {
                            var i = 3;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_4,
                        () =>
                        {
                            var i = 4;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_5,
                        () =>
                        {
                            var i = 5;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_6,
                        () =>
                        {
                            var i = 6;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_7,
                        () =>
                        {
                            var i = 7;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldc_I4_8,
                        () =>
                        {
                            var i = 8;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Add,
                        () =>
                        {
                            var l = 1;
                            var r = 2;
                            var x = l + r;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Add_Ovf,
                        () =>
                        {
                            checked
                            {
                                var l = 1;
                                var r = 2;
                                var x = l + r;
                            }
                        }
                    );
                    yield return TestCase(
                        EmitCode.Add_Ovf_Un,
                        () =>
                        {
                            unchecked
                            {
                                var l = 1;
                                var r = 2;
                                var x = l + r;
                            }
                        }
                    );
                    yield return TestCase(
                        EmitCode.Sub,
                        () =>
                        {
                            var l = 1;
                            var r = 2;
                            var x = r - l;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Sub_Ovf,
                        () =>
                        {
                            checked
                            {
                                var l = 1;
                                var r = 2;
                                var x = r - l;
                            }
                        }
                    );
                    yield return TestCase(
                        EmitCode.Sub_Ovf_Un,
                        () =>
                        {
                            unchecked
                            {
                                var l = 1;
                                var r = int.MinValue;
                                var x = r - l;
                            }
                        }
                    );
                    yield return TestCase(
                        EmitCode.Mul,
                        () =>
                        {
                            var l = 1;
                            var r = 2;
                            var x = l * r;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Div,
                        () =>
                        {
                            var l = 1;
                            var r = 2;
                            var x = r / l;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Rem,
                        () =>
                        {
                            var l = 1;
                            var r = 2;
                            var x = r % l;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Neg,
                        () =>
                        {
                            var i = 1;
                            var x = -i;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldstr,
                        () =>
                        {
                            var i = "Hello";
                        }
                    );
                    yield return TestCase(
                        EmitCode.Stloc_0, // and Stloc_1, Stloc_2, Stloc_3
                        () =>
                        {
                            int i1, i2, i3, i4;
                            i1 = 0;
                            i2 = 0;
                            i3 = 0;
                            i4 = 0;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Shl,
                        () =>
                        {
                            var i = 1;
                            var x = i << 1;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Shr,
                        () =>
                        {
                            var i = 1;
                            var x = i >> 1;
                        }
                    );
                    yield return TestCase(
                        EmitCode.And,
                        () =>
                        {
                            var b1 = true;
                            var b2 = false;
                            var x = b1 && b2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Or,
                        () =>
                        {
                            var b1 = true;
                            var b2 = false;
                            var x = b1 || b2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Xor,
                        () =>
                        {
                            var b1 = true;
                            var b2 = false;
                            var x = b1 ^ b2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Not,
                        () =>
                        {
                            var b = true;
                            var x = !b;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ceq,
                        () =>
                        {
                            var i1 = 1;
                            var i2 = 2;
                            var x = i1 == i2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Cgt,
                        () =>
                        {
                            var i1 = 1;
                            var i2 = 2;
                            var x = i1 > i2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Clt,
                        () =>
                        {
                            var i1 = 1;
                            var i2 = 2;
                            var x = i1 < i2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Cgt_Un,
                        () =>
                        {
                            var i1 = 1;
                            var i2 = 2;
                            var x = i1 > i2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Clt_Un,
                        () =>
                        {
                            var i1 = 1;
                            var i2 = 2;
                            var x = i1 < i2;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Ldloc_0, // and Ldloc_1, Ldloc_2, Ldloc_3
                        () =>
                        {
                            int i1, i2, i3, i4;
                            i1 = 0;
                            i2 = 0;
                            i3 = 0;
                            i4 = 0;
                            var x = i1;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Box,
                        () =>
                        {
                            var i = 1;
                            var x = (object)i;
                        }
                    );
                    yield return TestCase(
                        EmitCode.Newarr,
                        () =>
                        {
                            var x = new int[1];
                        }
                    );
                    yield return TestCase(
                        EmitCode.Newobj,
                        () =>
                        {
                            var x = new DummyClass();
                        }
                    );
                    yield return TestCase(
                        EmitCode.Call,
                        () =>
                        {
                            var x = new DummyClass().DummyMethod();
                        }
                    );
                    yield return TestCase(
                        EmitCode.Initobj,
                        () =>
                        {
                            var x = default(int);
                        }
                    );
                    yield return TestCase(
                        EmitCode.Isinst,
                        () =>
                        {
                            var x = new DummyClass() as object;
                        }
                    );
                    
                    yield break;
                    
                    object[] TestCase(OpCode code, Action body)
                    {
                        return new object[] { code, body };
                    }
                }
            }
        }
    }
}