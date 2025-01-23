using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Pose.IL;

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;

            var delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false, null);
            
#if TRACE
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
#endif
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

#if TRACE
            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
#endif
            
            methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}