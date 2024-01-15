// See https://aka.ms/new-console-template for more information

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pose.IL;

namespace Pose.Sandbox
{
    public class Program
    {
        public static async Task<int> GetAsyncInt() => await Task.FromResult(1);

        public static async Task Lol()
        {
            var asyncInt = await GetAsyncInt();
            Console.WriteLine(asyncInt);
        }
        
        public static void Main(string[] args)
        {
            //Lol().GetAwaiter().GetResult();
            
            var shim = Shim
                .Replace(() => Program.GetAsyncInt())
                .With(() => Task.FromResult(2));
            
            PoseContext.IsolateAsync(
                async () =>
                {
                    var @int = await GetAsyncInt();
                    Console.WriteLine(@int);
                }, shim).GetAwaiter().GetResult();
            /*
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
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
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
*/
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