using System;
using System.Globalization;
using System.Threading;
using FluentAssertions;
using Pose.Exceptions;
using Xunit;

// ReSharper disable RedundantLambdaParameterType
// ReSharper disable PossibleNullReferenceException

// See: https://stackoverflow.com/a/34876963
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Pose.Tests
{
    public class ShimTests
    {
        public class Methods
        {
            public class StaticTypes
            {
                private class Instance
                {
                    public static string StaticMethod() => null;
                }

                [Fact]
                public void Can_shim_static_method()
                {
                    // Arrange
                    const string shimmedValue = "String";
                    
                    var shim = Shim
                        .Replace(() => Instance.StaticMethod())
                        .With(() => "String");

                    // Act
                    string returnedValue = default;
                    PoseContext.Isolate(
                        () => { returnedValue = Instance.StaticMethod(); },
                        shim
                    );
            
                    // Assert
                    returnedValue.Should().BeEquivalentTo(shimmedValue, because: "that is what the shim is configured to return");
                }
            }

            public class ReferenceTypes
            {
                private class Instance
                {
                    // ReSharper disable once MemberCanBeMadeStatic.Local
                    public string GetString()
                    {
                        return "!";
                    }
                }
                
                [Fact]
                public void Can_shim_method_of_any_instance()
                {
                    // Arrange
                    var action = new Func<Instance, string>((Instance @this) => "String");
                    var shim = Shim.Replace(() => Is.A<Instance>().GetString()).With(action);

                    // Act
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new Instance();
                            dt = instance.GetString();
                        }, shim);
            
                    // Assert
                    dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
                }

                [Fact]
                public void Can_shim_method_of_specific_instance()
                {
                    // Arrange
                    const string configuredValue = "String";
                    
                    var instance = new Instance();
                    var shim = Shim
                        .Replace(() => instance.GetString())
                        .With((Instance _) => configuredValue);

                    // Act
                    string value = default;
                    PoseContext.Isolate(
                        () => { value = instance.GetString(); },
                        shim
                    );
            
                    // Assert
                    value.Should().BeEquivalentTo(configuredValue, because: "that is what the shim is configured to return");
                }
                
                [Fact]
                public void Shims_only_the_method_of_the_specified_instance()
                {
                    // Arrange
                    var shimmedInstance = new Instance();
                    var shim = Shim
                        .Replace(() => shimmedInstance.GetString())
                        .With((Instance @this) => "String");

                    // Act
                    string responseFromShimmedInstance = default;
                    string responseFromNonShimmedInstance = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            responseFromShimmedInstance = shimmedInstance.GetString();
                            var nonShimmedInstance = new Instance();
                            responseFromNonShimmedInstance = nonShimmedInstance.GetString();
                        }, shim);
            
                    // Assert
                    responseFromShimmedInstance.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
                    responseFromNonShimmedInstance.Should().NotBeEquivalentTo("String", because: "the shim is configured for a specific instance");
                    responseFromNonShimmedInstance.Should().BeEquivalentTo("!", because: "that is what the instance returns by default");
                }
            }
            
            public class ValueTypes
            {
                private struct InstanceValue
                {
                    public string GetString() => null;
                }

                [Fact]
                public void Can_shim_instance_method_of_value_type()
                {
                    // Arrange
                    const string configuredValue = "String";
                    var shim = Shim
                        .Replace(() => Is.A<InstanceValue>().GetString())
                        .With((ref InstanceValue @this) => configuredValue);

                    // Act
                    string value = default;
                    PoseContext.Isolate(
                        () => { value = new InstanceValue().GetString(); },
                        shim
                    );
            
                    // Assert
                    value.Should().BeEquivalentTo(configuredValue, because: "that is what the shim is configured to return");
                }

            }
            
            public class AbstractMethods
            {
                private abstract class AbstractBase
                {
                    public virtual string GetStringFromAbstractBase() => "!";

                    public virtual string GetTFromAbstractBase(string input) => "?";
                    
                    public abstract string GetAbstractString();
                }

                private class DerivedFromAbstractBase : AbstractBase
                {
                    public override string GetAbstractString() => throw new NotImplementedException();
                    
                }

                private class ShadowsMethodFromAbstractBase : AbstractBase
                {
                    public override string GetStringFromAbstractBase() => "Shadow";
                    
                    public override string GetAbstractString() => throw new NotImplementedException();
                }
                
                [Fact]
                public void Can_shim_instance_method_of_abstract_type()
                {
                    // Arrange
                    var shim = Shim
                        .Replace(() => Is.A<AbstractBase>().GetStringFromAbstractBase())
                        .With((AbstractBase @this) => "Hello");

                    // Act
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new DerivedFromAbstractBase();
                            dt = instance.GetStringFromAbstractBase();
                        },
                        shim
                    );
                    
                    // Assert
                    dt.Should().BeEquivalentTo("Hello", because: "the shim configured the base class");
                }
                
                [Fact]
                public void Can_shim_instance_method_with_parameters_declared_on_abstract_type()
                {
                    // Arrange
                    var shim = Shim
                        .Replace(() => Is.A<AbstractBase>().GetTFromAbstractBase(Is.A<string>()))
                        .With((AbstractBase @this, string @string) => "Hello");

                    // Act
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new DerivedFromAbstractBase();
                            dt = instance.GetTFromAbstractBase("");
                        },
                        shim
                    );
                    
                    // Assert
                    dt.Should().BeEquivalentTo("Hello", because: "the shim configured the base class");
                }
                
                [Fact]
                public void Can_shim_abstract_method_of_abstract_type()
                {
                    // Arrange
                    const string returnValue = "Hello";
                    
                    var wasCalled = false;
                    var action = new Func<AbstractBase, string>(
                        (AbstractBase @this) =>
                        {
                            wasCalled = true;
                            return returnValue;
                        });
                    var shim = Shim
                        .Replace(() => Is.A<AbstractBase>().GetAbstractString())
                        .With(action);

                    // Act
                    string dt = default;
                    wasCalled.Should().BeFalse(because: "no calls have been made yet");
                    // ReSharper disable once SuggestVarOrType_SimpleTypes
                    Action act = () => PoseContext.Isolate(
                        () =>
                        {
                            var instance = new DerivedFromAbstractBase();
                            dt = instance.GetAbstractString();
                        },
                        shim
                    );
                    
                    // Assert
                    act.Should().NotThrow(because: "the shim works");
                    wasCalled.Should().BeTrue(because: "the shim has been invoked");
                    dt.Should().BeEquivalentTo(returnValue, because: "the shim configured the base class");
                }
                
                [Fact]
                public void Shim_is_not_invoked_if_method_is_overriden_in_derived_type()
                {
                    // Arrange
                    var wasCalled = false;
                    var action = new Func<AbstractBase, string>(
                        (AbstractBase @this) =>
                        {
                            wasCalled = true;
                            return "Hello";
                        });
                    var shim = Shim
                        .Replace(() => Is.A<AbstractBase>().GetStringFromAbstractBase())
                        .With(action);

                    // Act
                    string dt = default;
                    wasCalled.Should().BeFalse(because: "no calls have been made yet");
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new ShadowsMethodFromAbstractBase();
                            dt = instance.GetStringFromAbstractBase();
                        },
                        shim
                    );
                    
                    // Assert
                    var _ = new ShadowsMethodFromAbstractBase();
                    dt.Should().BeEquivalentTo(_.GetStringFromAbstractBase(), because: "the shim configured the base class");
                    wasCalled.Should().BeFalse(because: "the shim was not invoked");
                }
            }

            public class SealedTypes
            {
                private sealed class SealedClass
                {
                    public string GetSealedString() => nameof(GetSealedString);
                }

                [Fact]
                public void Can_shim_method_of_sealed_class()
                {
                    // Arrange
                    var action = new Func<SealedClass, string>((SealedClass @this) => "String");
                    var shim = Shim.Replace(() => Is.A<SealedClass>().GetSealedString()).With(action);

                    // Act
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new SealedClass();
                            dt = instance.GetSealedString();
                        },
                        shim
                    );
            
                    // Assert
                    dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");

                    var sealedClass = new SealedClass();
                    dt.Should().NotBeEquivalentTo(sealedClass.GetSealedString(), because: "that is the original value");
                }
            }
        }
        
        public class Getters
        {
            public class StaticTypes
            {
                private class Instance
                {
                    public static string StaticString { get; set; }
                }

                [Fact]
                public void Can_shim_static_property_getter()
                {
                    // Arrange
                    var shim = Shim
                        .Replace(() => Instance.StaticString)
                        .With(() => "Hello");

                    // Act
                    string value = default;
                    PoseContext.Isolate(() => { value = Instance.StaticString; }, shim);
            
                    // Assert
                    value.Should().BeEquivalentTo("Hello", because: "that is what the shim is configured to return");
                }
            }

            public class ReferenceTypes
            {
                private class Instance
                {
                    public string Text { get; set; }
                }

                [Fact]
                public void Can_shim_property_getter_of_specific_instance()
                {
                    // Arrange
                    var instance = new Instance();
                    var action = new Func<Instance, string>((Instance @this) => "Hello");
                    var shim = Shim.Replace(() => instance.Text).With(action);

                    // Act
                    string dt = default;
                    Instance nonShimmedInstance = default;
                    string textFromNonShimmedInstance = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            dt = instance.Text;
                            
                            nonShimmedInstance = new Instance();
                            textFromNonShimmedInstance = nonShimmedInstance.Text;
                        }, shim);
            
                    // Assert
                    dt.Should().BeEquivalentTo("Hello", because: "that is what the shim is configured to return");

                    textFromNonShimmedInstance.Should().NotBeEquivalentTo(dt, because: "the shim is for a specific instance");
                }
                
                [Fact]
                public void Can_shim_property_getter_of_any_instance()
                {
                    // Arrange
                    var action = new Func<Instance, string>((Instance @this) => "Hello");
                    var shim = Shim.Replace(() => Is.A<Instance>().Text).With(action);

                    // Act
                    string value1 = default;
                    string value2 = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            value1 = new Instance().Text;
                            value2 = new Instance().Text;
                        }, shim);
            
                    // Assert
                    value1.Should().BeEquivalentTo(value2, because: "the shim is configured for any instance");

                    var instance = new Instance();
                    instance.Text.Should().NotBeEquivalentTo(value1, because: "this instance is created outside the isolated code");
                }
            }
            
            public class ValueTypes
            {
                private struct InstanceValue
                {
                    public string Text { get; set; }
                }
                
                [Fact]
                public void Cannot_shim_property_getter_of_specific_instance()
                {
                    // Arrange
                    var instance = new InstanceValue();
                    Action act = () => Shim
                        .Replace(() => instance.Text)
                        .With((ref InstanceValue @this) => string.Empty);

                    // Assert
                    act.Should().Throw<NotSupportedException>(because: "instance methods on specific value type instances cannot be replaced");
                }
                
                [Fact]
                public void Can_shim_property_getter_of_any_instance()
                {
                    // Arrange
                    var shim = Shim
                        .Replace(() => Is.A<InstanceValue>().Text)
                        .With((ref InstanceValue @this) => "Hello");

                    // Act
                    string value1 = default;
                    string value2 = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            value1 = new InstanceValue().Text;
                            value2 = new InstanceValue().Text;
                        }, shim);
            
                    // Assert
                    value1.Should().BeEquivalentTo(value2, because: "the shim is configured for any instance");

                    var instance = new InstanceValue();
                    instance.Text.Should().NotBeEquivalentTo(value1, because: "this instance is created outside the isolated code");
                }
            }

            public class SealedTypes
            {
                private sealed class SealedClass
                {
                    public string SealedString { get; set; } = nameof(SealedString);
                }
                
                [Fact]
                public void Can_shim_property_getter_of_sealed_class()
                {
                    // Arrange
                    var action = new Func<SealedClass, string>((SealedClass @this) => "String");
                    var shim = Shim
                        .Replace(() => Is.A<SealedClass>().SealedString)
                        .With(action);

                    // Act
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new SealedClass();
                            dt = instance.SealedString;
                        },
                        shim
                    );
            
                    // Assert
                    dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
            
                    var sealedClass = new SealedClass();
                    dt.Should().NotBeEquivalentTo(sealedClass.SealedString, because: "that is the original value");
                }
            }
        }
        
        public class Setters
        {
            public class StaticTypes
            {
                private class Instance
                {
                    public static string StaticString { get; set; }
                }

                [Fact]
                public void Can_shim_static_property_setter()
                {
                    // Arrange
                    var wasCalled = false;
                    var shim = Shim
                        .Replace(() => Instance.StaticString, true)
                        .With(new Action<string>(_ => { wasCalled = true; }));

                    // Pre-Act Assert
                    wasCalled.Should().BeFalse(because: "the shim has not been called yet");
                    
                    // Act
                    PoseContext.Isolate(() => { Instance.StaticString = "Hello"; }, shim);
                    
                    // Assert
                    wasCalled.Should().BeTrue(because: "the shim has been called");
                }
            }

            public class ReferenceTypes
            {
                private class Instance
                {
                    public string Text { get; set; }
                }
                
                [Fact]
                public void Can_shim_property_setter_of_any_instance()
                {
                    // Arrange
                    var invocationCount = 0;

                    var shim = Shim
                        .Replace(() => Is.A<Instance>().Text, true)
                        .With((Instance @this, string prop) => { invocationCount++; });

                    // Pre-act assert
                    invocationCount.Should().Be(0, because: "the shim has not been called yet");
                    
                    // Act
                    PoseContext.Isolate(
                        () =>
                        {
                            new Instance().Text = "Hello";
                            new Instance().Text = "Hello";
                        }, shim);
                    
                    // Assert
                    invocationCount.Should().Be(2, because: "the shim was invoked 2 times");
                }
                
                [Fact]
                public void Can_shim_property_setter_of_specific_instance()
                {
                    // Arrange
                    var instance = new Instance();
                    var wasCalled = false;
                    var action = new Action<Instance, string>((Instance @this, string prop) => { wasCalled = true; });
            
                    // Act
                    var shim = Shim.Replace(() => Is.A<Instance>().Text, true).With(action);

                    // Assert
                    wasCalled.Should().BeFalse(because: "the shim has not been called yet");
                    PoseContext.Isolate(() => { instance.Text = "Hello"; }, shim);
                    wasCalled.Should().BeTrue(because: "the shim has been called");
                }
            }
            
            public class ValueTypes
            {
                private struct Instance
                {
                    public string Text { get; set; }
                }
                
                [Fact]
                public void Can_shim_property_setter_of_any_instance()
                {
                    // Arrange
                    var invocationCount = 0;

                    var shim = Shim
                        .Replace(() => Is.A<Instance>().Text, true)
                        .With((ref Instance @this, string prop) => { invocationCount++; });

                    // Pre-act assert
                    invocationCount.Should().Be(0, because: "the shim has not been called yet");
                    
                    // Act
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance1 = new Instance { Text = "Hello" };
                            var instance2 = new Instance { Text = "Hello" };
                        }, shim);
                    
                    // Assert
                    invocationCount.Should().Be(2, because: "the shim was invoked 2 times");
                }
                
                [Fact]
                public void Cannot_shim_property_setter_of_specific_instance()
                {
                    // Arrange
                    var instance = new Instance();
                    Action act = () => Shim
                        .Replace(() => instance.Text, setter: true)
                        .With((ref Instance @this) => string.Empty);

                    // Assert
                    act.Should().Throw<NotSupportedException>(because: "instance methods on specific value type instances cannot be replaced");
                }
            }
            
            public class SealedTypes
            {
                private sealed class SealedClass
                {
                    public string SealedString { get; set; } = nameof(SealedString);
                }
                
                [Fact]
                public void Can_shim_property_setter_of_sealed_class()
                {
                    // Arrange
                    var wasCalled = false;
                    var action = new Action<SealedClass, string>((SealedClass @this, string value) =>
                        {
                            wasCalled = true;
                            @this.SealedString = "Something";
                        }
                    );
                    var shim = Shim
                        .Replace(() => Is.A<SealedClass>().SealedString, setter: true)
                        .With(action);

                    // Act
                    wasCalled.Should().BeFalse(because: "no calls have been made yet");
                    string dt = default;
                    PoseContext.Isolate(
                        () =>
                        {
                            var instance = new SealedClass();
                            instance.SealedString = "!!!";
                            dt = instance.SealedString;
                        },
                        shim
                    );
            
                    // Assert
                    wasCalled.Should().BeTrue(because: "the shim has been invoked");
                    dt.Should().BeEquivalentTo("Something", because: "that is what the shim is configured to return");
            
                    var sealedClass = new SealedClass();
                    dt.Should().NotBeEquivalentTo(sealedClass.SealedString, because: "that is the original value");
                }
            }
        }
        
        public class Constructors
        {
            public class General
            {
                private class Instance
                {
                    public string Text { get; set; }
                }

                [Fact]
                public void Can_invoke_constructor_in_isolation()
                {
                    // Arrange
                    Instance x = null;
                    Action act = () => PoseContext.Isolate(
                        () =>
                        {
                            x = new Instance(); // Specifically this line should *not* fail!
                            x.Text = nameof(Instance.Text);
                        }
                    );
            
                    // Act + Assert
                    act.Should().NotThrow(because: "the constructor can be invoked in isolation");
                    x.Should().NotBeNull(because: "the instance has been initialized in isolation");
                    x.Text.Should().BeEquivalentTo(nameof(Instance.Text), because: "the property was set in isolation");

                }
            }
            
            public class StaticTypes
            {
                private static class Instance
                {
                    public static string Text { get; set; }

                    static Instance()
                    {
                        
                    }
                }

                [Fact(Skip = "Not currently possible")]
                public void Can_shim_constructor_of_sealed_reference_type()
                {
                    // // Arrange
                    // var action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
                    // var shim = Shim
                    //     .Replace(() => new Instance())
                    //     .With(action);
                    //
                    // // Act
                    // Instance dt = default;
                    // PoseContext.Isolate(
                    //     () => { dt = new Instance(); },
                    //     shim
                    // );
                    //
                    // // Assert
                    // dt.Text.Should().BeEquivalentTo(nameof(Instance.Text), because: "that is what the shim is configured to return");
                }

            }

            public class ReferenceTypes
            {
                private class Instance
                {
                    public string Text { get; set; }
                }

                [Fact]
                public void Can_shim_constructor_of_reference_type()
                {
                    // Arrange
                    var action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
                    var shim = Shim
                        .Replace(() => new Instance())
                        .With(action);

                    // Act
                    Instance dt = default;
                    PoseContext.Isolate(
                        () => { dt = new Instance(); },
                        shim
                    );
            
                    // Assert
                    dt.Text.Should().BeEquivalentTo(nameof(Instance.Text), because: "that is what the shim is configured to return");
                }
            }
            
            public class ValueTypes
            {
                private struct Instance
                {
                    public string Text { get; set; }
                }

                [Fact(Skip = "Not supported")]
                public void Can_shim_constructor_of_value_type()
                {
                    // Arrange
                    var action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
                    var shim = Shim
                        .Replace(() => new Instance())
                        .With(action);

                    // Act
                    Instance dt = default;
                    PoseContext.Isolate(
                        () => { dt = new Instance(); },
                        shim
                    );
            
                    // Assert
                    dt.Text.Should().BeEquivalentTo(nameof(Instance.Text), because: "that is what the shim is configured to return");
                }
            }

            public class SealedTypes
            {
                private sealed class Instance
                {
                    public string Text { get; set; }
                }

                [Fact]
                public void Can_shim_constructor_of_sealed_reference_type()
                {
                    // Arrange
                    var action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
                    var shim = Shim
                        .Replace(() => new Instance())
                        .With(action);

                    // Act
                    Instance dt = default;
                    PoseContext.Isolate(
                        () => { dt = new Instance(); },
                        shim
                    );
            
                    // Assert
                    dt.Text.Should().BeEquivalentTo(nameof(Instance.Text), because: "that is what the shim is configured to return");
                }

            }
        }

        public class ShimSignatureValidation
        {
            private class Instance
            {
                public string GetString() => null;

                public int GetInt(string someValue) => int.MinValue;
            }

            [Fact]
            public void Throws_InvalidShimSignatureException_if_the_signature_of_the_replacement_does_not_match()
            {
                // Arrange
                var shimTests = new Instance();
                
                // Act
                Action act = () => Shim.Replace(() => shimTests.GetString()).With(() => { }); // Targets Shim.Replace(Expression<Func<T>>) 
                Action act1 = () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }); // Targets Shim.Replace(Expression<Action>) 
                
                // Assert
                act.Should().Throw<InvalidShimSignatureException>(because: "the signature of the replacement method does not match the original");
                act1.Should().Throw<InvalidShimSignatureException>(because: "the signature of the replacement method does not match the original");
            }
            
            [Fact]
            public void Throws_InvalidShimSignatureException_if_parameter_types_for_the_replacement_do_not_match()
            {
                // Arrange
                var shimTests = new Instance();
                
                // Act
                Action act = () => Shim
                    .Replace(() => shimTests.GetInt(Is.A<string>()))
                    .With((Instance instance, int x) => { return int.MinValue; }); // Targets Shim.Replace(Expression<Func<T>>) 
                
                // Assert
                act.Should().Throw<InvalidShimSignatureException>(because: "the parameter types for the replacement method does not match the original");
            }
            
            [Fact]
            public void Reports_types_when_throwing_InvalidShimSignatureException()
            {
                // Arrange
                var shimTests = new Instance();
                
                // Act
                Action act = () => Shim.Replace(() => shimTests.GetString()).With(() => { }); // Targets Shim.Replace(Expression<Func<T>>) 
                Action act1 = () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }); // Targets Shim.Replace(Expression<Action>) 
                Action act2 = () => Shim.Replace(() => Is.A<DateTime>().Date).With((DateTime @this) => { return new DateTime(2004, 1, 1); }); // Targets Shim.Replace(Expression<Action>) 
                Action act3 = () => Shim.Replace(() => Is.A<DateTime>().Date).With((ref TimeSpan @this) => { return new DateTime(2004, 1, 1); }); // Targets Shim.Replace(Expression<Action>) 
                
                // Assert
                act.Should()
                    .Throw<InvalidShimSignatureException>(because: "the signature of the replacement method does not match the original")
                    .WithMessage("*Expected System.String* Got System.Void");
                act1.Should()
                    .Throw<InvalidShimSignatureException>(because: "the signature of the replacement method does not match the original")
                    .WithMessage("*Expected 1. Got 0*");
                act2.Should()
                    .Throw<InvalidShimSignatureException>(because: "value types must be passed by ref")
                    .WithMessage("*ValueType instances must be passed by ref*");
                act3.Should()
                    .Throw<InvalidShimSignatureException>(because: "the signature of the replacement method does not match the original")
                    .WithMessage("*Expected System.DateTime* Got System.TimeSpan*");
            }
        }

        // [Fact]
        // public void TestShimReplaceWith()
        // {
        //     // Arrange
        //     var shimTests = new Instance();
        //     var action = new Action(() => { });
        //     var actionInstance = new Action<Instance>(s => { });
        //
        //     // Act
        //     var shim = Shim.Replace(() => Console.WriteLine()).With(action);
        //     var shim1 = Shim.Replace(() => shimTests.VoidMethod()).With(actionInstance);
        //
        //     // Assert
        //     var consoleWriteLineMethod = typeof(Console).GetMethod(nameof(Console.WriteLine), Type.EmptyTypes);
        //     shim.Original.Should().BeSameAs(consoleWriteLineMethod);
        //     shim.Replacement.Should().BeSameAs(action);
        //
        //     var voidMethod = typeof(Instance).GetMethod(nameof(Instance.VoidMethod));
        //     shim1.Original.Should().BeSameAs(voidMethod, because: "the shim is configured for this method");
        //     shim1.Instance.Should().BeSameAs(shimTests, because: "the shim is configured for this instance");
        //     shim1.Replacement.Should().BeSameAs(actionInstance, because: "that is the shim's replacement");
        // }

        public class Legacy
        {
            [Fact]
            public void TestReplacePropertyGetter()
            {
                var shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

                var currentCultureGetMethod = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod;
                shim.Original.Should().BeEquivalentTo(currentCultureGetMethod, because: "the shim configures that method");
                shim.Replacement.Should().BeNull(because: "no replacement is configured");
            }

            [Fact]
            public void TestReplacePropertySetter()
            {
                // Arrange
                var shim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true);

                var currentCultureSetMethod = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).SetMethod;
                shim.Original.Should().BeEquivalentTo(currentCultureSetMethod, because: "the shim configures that method");
                shim.Replacement.Should().BeNull(because: "no replacement is configured");
            }

            [Fact]
            public void TestReplacePropertySetterAction()
            {
                // Arrange
                var getterExecuted = false;
                var getterShim = Shim
                    .Replace(() => Is.A<Thread>().CurrentCulture)
                    .With(
                        (Thread t) =>
                        {
                            getterExecuted = true;
                            return t.CurrentCulture;
                        }
                    );

                var setterExecuted = false;
                var setterShim = Shim
                    .Replace(() => Is.A<Thread>().CurrentCulture, true)
                    .With(
                        (Thread t, CultureInfo value) =>
                        {
                            setterExecuted = true;
                            t.CurrentCulture = value;
                        }
                    );

                // Pre-Act asserts
                var currentCultureProperty = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo));
                getterShim.Original.Should().BeEquivalentTo(currentCultureProperty.GetMethod, because: "the shim configures that method");
                setterShim.Original.Should().BeEquivalentTo(currentCultureProperty.SetMethod, because: "the shim configures that method");

                // Act
                PoseContext.Isolate(
                    () =>
                    {
                        var oldCulture = Thread.CurrentThread.CurrentCulture;
                        Thread.CurrentThread.CurrentCulture = oldCulture;
                    },
                    getterShim,
                    setterShim
                );

                // Assert
                getterExecuted.Should().BeTrue(because: "the shim was executed");
                setterExecuted.Should().BeTrue(because: "the shim was executed");
            }
        }
    }
}
