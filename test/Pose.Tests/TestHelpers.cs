using System;
using System.Linq.Expressions;

namespace Pose.Tests
{
    internal static class TestHelpers
    {
        public static BinaryExpression LocalField<TType>(Expression<Func<TType>> expression, TType assignValue)
        {
            return Expression.Assign(
                expression.Body,
                Expression.Constant(assignValue)
            );
        }

        public static TReturn SetAndReturn<TReturn>(Expression expression, TReturn returnValue)
        {
            var lambda = Expression.Lambda<Func<TReturn>>(
                Expression.Block(
                    expression,
                    Expression.Constant(returnValue)
                )
            );

            return lambda.Compile().Invoke();
        }
    }
}