using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.CompilerServices.System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pose;
using Pose.IL;

namespace System.Runtime.CompilerServices
{
    /// <summary>Shared helpers for manipulating state related to async state machines.</summary>
    // internal static class AsyncMethodBuilderCore // debugger depends on this exact name
    // {
    //     public static void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    //     {
    //         if (stateMachine == null) // TStateMachines are generally non-nullable value types, so this check will be elided
    //         {
    //             //ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
    //         }
    //
    //         Thread currentThread = Thread.CurrentThread;
    //
    //         // Store current ExecutionContext and SynchronizationContext as "previousXxx".
    //         // This allows us to restore them and undo any Context changes made in stateMachine.MoveNext
    //         // so that they won't "leak" out of the first await.
    //         ExecutionContext previousExecutionCtx = currentThread.ExecutionContext;
    //         SynchronizationContext previousSyncCtx = null;
    //
    //         try
    //         {
    //             Console.WriteLine(stateMachine.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)[0]);
    //             stateMachine.MoveNext();
    //         }
    //         finally
    //         {
    //         }
    //     }
    // }
    
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    internal readonly struct VoidTaskResult
    {
    }
    
    // AsyncVoidMethodBuilder.cs in your project
    public class AsyncTaskMethodBuilder
    {
        private Task<VoidTaskResult> m_task; // Debugger depends on the exact name of this field.
        
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine
        )
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var forwardingMethod = typeof(AsyncTaskMethodBuilder<VoidTaskResult>).GetMethod("AwaitOnCompleted",new []{typeof(TAwaiter), typeof(TStateMachine), typeof(Task<VoidTaskResult>)});
            forwardingMethod.Invoke(null, new object[] { awaiter, stateMachine, m_task });
            //AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
        }
        
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            
        }
    
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            Console.WriteLine("SetStateMachine");
        }
    
        public void SetException(Exception exception)
        {
            Console.WriteLine("SetException");
        }
        
        public Task Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_task ?? InitializeTaskAsPromise();
        }
    
        /// <summary>
        /// Initializes the task, which must not yet be initialized.  Used only when the Task is being forced into
        /// existence when no state machine is needed, e.g. when the builder is being synchronously completed with
        /// an exception, when the builder is being used out of the context of an async method, etc.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task<VoidTaskResult> InitializeTaskAsPromise()
        {
            var task = typeof(Task<VoidTaskResult>)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault()
                .Invoke(Type.EmptyTypes) as Task<VoidTaskResult>;
            return m_task = task;
        }
    
        public AsyncTaskMethodBuilder()
            => Console.WriteLine(".ctor");
    
        public static AsyncTaskMethodBuilder Create()
            => new AsyncTaskMethodBuilder();
    
        public void SetResult()
        {
            // Get the currently stored task, which will be non-null if get_Task has already been accessed.
            // If there isn't one, store the supplied completed task.
            if (m_task is null)
            {
                var methodInfos = typeof(Task).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
                Console.WriteLine(methodInfos.FirstOrDefault().Name);
    
                //m_task = Task.s_cachedCompleted;
            }
            else
            {
                // Otherwise, complete the task that's there.
                var methodInfos = typeof(AsyncTaskMethodBuilder<VoidTaskResult>).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                Console.WriteLine(methodInfos.FirstOrDefault().Name);
                //AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(m_task, );
            }
    
        }
    
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            Console.WriteLine("Start");
            var shims = PoseContext.Shims;
            var methodInfos = stateMachine.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            var methodInfo = methodInfos[0];
            Console.WriteLine(methodInfo.Name);
            var methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            var methodBase = (MethodInfo) methodRewriter.Rewrite();
            var makeGenericType = typeof(Action<>).MakeGenericType(stateMachine.GetType());
            methodBase.CreateDelegate(makeGenericType).DynamicInvoke(stateMachine);
            //methodBase.Invoke(stateMachine, new object[] { stateMachine });
    
            // methodBase.Invoke(this, new object[] { stateMachine });
            //stateMachine.MoveNext();
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

            //var enumerable = new Shim[]{Shim.Replace(() => System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create())
            //    .With(() => System.Runtime.CompilerServices1.AsyncTaskMethodBuilder.Create())};
            Shims = shims; //.Concat(enumerable).ToArray();
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
            // if (shims == null || shims.Length == 0)
            // {
            //     AsyncHelper.RunASync(entryPoint.Invoke);
            //     return;
            // }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            var delegateType = typeof(Func<Task>); //.MakeGenericType(entryPoint.Target.GetType());
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
            var @delegate = methodInfo.CreateDelegate(delegateType);
            var x = @delegate.DynamicInvoke() as Task;

            await x;
        }
    }
}

namespace System.Runtime.CompilerServices.System.Runtime.CompilerServices
{
    internal interface IAsyncStateMachineBox { }
}