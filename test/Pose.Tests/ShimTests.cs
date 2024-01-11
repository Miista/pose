using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using Pose.Exceptions;
using Pose.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Console;

namespace Pose.Tests
{
    [TestClass]
    public class ShimTests
    {
        [TestMethod]
        public void TestReplace()
        {
            Shim shim = Shim.Replace(() => Console.WriteLine(""));

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplaceWithInstanceVariable()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim.Original);
            Assert.AreSame(shimTests, shim.Instance);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestShimReplaceWithInvalidSignature()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => shimTests.TestReplace()).With(() => { }));
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }));
        }

        [TestMethod]
        public void TestShimReplaceWith()
        {
            ShimTests shimTests = new ShimTests();
            Action action = new Action(() => { });
            Action<ShimTests> actionInstance = new Action<ShimTests>((s) => { });

            Shim shim = Shim.Replace(() => Console.WriteLine()).With(action);
            Shim shim1 = Shim.Replace(() => shimTests.TestReplace()).With(actionInstance);

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", Type.EmptyTypes), shim.Original);
            Assert.AreEqual(action, shim.Replacement);

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim1.Original);
            Assert.AreSame(shimTests, shim1.Instance);
            Assert.AreEqual(actionInstance, shim1.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertyGetter()
        {
            Shim shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertySetter()
        {
            Shim shim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true);

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
            Func<DateTime> action = new Func<DateTime>(() => new DateTime(2004, 1, 1));
            Shim shim = Shim.Replace(() => DateTime.Now).With(action);

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
            Func<Instance, string> action = new Func<Instance, string>((Instance @this) => "Hello");
            Shim shim = Shim.Replace(() => instance.Text).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = instance.Text; }, shim);
            
            Assert.AreEqual("Hello", dt);
        }
        
        [TestMethod]
        public void Can_shim_instance_property_setter()
        {
            var instance = new Instance();
            var wasCalled = false;
            Action<Instance, string> action = new Action<Instance, string>((Instance @this, string prop) => { wasCalled = true; });
            Shim shim = Shim.Replace(() => Is.A<Instance>().Text, true).With(action);

            Assert.IsFalse(wasCalled);
            PoseContext.Isolate(() => { instance.Text = "Hello"; }, shim);
            Assert.IsTrue(wasCalled);
        }
        
        [TestMethod]
        public void Can_shim_static_property_getter()
        {
            Func<string> action = new Func<string>(() => "Hello");
            Shim shim = Shim.Replace(() => Instance.StaticString).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = Instance.StaticString; }, shim);
            
            Assert.AreEqual("Hello", dt);
        }

        [TestMethod]
        public void Can_shim_static_property_setter()
        {
            var wasCalled = false;
            Action<string> action = new Action<string>(prop => { wasCalled = true; });
            Shim shim = Shim.Replace(() => Instance.StaticString, true).With(action);

            Assert.IsFalse(wasCalled);
            PoseContext.Isolate(() => { Instance.StaticString = "Hello"; }, shim);
            Assert.IsTrue(wasCalled);
        }
        
        [TestMethod]
        public void Can_shim_instance_method()
        {
            Func<Instance, string> action = new Func<Instance, string>((Instance @this) => "String");
            Shim shim = Shim.Replace(() => Is.A<Instance>().GetString()).With(action);

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
            Shim shim = Shim.Replace(() => Is.A<InstanceValue>().GetString()).With(
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
            Func<Instance, string> action = new Func<Instance, string>((Instance @this) => "String");
            Shim shim = Shim.Replace(() => instance.GetString()).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = instance.GetString(); }, shim);
            
            Assert.AreEqual("String", dt);
        }
        
        [TestMethod]
        public void Can_shim_static_method()
        {
            Func<string> action = new Func<string>(() => "String");
            Shim shim = Shim.Replace(() => Instance.StaticMethod()).With(action);

            string dt = default;
            PoseContext.Isolate(() => { dt = Instance.StaticMethod(); }, shim);
            
            Assert.AreEqual("String", dt);
        }
        
        [TestMethod]
        public void Can_shim_constructor()
        {
            Func<Instance> action = new Func<Instance>(() => new Instance(){Text = nameof(Instance.Text)});
            Shim shim = Shim.Replace(() => new Instance()).With(action);

            Instance dt = default;
            PoseContext.Isolate(() => { dt = new Instance(); }, shim);
            
            Assert.AreEqual(nameof(Instance.Text), dt.Text);
        }
    }
}
