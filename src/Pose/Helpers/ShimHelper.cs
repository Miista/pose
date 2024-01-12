using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Exceptions;

namespace Pose.Helpers
{
    internal static class ShimHelper
    {
        public static MethodBase GetMethodFromExpression(Expression expression, bool setter, out object instanceOrType)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExpression = expression as MemberExpression ?? throw new Exception($"Cannot cast expression to {nameof(MemberExpression)}");
                        var memberInfo = memberExpression.Member;
                        if (memberInfo.MemberType == MemberTypes.Property)
                        {
                            var propertyInfo = memberInfo as PropertyInfo ?? throw new Exception($"Cannot cast {nameof(memberInfo)} to {nameof(PropertyInfo)}");
                            instanceOrType = GetObjectInstanceOrType(memberExpression.Expression);
                            return setter ? propertyInfo.GetSetMethod() : propertyInfo.GetGetMethod();
                        }
                        else
                        {
                            throw new NotImplementedException("Unsupported expression");
                        }
                    }
                case ExpressionType.Call:
                    var methodCallExpression = expression as MethodCallExpression ?? throw new Exception($"Cannot cast expression to {nameof(MethodCallExpression)}");
                    instanceOrType = GetObjectInstanceOrType(methodCallExpression.Object);
                    return methodCallExpression.Method;
                case ExpressionType.New:
                    var newExpression = expression as NewExpression ?? throw new Exception($"Cannot cast expression to {nameof(NewExpression)}");
                    instanceOrType = null;
                    return newExpression.Constructor;
                default:
                    throw new NotImplementedException("Unsupported expression");
            }
        }

        public static void ValidateReplacementMethodSignature(MethodBase original, MethodInfo replacement, Type type, bool setter)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            var isValueType = original.DeclaringType?.IsValueType ?? throw new Exception($"Method {original.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            var isStatic = original.IsStatic;
            var isConstructor = original.IsConstructor;
            var isStaticOrConstructor = isStatic || isConstructor;

            var validReturnType = isConstructor ? original.DeclaringType : (original as MethodInfo).ReturnType;
            validReturnType = setter ? typeof(void) : validReturnType;
            var shimReturnType = replacement.ReturnType;

            var validOwningType = type;
            var shimOwningType = isStaticOrConstructor
                ? validOwningType : replacement.GetParameters().Select(p => p.ParameterType).FirstOrDefault();

            var validParameterTypes = original.GetParameters().Select(p => p.ParameterType).ToArray();
            var shimParameterTypes = replacement.GetParameters()
                                        .Select(p => p.ParameterType)
                                        .Skip(isStaticOrConstructor ? 0 : 1)
                                        .ToArray();

            if (validReturnType != shimReturnType)
                throw new InvalidShimSignatureException("Mismatched return types");

            if (!isStaticOrConstructor)
            {
                if (isValueType && !shimOwningType.IsByRef)
                    throw new InvalidShimSignatureException("ValueType instances must be passed by ref");
            }

            if ((isValueType && !isStaticOrConstructor ? validOwningType.MakeByRefType() : validOwningType) != shimOwningType)
                throw new InvalidShimSignatureException("Mismatched instance types");

            if (validParameterTypes.Count() != shimParameterTypes.Count())
                throw new InvalidShimSignatureException("Parameters count do not match");

            for (var i = 0; i < validParameterTypes.Count(); i++)
            {
                if (validParameterTypes.ElementAt(i) != shimParameterTypes.ElementAt(i))
                    throw new InvalidShimSignatureException($"Parameter types at {i} do not match");
            }
        }

        public static object GetObjectInstanceOrType(Expression expression)
        {
            object instanceOrType = null;
            switch (expression?.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExpression = expression as MemberExpression ?? throw new Exception($"Cannot cast expression to {nameof(MemberExpression)}");
                        var constantExpression = memberExpression.Expression as ConstantExpression;
                        
                        if (memberExpression.Member.MemberType == MemberTypes.Field)
                        {
                            var fieldInfo = memberExpression.Member as FieldInfo ?? throw new Exception($"Cannot cast {nameof(MemberExpression.Member)} to {nameof(FieldInfo)}");
                            var obj = fieldInfo.IsStatic ? null : constantExpression?.Value;
                            instanceOrType = fieldInfo.GetValue(obj);
                        }
                        else if (memberExpression.Member.MemberType == MemberTypes.Property)
                        {
                            var propertyInfo = memberExpression.Member as PropertyInfo ?? throw new Exception($"Cannot cast {nameof(MemberExpression.Member)} to {nameof(PropertyInfo)}");
                            var obj = propertyInfo.GetMethod.IsStatic ? null : constantExpression?.Value;
                            instanceOrType = propertyInfo.GetValue(obj);
                        }
                        EnsureInstanceNotValueType(instanceOrType);
                        break;
                    }
                case ExpressionType.Call:
                    {
                        var methodCallExpression = expression as MethodCallExpression ?? throw new Exception($"Cannot cast expression to {nameof(MethodCallExpression)}");
                        var methodInfo = methodCallExpression.Method;
                        instanceOrType = methodInfo.GetGenericArguments().FirstOrDefault();
                        break;
                    }
                default:
                    return null;
            }

            return instanceOrType;
        }

        private static void EnsureInstanceNotValueType(object instance)
        {
            if (instance.GetType().IsSubclassOf(typeof(ValueType)))
                throw new NotSupportedException("You cannot replace methods on specific value type instances");
        }
    }
}