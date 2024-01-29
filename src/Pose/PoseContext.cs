using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Pose.IL;

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
#if TRACE
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
#endif
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

#if TRACE
            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
#endif
            methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
        
        public static async Task Isolate(Func<Task> entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                await entryPoint.Invoke();
                return;
            }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            var delegateType = typeof(Func<Task>);
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
#if TRACE
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
#endif
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

#if TRACE
            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
#endif
            
            // ReSharper disable once PossibleNullReferenceException
            await (methodInfo.CreateDelegate(delegateType).DynamicInvoke() as Task);
        }
    }
}