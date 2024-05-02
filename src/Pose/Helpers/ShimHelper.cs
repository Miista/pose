using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Exceptions;
using Pose.Extensions;

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
                            throw new UnsupportedExpressionException($"Expression (of type {expression.GetType()}) with NodeType '{expression.NodeType}' is not supported");
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
                case ExpressionType.Convert:
                case ExpressionType.Not:
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                    var unaryExpression = expression as UnaryExpression ?? throw new Exception($"Cannot cast expression to {nameof(UnaryExpression)}");
                    instanceOrType = null;
                    return unaryExpression.Method ?? throw new Exception(GetExceptionMessage(expression));
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                case ExpressionType.Modulo:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.And:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Or:
                    var binaryExpression = expression as BinaryExpression ?? throw new Exception($"Cannot cast expression to {nameof(BinaryExpression)}");
                    instanceOrType = null;
                    return binaryExpression.Method ?? throw new Exception(GetExceptionMessage(expression));
                default:
                    throw new UnsupportedExpressionException($"Expression (of type {expression.GetType()}) with NodeType '{expression.NodeType}' is not supported");
            }
        }

        private static string GetExceptionMessage(Expression expression)
        {
            if (expression.NodeType.IsOverloadableOperator())
            {
                return
                    $"Cannot shim the {expression.NodeType} operator on {expression.Type} because the type itself does not overload this operator.";
            }

            return $"The expression for node type {expression.NodeType} could not be mapped to a method";
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
                throw new InvalidShimSignatureException($"Mismatched return types. Expected {validReturnType}. Got {shimReturnType}");

            if (!isStaticOrConstructor)
            {
                if (isValueType && !shimOwningType.IsByRef)
                    throw new InvalidShimSignatureException("ValueType instances must be passed by ref");
            }

            var expectedOwningType = (isValueType && !isStaticOrConstructor ? validOwningType.MakeByRefType() : validOwningType);
            if (expectedOwningType != shimOwningType)
                throw new InvalidShimSignatureException($"Mismatched instance types. Expected {expectedOwningType.FullName}. Got {shimOwningType.FullName}");

            if (validParameterTypes.Length != shimParameterTypes.Length)
                throw new InvalidShimSignatureException($"Parameters count do not match. Expected {validParameterTypes.Length}. Got {shimParameterTypes.Length}");

            for (var i = 0; i < validParameterTypes.Length; i++)
            {
                var expectedType = validParameterTypes.ElementAt(i);
                var actualType = shimParameterTypes.ElementAt(i);
                
                if (expectedType != actualType)
                    throw new InvalidShimSignatureException($"Parameter types at {i} do not match. Expected '{expectedType}' but found {actualType}'");
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