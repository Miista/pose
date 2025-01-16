// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pose.Sandbox
{
    public static class TClass
    {
        public static int Get(this List<int> list)
        {
            return 0;
        }
    }
    
    public class Program
    {
        static void Constrain<TT>(TT a) where TT : IA{
            Console.WriteLine(a.GetString());
        }
        
        static void ConstrainD<TT>(TT a) where TT : D{
            Console.WriteLine(a.GetString2());
        }

        static void Box<TT>(TT a) where TT : B{
            Console.WriteLine(a.GetInt());
        }
        
        static void BoxD<TT>(TT a) where TT : B{
            Console.WriteLine(a.GetString2());
        }

        interface IA {
            string GetString();
        }
        
        abstract class B : D {
            public int GetInt(){return 0;}
        }

        abstract class D
        {
            public string GetString2() => "Wuu?";
        }
        
        class A : B, IA {
            public string GetString() => "Hello, World";
        }

        struct C : IA
        {
            public string GetString() => "Wee";
        }

        public class OverridenOperatorClass
        {
            public static explicit operator bool(OverridenOperatorClass c) => false;

            public static implicit operator int(OverridenOperatorClass c) => int.MinValue;

            public static OverridenOperatorClass operator +(OverridenOperatorClass l, OverridenOperatorClass r) => default(OverridenOperatorClass);
        }
        
         public static IQueryable<int> GetInts()
         {
             return new List<int>().AsQueryable();
         }
             
        public static void Main(string[] args)
        {
            var countShim = Shim
                .Replace(() => Is.A<int[]>().Count())
                .With((IEnumerable<int> ts) => 0);
            
            var getIntsShim = Shim
                .Replace(() => Program.GetInts())
                .With(() => new List<int> { 1 }.AsQueryable());

            var tt = Shim
                .Replace(() => Is.A<List<int>>().Get())
                .With((List<int> list) => 20);

            PoseContext.Isolate(
                () =>
                {
                    var xs = new int[] { 0, 1, 2 };
                    Console.WriteLine("X: " + xs.Length);
                    Console.WriteLine("Y: " + xs.Count());

                    var iis = Program.GetInts();
                    Console.WriteLine(iis.LongCount());

                    //Console.WriteLine("X: " + Program.GetInts().Count());
                }, getIntsShim, countShim);
            return;
                         
#if NET48
            Console.WriteLine("4.8");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NETCOREAPP2_0
            Console.WriteLine("2.0");

            var with = Shim.Replace(() => Is.A<B>().GetInt()).With(delegate(B x) { return 5;});
            var with1 = Shim.Replace(() => Is.A<D>().GetString2()).With(delegate(D x) { return "HeyD";});
            var with2 = Shim.Replace(() => Is.A<A>().GetString()).With(delegate(A x) { return "Hey";});
            var shim = Shim.Replace(() => Is.A<C>().GetString()).With(delegate(ref C @this) { return "Hey2"; });

            // var with2 = Shim.Replace(() => Is.A<IA>().GetString()).With(delegate(IA x) { return "Hey";});
            PoseContext.Isolate(
                () =>
                {
                    var a = new A();
                    // Box(a);
                    Console.WriteLine(a.GetString());
                    Constrain(a);
                    ConstrainD(a);
                    BoxD(a);

                    var c = new C();
                    Console.WriteLine(c.GetString());
                    Constrain(c);
                }, with, with1, shim, with2);
            
            // var sut1 = new OverridenOperatorClass();
            // int s = sut1;
            // Shim.Replace(() => Is.A<OverridenOperatorClass>() + Is.A<OverridenOperatorClass>())
            //     .With(delegate(OverridenOperatorClass l, int r) { return default(OverridenOperatorClass); });
            // var operatorShim = Shim.Replace(() => (bool) sut1)
            //     .With(delegate (OverridenOperatorClass c) { return true; });
            // var dateTimeAddShim = Shim.Replace(() => Is.A<DateTime>() + Is.A<TimeSpan>())
            //     .With(delegate(DateTime dt, TimeSpan ts) { return new DateTime(2004, 01, 01); });
            // var dateTimeSubtractShim = Shim.Replace(() => Is.A<DateTime>() - Is.A<TimeSpan>())
            //     .With(delegate(DateTime dt, TimeSpan ts) { return new DateTime(1990, 01, 01); });
            //
            // PoseContext.Isolate(
            //     () =>
            //     {
            //         var dateTime = DateTime.Now;
            //         Console.WriteLine($"Date: {dateTime}");
            //         var ts = TimeSpan.FromSeconds(1);
            //         Console.WriteLine($"Time: {ts}");
            //
            //         var time = dateTime + ts;
            //         Console.WriteLine($"Result1: {time}");
            //
            //         var time2 = dateTime - ts;
            //         Console.WriteLine($"Result2: {time2}");
            //     }, dateTimeAddShim, dateTimeSubtractShim
            // );
#elif NET6_0
            Console.WriteLine("6.0");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NET7_0
            Console.WriteLine("7.0");

            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NETCOREAPP3_0
            Console.WriteLine("3.0");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#else
            Console.WriteLine("Other");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#endif

            // var dateTimeShim = Shim.Replace(() => T.I).With(() => "L");
            // var dateTimeShim1 = Shim.Replace(() => T.Get()).With(() => "Word");
            // var inst = new Inst();
            // var f = new Func<Inst, string>(i => "Word");
            // var dateTimeShim2 = Shim.Replace(() => inst.S).With(f);
            // var dateTimeShim3 = Shim.Replace(() => inst.Get()).With(f);
            // var dateTimeShim4 = Shim.Replace(() => Is.A<Inst>().S).With(f);
            // var dateTimeShim5 = Shim.Replace(() => Is.A<Inst>().Get()).With(f);
            // var dateTimeShim6 = Shim.Replace(() => Is.A<Inst>().Get()).With(delegate(Inst @this) { return "Word"; });
            //
            // PoseContext.Isolate(
            //     () =>
            //     {
            //         // Console.Write(T.I);
            //         // Console.WriteLine(T.Get());
            //         try
            //         {
            //             Console.WriteLine(inst.S);
            //         }
            //         catch (Exception e) { }
            //         finally { }
            //
            //         // Console.WriteLine(T.I);
            //     }, dateTimeShim, dateTimeShim4);
        }
    }

    public class Inst
    {
        public string S { get; set; } = "_";

        public string Get()
        {
            return "h";
        }
    }
    
    public static class T
    {
        public static string I
        {
            get { return "H"; }
        }

        public static string Get() => "Hello";
    }
}