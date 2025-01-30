using System;
using System.Linq.Expressions;

namespace Pose.Extensions
{
    internal static class ExpressionExtensions
    {
        public static Expression EnsureExpressionType(this Expression value, Type type)
        {
            if (value.Type == type) return value;

            if (type == typeof(bool))
            {
                return value switch
                {
                    // bools are represented using ints by the CLR
                    ConstantExpression ce => ce.Value switch
                    {
                        0 => Expression.Constant(false),
                        1 => Expression.Constant(true),
                        _ => throw new Exception("Invalid value for boolean")
                    }
                };
            }

            return value;
        }

    }
}