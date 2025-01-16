using System.Threading.Tasks;

// namespace System.Runtime.CompilerServices
// {
//     // AsyncVoidMethodBuilder.cs in your project
//     public struct AsyncVoidMethodBuilder
//     {
//         public static AsyncVoidMethodBuilder Create()
//             => new AsyncVoidMethodBuilder();
//  
//         public void SetResult() => Console.WriteLine("SetResult");
//  
//         public void Start<TStateMachine>(ref TStateMachine stateMachine)
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("Start");
//             stateMachine.MoveNext();
//         }
//  
//         // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
//         // and SetStateMachine are empty
//     }
//     
//     public class AsyncTaskMethodBuilder
//     {
//         public static AsyncTaskMethodBuilder Create()
//             => new AsyncTaskMethodBuilder();
//  
//         public void SetResult() => Console.WriteLine("SetResult");
//  
//         public void Start<TStateMachine>(ref TStateMachine stateMachine)
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("Start");
//             stateMachine.MoveNext();
//         }
//  
//         private Task m_task; // lazily-initialized: must not be readonly
//         
//         public Task Task
//         {
//             get
//             {
//                 // Get and return the task. If there isn't one, first create one and store it.
//                 var task = m_task;
//                 if (task == null)
//                 {
//                     m_task = task = new Task(() => {});
//                     
//                 }
//                 return task;
//             }
//         }
//
//         public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
//             ref TAwaiter awaiter,
//             ref TStateMachine stateMachine
//         )
//             where TAwaiter : ICriticalNotifyCompletion
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("AwaitUnsafeOnCompleted");
//         }
//
//         public void AwaitOnCompleted<TAwaiter, TStateMachine>(
//             ref TAwaiter awaiter,
//             ref TStateMachine stateMachine
//         )
//             where TAwaiter : INotifyCompletion
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("AwaitOnCompleted");
//         }
//         
//         public void SetStateMachine(IAsyncStateMachine stateMachine)
//         {
//             Console.WriteLine("SetStateMachine");
//         }
//
//         internal void SetResult(Task completedTask)
//         {
//             
//         }
//         
//         public void SetException(Exception exception)
//         {
//         }
//
//         // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
//         // and SetStateMachine are empty
//     }
//     
//     public class AsyncTaskMethodBuilder<TResult>
//     {
//         public static AsyncTaskMethodBuilder<TResult> Create() => new AsyncTaskMethodBuilder<TResult>();
//
//         public void SetResult(TResult result) => Console.WriteLine("SetResult");
//  
//         public void Start<TStateMachine>(ref TStateMachine stateMachine)
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("Start");
//             stateMachine.MoveNext();
//         }
//
//         public void SetStateMachine(IAsyncStateMachine stateMachine)
//         {
//             Console.WriteLine("SetStateMachine");
//         }
//         
//         public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
//             ref TAwaiter awaiter,
//             ref TStateMachine stateMachine
//         )
//             where TAwaiter : ICriticalNotifyCompletion
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("AwaitUnsafeOnCompleted");
//         }
//
//         private Task<TResult> m_task; // lazily-initialized: must not be readonly
//         
//         public Task<TResult> Task
//         {
//             get
//             {
//                 // Get and return the task. If there isn't one, first create one and store it.
//                 var task = m_task;
//                 if (task == null)
//                 {
//                     m_task = task = new Task<TResult>(() => default(TResult));
//                     
//                 }
//                 return task;
//             }
//         }
//
//         public void AwaitOnCompleted<TAwaiter, TStateMachine>(
//             ref TAwaiter awaiter,
//             ref TStateMachine stateMachine
//         )
//             where TAwaiter : INotifyCompletion
//             where TStateMachine : IAsyncStateMachine
//         {
//             Console.WriteLine("AwaitOnCompleted");
//         }
//
//         public void SetResult(Task<TResult> completedTask)
//         {
//             
//         }
//         
//         public void SetException(Exception exception)
//         {
//         }
//  
//         // AwaitOnCompleted, AwaitUnsafeOnCompleted, SetException 
//         // and SetStateMachine are empty
//     }
// }