using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Pose.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

// ReSharper disable RedundantLambdaParameterType
// ReSharper disable PossibleNullReferenceException

namespace Pose.Tests
{
    public class ShimTests
    {
        [Fact]
        public void Can_replace_static_method()
        {
            // Act
            var shim = Shim.Replace(() => Console.WriteLine(""));

            // Assert
            var consoleWriteLineMethodInfo = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });
            consoleWriteLineMethodInfo.Should().BeSameAs(shim.Original.As<MethodInfo>(), because: "the shim represents that method");
            shim.Replacement.Should().BeNull(because: "no replacement has been specified");
        }

        [Fact]
        public void Can_replace_instance_method_of_specific_instance()
        {
            // Arrange
            var instance = new Instance();

            // Act
            var shim = Shim.Replace(() => instance.GetString());
            
            // Assert
            var methodInfo = typeof(Instance).GetMethod(nameof(Instance.GetString));

            shim.Original.Should().Be(methodInfo, because: "the shim represents that method");
            shim.Instance.Should().BeSameAs(instance, because: "the shim is configured for this specific instance");
            shim.Replacement.Should().BeNull(because: "no replacement has been specified");
            shim.Instance.Should().NotBeSameAs(new Instance(), because: "the shim is configured for a specific instance");
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

        [Fact]
        public void TestShimReplaceWith()
        {
            // Arrange
            var shimTests = new Instance();
            var action = new Action(() => { });
            var actionInstance = new Action<Instance>(s => { });

            // Act
            var shim = Shim.Replace(() => Console.WriteLine()).With(action);
            var shim1 = Shim.Replace(() => shimTests.VoidMethod()).With(actionInstance);

            // Assert
            var consoleWriteLineMethod = typeof(Console).GetMethod(nameof(Console.WriteLine), Type.EmptyTypes);
            shim.Original.Should().BeSameAs(consoleWriteLineMethod);
            shim.Replacement.Should().BeSameAs(action);

            var voidMethod = typeof(Instance).GetMethod(nameof(Instance.VoidMethod));
            shim1.Original.Should().BeSameAs(voidMethod, because: "the shim is configured for this method");
            shim1.Instance.Should().BeSameAs(shimTests, because: "the shim is configured for this instance");
            shim1.Replacement.Should().BeSameAs(actionInstance, because: "that is the shim's replacement");
        }

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
                .With((Thread t) =>
                {
                    getterExecuted = true;
                    return t.CurrentCulture;
                });
            
            var setterExecuted = false;
            var setterShim = Shim
                .Replace(() => Is.A<Thread>().CurrentCulture, true)
                .With((Thread t, CultureInfo value) =>
                {
                    setterExecuted = true;
                    t.CurrentCulture = value;
                });

            // Pre-Act asserts
            var currentCultureProperty = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo));
            getterShim.Original.Should().BeEquivalentTo(currentCultureProperty.GetMethod, because: "the shim configures that method");
            setterShim.Original.Should().BeEquivalentTo(currentCultureProperty.SetMethod, because: "the shim configures that method");

            // Act
            PoseContext.Isolate(() =>
            {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }, getterShim, setterShim);

            // Assert
            getterExecuted.Should().BeTrue(because: "the shim was executed");
            setterExecuted.Should().BeTrue(because: "the shim was executed");
        }
        
        [TestMethod]
        public void Can_shim_static_property()
        {
            // Arrange
            var action = new Func<DateTime>(() => new DateTime(2004, 1, 1));
            var shim = Shim
                .Replace(() => DateTime.Now)
                .With(action);

            // Act
            DateTime dt = default;
            PoseContext.Isolate(
                () => { dt = DateTime.Now; },
                shim
            );
            
            // Assert
            dt.Should().BeCloseTo(new DateTime(2004, 1, 1), TimeSpan.Zero, because: "that is what the shim returns");
        }

        private class Instance
        {
            public string Text { get; set; } = "_";
            
            public string GetString()
            {
                return "!";
            }

            public static string StaticString { get; set; } = "?";

            public static string StaticMethod()
            {
                return "0";
            }

            public void VoidMethod() { }
        }
        
        private struct InstanceValue
        {
            public string GetString()
            {
                return "!";
            }
        }
        
        [TestMethod]
        public void Can_shim_instance_property_getter()
        {
            // Arrange
            var instance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "Hello");
            var shim = Shim.Replace(() => instance.Text).With(action);

            // Act
            string dt = default;
            PoseContext.Isolate(() => { dt = instance.Text; }, shim);
            
            // Assert
            dt.Should().BeEquivalentTo("Hello", because: "that is what the shim is configured to return");
        }
        
        [TestMethod]
        public void Can_shim_instance_property_setter()
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
        
        [TestMethod]
        public void Can_shim_static_property_getter()
        {
            // Arrange
            var action = new Func<string>(() => "Hello");
            var shim = Shim.Replace(() => Instance.StaticString).With(action);

            // Act
            string dt = default;
            PoseContext.Isolate(() => { dt = Instance.StaticString; }, shim);
            
            // Assert
            dt.Should().BeEquivalentTo("Hello", because: "that is what the shim is configured to return");
        }

        [TestMethod]
        public void Can_shim_static_property_setter()
        {
            // Arrange
            var wasCalled = false;
            var action = new Action<string>(prop => { wasCalled = true; });
            var shim = Shim.Replace(() => Instance.StaticString, true).With(action);

            // Act + Assert
            wasCalled.Should().BeFalse(because: "the shim has not been called yet");
            PoseContext.Isolate(() => { Instance.StaticString = "Hello"; }, shim);
            wasCalled.Should().BeTrue(because: "the shim has been called");
        }
        
        [TestMethod]
        public void Can_shim_instance_method()
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
        
        [TestMethod]
        public void Can_shim_instance_method_of_value_type()
        {
            // Arrange
            var shim = Shim
                .Replace(() => Is.A<InstanceValue>().GetString())
                .With(delegate(ref InstanceValue @this) { return "String"; });

            // Act
            string dt = default;
            PoseContext.Isolate(
                () => { dt = new InstanceValue().GetString(); },
                shim
            );
            
            // Assert
            dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
        }
        
        [TestMethod]
        public void Can_shim_instance_method_of_specific_instance()
        {
            // Arrange
            var instance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "String");
            var shim = Shim.Replace(() => instance.GetString()).With(action);

            // Act
            string dt = default;
            PoseContext.Isolate(
                () => { dt = instance.GetString(); },
                shim
            );
            
            // Assert
            dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
        }
                
        [TestMethod]
        public void Shims_only_the_method_of_the_specified_instance()
        {
            // Arrange
            var shimmedInstance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "String");
            var shim = Shim.Replace(() => shimmedInstance.GetString()).With(action);

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
        
        [TestMethod]
        public void Can_shim_static_method()
        {
            // Arrange
            var action = new Func<string>(() => "String");
            var shim = Shim.Replace(() => Instance.StaticMethod()).With(action);

            // Act
            string dt = default;
            PoseContext.Isolate(
                () => { dt = Instance.StaticMethod(); },
                shim
            );
            
            // Assert
            dt.Should().BeEquivalentTo("String", because: "that is what the shim is configured to return");
        }
        
        [TestMethod]
        public void Can_shim_constructor()
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
}
