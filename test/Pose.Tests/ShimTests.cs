using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Pose.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
// ReSharper disable RedundantLambdaParameterType

namespace Pose.Tests
{
    [TestClass]
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

        [TestMethod]
        public void TestReplacePropertyGetter()
        {
            var shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertySetter()
        {
            var shim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).SetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }
        
        
        [TestMethod]
        [Ignore]
        public void TestReplacePropertySetterAction()
        {
            var getterExecuted = false;
            var getterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture)
                .With((Thread t) =>
                {
                    getterExecuted = true;
                    return t.CurrentCulture;
                });
            var setterExecuted = false;
            var setterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true)
                .With((Thread t, CultureInfo value) =>
                {
                    setterExecuted = true;
                    t.CurrentCulture = value;
                });

            var currentCultureProperty = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo));
            Assert.AreEqual(currentCultureProperty.GetMethod, getterShim.Original);
            Assert.AreEqual(currentCultureProperty.SetMethod, setterShim.Original);

            PoseContext.Isolate(() =>
            {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }, getterShim, setterShim);

            Assert.IsTrue(getterExecuted, "Getter not executed");
            Assert.IsTrue(setterExecuted, "Setter not executed");
        }
        
        [TestMethod]
        public void Can_shim_static_property()
        {
            var action = new Func<DateTime>(() => new DateTime(2004, 1, 1));
            var shim = Shim.Replace(() => DateTime.Now).With(action);

            DateTime dt = default;
            PoseContext.Isolate(() => { dt = DateTime.Now; }, shim);
            
            Assert.AreEqual(2004, dt.Year);
            Assert.AreEqual(1, dt.Day);
            Assert.AreEqual(1, dt.Month);
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
            var instance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "Hello");
            var shim = Shim.Replace(() => instance.Text).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = instance.Text; }, shim);
            
            Assert.AreEqual("Hello", dt);
        }
        
        [TestMethod]
        public void Can_shim_instance_property_setter()
        {
            var instance = new Instance();
            var wasCalled = false;
            var action = new Action<Instance, string>((Instance @this, string prop) => { wasCalled = true; });
            var shim = Shim.Replace(() => Is.A<Instance>().Text, true).With(action);

            Assert.IsFalse(wasCalled);
            PoseContext.Isolate(() => { instance.Text = "Hello"; }, shim);
            Assert.IsTrue(wasCalled);
        }
        
        [TestMethod]
        public void Can_shim_static_property_getter()
        {
            var action = new Func<string>(() => "Hello");
            var shim = Shim.Replace(() => Instance.StaticString).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = Instance.StaticString; }, shim);
            
            Assert.AreEqual("Hello", dt);
        }

        [TestMethod]
        public void Can_shim_static_property_setter()
        {
            var wasCalled = false;
            var action = new Action<string>(prop => { wasCalled = true; });
            var shim = Shim.Replace(() => Instance.StaticString, true).With(action);

            Assert.IsFalse(wasCalled);
            PoseContext.Isolate(() => { Instance.StaticString = "Hello"; }, shim);
            Assert.IsTrue(wasCalled);
        }
        
        [TestMethod]
        public void Can_shim_instance_method()
        {
            var action = new Func<Instance, string>((Instance @this) => "String");
            var shim = Shim.Replace(() => Is.A<Instance>().GetString()).With(action);

            string dt = default;
            PoseContext.Isolate(
                () =>
                {
                    var instance = new Instance();
                    dt = instance.GetString();
                }, shim);
            
            Assert.AreEqual("String", dt);
        }
        
        [TestMethod]
        public void Can_shim_instance_method_of_value_type()
        {
            var shim = Shim.Replace(() => Is.A<InstanceValue>().GetString()).With(
                delegate(ref InstanceValue @this) { return "String"; });

            string dt = default;
            PoseContext.Isolate(
                () =>
                {
                    var instance = new InstanceValue();
                    dt = instance.GetString();
                }, shim);
            
            Assert.AreEqual("String", dt);
        }
        
        [TestMethod]
        public void Can_shim_instance_method_of_specific_instance()
        {
            var instance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "String");
            var shim = Shim.Replace(() => instance.GetString()).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = instance.GetString(); }, shim);
            
            Assert.AreEqual("String", dt);
        }
                
        [TestMethod]
        public void Can_shim_instance_method_of_specific_instance_1()
        {
            var instance = new Instance();
            var action = new Func<Instance, string>((Instance @this) => "String");
            var shim = Shim.Replace(() => instance.GetString()).With(action);

            string dt1 = default;
            string dt2 = default;
            PoseContext.Isolate(
                () =>
                {
                    dt1 = instance.GetString();
                    var instance2 = new Instance();
                    dt2 = instance2.GetString();
                }, shim);
            
            Assert.AreEqual("String", dt1);
            Assert.AreEqual("!", dt2);
        }
        
        [TestMethod]
        public void Can_shim_static_method()
        {
            var action = new Func<string>(() => "String");
            var shim = Shim.Replace(() => Instance.StaticMethod()).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = Instance.StaticMethod(); }, shim);
            
            Assert.AreEqual("String", dt);
        }
        
        [TestMethod]
        public void Can_shim_constructor()
        {
            var action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
            var shim = Shim.Replace(() => new Instance()).With(action);

            Instance dt = default;
            PoseContext.Isolate(() => { dt = new Instance(); }, shim);
            
            Assert.AreEqual(nameof(Instance.Text), dt.Text);
        }
        
        [TestMethod]
        public void Can_invoke_constructor_in_isolation()
        {
            try
            {
                Instance x = null;
                PoseContext.Isolate(
                    () =>
                    {
                        x = new Instance();
                        x.Text = nameof(Instance.Text);
                    });
                Assert.IsNotNull(x);
                Assert.AreEqual(nameof(Instance.Text), x.Text);
            }
            catch (Exception e)
            {
                throw;
            }
         
            Assert.IsTrue(true);
        }
    }
}
