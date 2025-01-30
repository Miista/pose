using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Helpers;

namespace Pose
{
    public partial class Shim
    {
        private readonly Expression _originalExpression;

        private bool _setter;

        internal bool IsInstanceSpecific => Instance != null;

        internal MethodBase Original { get; }
        internal Delegate Replacement { get; private set; }
        internal object Instance { get; }
        internal Type Type { get; }
        internal Expression ReplacementExpression { get; private set; }
        
        private Shim(Expression originalExpression, MethodBase original, object instanceOrType)
        {
            _originalExpression = originalExpression ?? throw new ArgumentNullException(nameof(originalExpression));
            Original = original ?? throw new ArgumentNullException(nameof(original));
            
            if (instanceOrType is Type type)
            {
                Type = type;
            }
            else
            {
                Instance = instanceOrType;
            }
        }

        [ExcludeFromCodeCoverage(Justification = "Forwards to ReplaceImpl")]
        public static Shim Replace(Expression<Action> expression, bool setter = false)
            => ReplaceImpl(expression, setter);

        [ExcludeFromCodeCoverage(Justification = "Forwards to ReplaceImpl")]
        public static Shim Replace<T>(Expression<Func<T>> expression, bool setter = false)
            => ReplaceImpl(expression, setter);

        private static Shim ReplaceImpl<T>(Expression<T> expression, bool setter)
        {
            var methodBase = ShimHelper.GetMethodFromExpression(expression.Body, setter, out var instance);
            return new Shim(expression, methodBase, instance) { _setter = setter };
        }

        private Shim WithImpl(Delegate replacement)
        {
            Replacement = replacement;
            ShimHelper.ValidateReplacementMethodSignature(
                original: Original,
                replacement: Replacement.Method,
                type: Instance?.GetType() ?? Type,
                setter: _setter
            );
            
            return this;
        }
        
        private Shim WithImpl(Expression replacement)
        {
            ReplacementExpression = replacement;
            ShimHelper.ValidateReplacementExpressionSignature(
                originalMethod: Original,
                originalExpression: _originalExpression,
                replacementExpression: ReplacementExpression,
                type: Instance?.GetType() ?? Type,
                setter: _setter
            );
            
            return this;
        }
    }
}