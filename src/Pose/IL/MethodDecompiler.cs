using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Pose.IL
{
    internal class MethodDecompiler
    {
        private MethodBase _method;

        private Stack<Expression> _stack;

        private ParameterExpression[] _initLocals;

        private List<ParameterExpression> _locals;

        private ParameterExpression[] _arguments;

        public MethodDecompiler(MethodBase method)
        {
            _method = method;
            _stack = new Stack<Expression>();
            _initLocals = new ParameterExpression[method.GetMethodBody().LocalVariables.Count];
            _locals = new List<ParameterExpression>();

            ParameterInfo[] parameters = method.GetParameters();
            _arguments = new ParameterExpression[method.IsStatic ? parameters.Length : parameters.Length + 1];

            if (!method.IsStatic)
            {
                _arguments[0] = Expression.Parameter(method.DeclaringType);
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                _arguments[method.IsStatic ? i : i + 1] = Expression.Parameter(parameters[i].ParameterType);
            }
        }

        public LambdaExpression Decompile() => default;
    }
}