// See https://aka.ms/new-console-template for more information

using System;

namespace Pose.Sandbox
{
    public class Program
    {
        static void Constrain<TT>(TT a) where TT : IA{
            Console.WriteLine(a.GetString());
        }

        static void Box<TT>(TT a) where TT : B{
            Console.WriteLine(a.GetInt());
        }

        interface IA {
            string GetString();
        }
        
        abstract class B {
            public int GetInt(){return 0;}
        }
        class A : B, IA {
            public string GetString() => "Hello, World";
        }

        public class OverridenOperatorClass
        {
            public static explicit operator bool(OverridenOperatorClass c) => false;

            public static implicit operator int(OverridenOperatorClass c) => int.MinValue;

            public static OverridenOperatorClass operator +(OverridenOperatorClass l, OverridenOperatorClass r) => default(OverridenOperatorClass);
        }
        
        public static void Main(string[] args)
        {
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
            var with1 = Shim.Replace(() => Is.A<A>().GetString()).With(delegate(A x) { return "Hey";});
            var with2 = Shim.Replace(() => Is.A<IA>().GetString()).With(delegate(IA x) { return "Hey";});
            PoseContext.Isolate(
                () =>
                {
                    var a = new A();
                    // Box(a);
                    Constrain(a);
                }, with, with1, with2);
            
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