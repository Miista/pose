// See https://aka.ms/new-console-template for more information

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Pose.IL;

namespace Pose.Sandbox
{
    // namespace System.Runtime.CompilerServices
    // {
    //     // AsyncVoidMethodBuilder.cs in your project
    //     public class AsyncTaskMethodBuilder
    //     {
    //         public void AwaitOnCompleted<TAwaiter, TStateMachine>(
    //             ref TAwaiter awaiter,
    //             ref TStateMachine stateMachine
    //         )
    //             where TAwaiter : INotifyCompletion
    //             where TStateMachine : IAsyncStateMachine
    //         {
    //         
    //         }
    //     
    //         public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
    //             ref TAwaiter awaiter, ref TStateMachine stateMachine)
    //             where TAwaiter : ICriticalNotifyCompletion
    //             where TStateMachine : IAsyncStateMachine
    //         {
    //         
    //         }
    //
    //         public void SetStateMachine(IAsyncStateMachine stateMachine) {}
    //     
    //         public void SetException(Exception exception) {}
    //     
    //         public Task Task => null;
    //
    //         public AsyncTaskMethodBuilder()
    //             => Console.WriteLine(".ctor");
    //
    //         public static AsyncTaskMethodBuilder Create()
    //             => new AsyncTaskMethodBuilder();
    //
    //         public void SetResult() => Console.WriteLine("SetResult");
    //
    //         public void Start<TStateMachine>(ref TStateMachine stateMachine)
    //             where TStateMachine : IAsyncStateMachine
    //         {
    //             Console.WriteLine("Start");
    //             var methodInfos = stateMachine.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
    //             var methodRewriter = MethodRewriter.CreateRewriter(methodInfos[0], false);
    //             var methodBase = methodRewriter.Rewrite();
    //             stateMachine.MoveNext();
    //         }
    //
    //         // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
    //         // and SetStateMachine are empty
    //     }   
    // }
    
    public class Program
    {
        public static async Task<int> GetAsyncInt()
        {
            await Task.Delay(1000);
            return await Task.FromResult(1);
        }

        public static async Task Lol()
        {
            var asyncInt = await GetAsyncInt();
            Console.WriteLine(asyncInt);
        }
        
        public static void Main(string[] args)
        {
            //Lol().GetAwaiter().GetResult();
            // var i = 0;
            // var type = i.GetType();
            // MethodBase methodBase = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            // Console.WriteLine(methodBase.DeclaringType.Name);
            // var methodRewriter = MethodRewriter.CreateRewriter(methodBase, false);
            // var method = (MethodInfo)methodRewriter.Rewrite();
            // var returnTypeName = method.ReturnType.Name;
            // var @delegate = method.CreateDelegate(typeof(void));
            //methodBase.Invoke(methodBase, new object[] { methodBase });
            //Console.WriteLine(type);
            var shim = Shim
                .Replace(() => Program.GetAsyncInt())
                .With(() => Task.FromResult(2));

            //var startMethod = typeof(Exception).Assembly.GetTypes().Where(t => t.Name.Contains("AsyncMethodBuilderCore")).FirstOrDefault().GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name.Contains("Start")).FirstOrDefault();
            //new Shim(startMethod, null).With()
            PoseContext.Shims = new Shim[] { shim };
            
            // var shim1 = Shim
            //     .Replace(() => System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create())
            //     .With(() => System.Runtime.CompilerServices1.AsyncTaskMethodBuilder.Create());
            
            PoseContext.IsolateAsync(
                async () =>
                {
                    var @int = await GetAsyncInt();
                    Console.WriteLine(@int);
                });
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