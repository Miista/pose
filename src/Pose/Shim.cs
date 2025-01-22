using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Helpers;

namespace Pose
{
    public partial class Shim
    {
        private MethodBase _original;
        private Delegate _replacement;
        private object _instance;
        private Type _type;
        private bool _setter;
        private Expression _expression;

        internal MethodBase Original
        {
            get
            {
                return _original;
            }
        }

        internal Delegate Replacement
        {
            get
            {
                return _replacement;
            }
        }

        internal object Instance
        {
            get
            {
                return _instance;
            }
        }

        internal Type Type
        {
            get
            {
                return _type;
            }
        }

        internal Expression Expression
        {
            get { return _expression; }
        }
        
        internal Expression ReplacementExpression { get; private set; }
        
        private Shim(MethodBase original, object instanceOrType, Expression expression)
        {
            _original = original;
            if (instanceOrType is Type type)
                _type = type;
            else
                _instance = instanceOrType;
            _expression = expression;
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
            return new Shim(methodBase, instance, expression) { _setter = setter };
        }

        private Shim WithImpl(Delegate replacement)
        {
            _replacement = replacement;
            ShimHelper.ValidateReplacementMethodSignature(this._original, this._replacement.Method, _instance?.GetType() ?? _type, _setter);
            return this;
        }
        
        private Shim WithImpl(Expression replacement)
        {
            ReplacementExpression = replacement;
            //_replacement = replacement;
            //ShimHelper.ValidateReplacementMethodSignature(this._original, this._replacement.Method, _instance?.GetType() ?? _type, _setter);
            return this;
        }
    }
}