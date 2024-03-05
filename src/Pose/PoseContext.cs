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
            var rewriter = MethodRewriter.CreateRewriter(entryPoint.Method, false);
            Console.WriteLine("----------------------------- Rewriting ----------------------------- ");
            var methodInfo = (MethodInfo)(rewriter.Rewrite());

            Console.WriteLine("----------------------------- Invoking ----------------------------- ");
            methodInfo.CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}