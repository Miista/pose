using System.Linq.Expressions;

namespace Pose.Extensions
{
    internal static class ExpressionTypeExtensions
    {
        public static bool IsOverloadableOperator(this ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.Not:
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
                    return true;
                default:
                    return false;
            }
        }
    }
}