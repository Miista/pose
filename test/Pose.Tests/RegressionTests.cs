using System;
using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;
using DateTime = System.DateTime;

namespace Pose.Tests
{
    public class RegressionTests
    {
        private enum TestEnum { A }
        
        [Fact(DisplayName = "Enum.IsDefined cannot be called from within PoseContext.Isolate #26")]
        public void Can_call_EnumIsDefined_from_Isolate()
        {
            // Arrange
            var shim = Shim
                .Replace(() => new DateTime(2024, 2, 2))
                .With((int year, int month, int day) => new DateTime(2004, 1, 1));
            var isDefined = false;
            
            // Act
            PoseContext.Isolate(
                () =>
                {
                    isDefined = Enum.IsDefined(typeof(TestEnum), nameof(TestEnum.A));
                }, shim);
            
            // Assert
            isDefined.Should().BeTrue(because: "Enum.IsDefined can be called from Isolate");
        }

        [Fact(DisplayName = "Problem accessing Linq methods (extension methods of IQueryable) inside isolated code #54")]
        public void Can_devirtualize_methods()
        {
            // Arrange
            var fooObject = new FooObject("bum");
            fooObject.FooStatic();
            var shimmedValue = new byte[1];

            var fooStaticShim = Shim
                .Replace(() => fooObject.FooRecall())
                .With((FooObject _) => shimmedValue);

            var lolObject = TopObject.Lol;

            // just to make sure the method works
            var calledOutsideIsolation = lolObject.LolMethod();

            // Act
            var calledInIsolation = Array.Empty<byte>();
            PoseContext.Isolate(() =>
            {
                calledInIsolation = lolObject.LolMethod();
            }, fooStaticShim);

            // Assert
            calledInIsolation.Should().BeEquivalentTo(shimmedValue, because: "that is the value we shimmed");
            calledOutsideIsolation.Should().BeEquivalentTo(Array.Empty<byte>(), because: "the method was not shimmed");
        }
    }

    internal static class FooExtensions
    {
        public static byte[] FooStatic(this FooObject obj)
        {
            Console.WriteLine(obj.Attr + "_original");
            return new byte[0];
        }

        public static byte[] FooRecall(this FooObject obj) => FooStatic(obj);
    }

    public class LolObject
    {
        public byte[] LolMethod()
        {
            var a = new CollectionClass();
            var fooObject = new FooObject("lol");
            Console.WriteLine("a");
            CollectionClass.CollectionClassCollection["foo"] =
                new ConcurrentDictionary<ICollectionClass, ICollectionClass> { [a] = new CollectionClass() };
            Console.WriteLine("b");
            var foo = CollectionClass.SomeStaticMethod(a);
            Console.WriteLine("c");
            return fooObject.FooRecall();
        }
    }

    public class CollectionClass : ICollectionClass
    {
        internal static ConcurrentDictionary<string, ConcurrentDictionary<ICollectionClass, ICollectionClass>> CollectionClassCollection =
            new();

        public static ICollectionClass SomeStaticMethod(ICollectionClass obj)
        {
            Console.WriteLine(CollectionClassCollection == null);
            Console.WriteLine(CollectionClassCollection?.ContainsKey("foo") ?? true);
            
            if (CollectionClassCollection["foo"].TryGetValue(obj, out ICollectionClass returnedObj))
            {
                Console.WriteLine("Here");
                return returnedObj;
            }

            Console.WriteLine("Not here");

            return null;
        }
    }

    public interface ICollectionClass { }

    public static class TopObject
    {
        private static LolObject lol;

        public static LolObject Lol => lol ??= new LolObject();
    }

    public class FooObject
    {
        public string Attr { get; set; }

        public FooObject(string attr)
        {
            Attr = attr;
        }
    }
}