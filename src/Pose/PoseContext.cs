using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Pose.IL;

namespace System.Runtime.CompilerServices
{
    // AsyncVoidMethodBuilder.cs in your project
    public class AsyncTaskMethodBuilder
    {
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine
        )
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            
        }
        
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) {}
        
        public void SetException(Exception exception) {}
        
        public Task Task => null;

        public AsyncTaskMethodBuilder()
            => Console.WriteLine(".ctor");
 
        public static AsyncTaskMethodBuilder Create()
            => new AsyncTaskMethodBuilder();
 
        public void SetResult() => Console.WriteLine("SetResult");
 
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            Console.WriteLine("Start");
            var methodInfos = stateMachine.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfos[0], false);
            var methodBase = methodRewriter.Rewrite();
            stateMachine.MoveNext();
        }
 
        // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
        // and SetStateMachine are empty
    }   
}

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { private set; get; }
        internal static Dictionary<MethodBase, DynamicMethod> StubCache { private set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            var delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
            methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
        
        public static async Task IsolateAsync(Func<Task> entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                await entryPoint.Invoke();
                return;
            }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            var delegateType = typeof(Func<Task>); //.MakeGenericType(entryPoint.Target.GetType());
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
            var @delegate = methodInfo.CreateDelegate(delegateType);
            @delegate.DynamicInvoke();
        }
    }
}