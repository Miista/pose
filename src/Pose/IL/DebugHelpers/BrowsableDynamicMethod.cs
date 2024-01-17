using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.IL.DebugHelpers
{
    internal class BrowsableDynamicMethod : MethodInfo
    {
        private readonly DynamicMethod _method;
        private readonly MethodBody _methodBody;

        public BrowsableDynamicMethod(DynamicMethod method, MethodBody methodBody)
        {
            _method = method;
            _methodBody = methodBody;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        public override MethodAttributes Attributes => MethodAttributes.Static;

        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        /// <summary>
        /// This method is called by Mono.Reflection.Disaasembler.GetInstructions
        /// </summary>
        public override Module Module => new DynamicModule(_method.GetILGenerator());

        public override Type DeclaringType => _method.DeclaringType;

        public override string Name => _method.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override Type[] GetGenericArguments() => Array.Empty<Type>();

        public override MethodBody GetMethodBody() => _methodBody;

        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

        public override ParameterInfo[] GetParameters() => _method.GetParameters();

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
            => throw new NotImplementedException();

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    }
}
