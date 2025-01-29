using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Pose.IL;

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { private set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;

            var delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            var rewriter = ExpressionTreeMethodRewriter.CreateRewriter(entryPoint.Method, false, entryPoint.Target);
            
#if TRACE
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
#endif
            if (entryPoint.Target == null)
            {
                rewriter.Rewrite().DynamicInvoke();
            }
            else
            {
                rewriter.Rewrite().DynamicInvoke(entryPoint.Target);
            }

#if TRACE
            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
#endif
            
            // methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}