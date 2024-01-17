using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Pose.IL;

namespace System.Runtime.CompilerServices1
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
            var methodInfo = methodInfos[0];
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var methodBase = methodRewriter.Rewrite();
            methodBase.Invoke(this, new object[] { stateMachine });
            stateMachine.MoveNext();
        }
    
        // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
        // and SetStateMachine are empty
    }   
}

namespace Pose
{
        /// <summary>
    /// A helper class to run Async code from a synchronize methods
    /// </summary>
    /// <remarks>
    /// Use this helper when your method isn't decorated with 'async', so you can't implement 'await' on the call to the async-method.
    /// </remarks>
    public static class AsyncHelper
    {
        private static readonly TaskFactory MyTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Call this method when you need the result back from the async-method you are calling.
        /// </summary>
        /// <example>
        /// <code>
        /// var result = AsyncHelper.RunASync&lt;bool&gt;(() => IsValueTrueAsync(true));
        /// </code>
        /// </example>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The function to run.</param>
        /// <returns>The result from running <paramref name="func"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="func"/> is null.</exception>
        public static TResult RunASync<TResult>(Func<Task<TResult>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            return MyTaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
        
        /// <summary>
        /// Call this method when you don't need any result back
        /// </summary>
        /// <example>
        /// <code>
        /// AsyncHelper.RunASync(() => Save(person));
        /// </code>
        /// </example>
        /// <param name="func">The function to run.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="func"/> is null.</exception>
        public static void RunASync(Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            MyTaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }

    public static class PoseContext
    {
        public static Shim[] Shims { set; get; }
        internal static Dictionary<MethodBase, DynamicMethod> StubCache { private set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            var enumerable = new Shim[]{Shim.Replace(() => System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create())
                .With(() => System.Runtime.CompilerServices1.AsyncTaskMethodBuilder.Create())};
            Shims = shims.Concat(enumerable).ToArray();
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            var delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
            methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
        
        public static void IsolateAsync(Func<Task> entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                AsyncHelper.RunASync(entryPoint.Invoke);
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
            AsyncHelper.RunASync(() => @delegate.DynamicInvoke() as Task);
        }
    }
}