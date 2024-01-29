// See https://aka.ms/new-console-template for more information

using System;
using System.Threading.Tasks;

namespace Pose.Sandbox
{
    public class Program
    {
        internal class MyClass
        {
            public async Task DoSomethingAsync() => await Task.CompletedTask;
        }
        
        public static async Task<int> GetIntAsync()
        {
            Console.WriteLine("Here");
            return await Task.FromResult(1);
        }
        
        public static async Task DoWorkAsync()
        {
            Console.WriteLine("Here");
            await Task.Delay(1000);
        }

        public static async Task Main(string[] args)
        {
            var staticAsyncShim = Shim.Replace(() => DoWorkAsync()).With(
                delegate
                {
                    Console.Write("Don't do work!");
                    return Task.CompletedTask;
                });
            var taskShim = Shim.Replace(() => Is.A<MyClass>().DoSomethingAsync())
                .With(delegate(MyClass @this)
                    {
                        Console.WriteLine("Shimming async Task");
                        return Task.CompletedTask;
                    }
                );
            await PoseContext.Isolate(
                async () =>
                {
                    await DoWorkAsync();
                }, staticAsyncShim
            );

            // #if NET48
//             Console.WriteLine("4.8");
//             var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             PoseContext.Isolate(
//                 () =>
//                 {
//                     Console.WriteLine(DateTime.Now);
//                 }, dateTimeShim);
// #elif NETCOREAPP2_0
//             Console.WriteLine("2.0");
//             var asyncVoidShim = Shim.Replace(() => DoWorkAsync())
//                 .With(
//                     () =>
//                     {
//                         Console.WriteLine("Shimming async Task");
//                         return Task.CompletedTask;
//                     }
//                 );
//             //var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             var asyncShim = Shim.Replace(() => GetIntAsync()).With(() =>
//             {
//                 Console.WriteLine("This actually works!!!");
//                 return Task.FromResult(15);
//             });
//             PoseContext.Isolate(
//                 async () =>
//                 {
//                     var result = await GetIntAsync();
//                     Console.WriteLine($"Result: {result}");
//                     //Console.WriteLine(DateTime.Now);
//                 }, asyncShim);
// #elif NET6_0
//             Console.WriteLine("6.0");
//             var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             PoseContext.Isolate(
//                 () =>
//                 {
//                     Console.WriteLine(DateTime.Now);
//                 }, dateTimeShim);
// #elif NET7_0
//             Console.WriteLine("7.0");
//
//             var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             PoseContext.Isolate(
//                 () =>
//                 {
//                     Console.WriteLine(DateTime.Now);
//                 }, dateTimeShim);
// #elif NETCOREAPP3_0
//             Console.WriteLine("3.0");
//             var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             PoseContext.Isolate(
//                 () =>
//                 {
//                     Console.WriteLine(DateTime.Now);
//                 }, dateTimeShim);
// #else
//             Console.WriteLine("Other");
//             var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
//             PoseContext.Isolate(
//                 () =>
//                 {
//                     Console.WriteLine(DateTime.Now);
//                 }, dateTimeShim);
// #endif

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