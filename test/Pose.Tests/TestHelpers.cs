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

        public static BinaryExpression LocalField<TType>(Expression<Func<TType>> expression, Func<Expression, Expression> assignValue)
        {
            return Expression.Assign(
                expression.Body,
                assignValue(expression.Body)
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
        
        public static void Set(Expression expression)
        {
            var lambda = Expression.Lambda<Action>(
                Expression.Block(
                    expression
                )
            );

            lambda.Compile().Invoke();
        }
    }
}