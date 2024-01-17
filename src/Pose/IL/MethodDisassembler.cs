using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Mono.Reflection;

namespace Pose.IL
{
    internal class MethodDisassembler
    {
        private readonly MethodBase _method;

        public MethodDisassembler(MethodBase method)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public IEnumerable<Instruction> GetILInstructions()
        {
            return _method.GetInstructions().ToList();
        }

        public List<MethodBase> GetMethodDependencies()
        {
            var methodDependencies = GetILInstructions()
                .Where(i => i.Operand as MethodInfo != null || i.Operand as ConstructorInfo != null)
                .Select(i => i.Operand as MethodBase);

            return methodDependencies.ToList();
        }
    }
}