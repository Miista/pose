using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#else
// Before .NET Standard 2.1 and/or .NET Core 3.0 the method GetUninitializedObject existed on FormatterServices
using RuntimeHelpers = System.Runtime.Serialization.FormatterServices;
#endif
using Pose.Extensions;
using Pose.Helpers;

// ReSharper disable RedundantExplicitArrayCreation

namespace Pose.IL
{
    internal static class Stubs
    {
        private static readonly MethodInfo GetTypeFromHandle =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))
            ?? throw new Exception($"Cannot get method {nameof(Type.GetTypeFromHandle)} from type {nameof(Type)}");
        
        private static readonly MethodInfo GetUninitializedObject =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))
            ?? throw new Exception($"Cannot get method {nameof(RuntimeHelpers.GetUninitializedObject)} from type {nameof(RuntimeHelpers)}");
        
        private static readonly MethodInfo CreateRewriter =
            typeof(MethodRewriter).GetMethod(nameof(MethodRewriter.CreateRewriter), new Type[] { typeof(MethodBase), typeof(bool) })
            ?? throw new Exception($"Cannot get method {nameof(MethodRewriter.CreateRewriter)} from type {nameof(MethodRewriter)}");

        private static readonly MethodInfo GetMethodFromHandle =
            typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) })
            ?? throw new Exception($"Cannot get method {nameof(MethodBase.GetMethodFromHandle)} from type {nameof(MethodBase)}");
        
        private static readonly MethodInfo GetIndexOfMatchingShim =
            typeof(StubHelper).GetMethod(nameof(StubHelper.GetIndexOfMatchingShim), new []{typeof(MethodBase), typeof(object)})
            ?? throw new Exception($"Cannot get method {nameof(StubHelper.GetIndexOfMatchingShim)} from type {nameof(StubHelper)}");
        
        private static readonly MethodInfo GetShimReplacementMethod =
            typeof(StubHelper).GetMethod(nameof(StubHelper.GetShimReplacementMethod))
            ?? throw new Exception($"Cannot get method {nameof(StubHelper.GetShimReplacementMethod)} from type {nameof(StubHelper)}");
        
        private static readonly MethodInfo GetMethodPointer =
            typeof(StubHelper).GetMethod(nameof(StubHelper.GetMethodPointer))
            ?? throw new Exception($"Cannot get method {nameof(StubHelper.GetMethodPointer)} from type {nameof(StubHelper)}");
        
        private static readonly MethodInfo GetShimDelegateTarget =
            typeof(StubHelper).GetMethod(nameof(StubHelper.GetShimDelegateTarget))
            ?? throw new Exception($"Cannot get method {nameof(StubHelper.GetShimDelegateTarget)} from type {nameof(StubHelper)}");
        
        private static readonly MethodInfo Rewrite =
            typeof(MethodRewriter).GetMethod(nameof(MethodRewriter.Rewrite))
            ?? throw new Exception($"Cannot get method {nameof(MethodRewriter.Rewrite)} from type {nameof(MethodRewriter)}");
        
        private static readonly MethodInfo DeVirtualizeMethod =
            typeof(StubHelper).GetMethod(nameof(StubHelper.DeVirtualizeMethod), new Type[] { typeof(object), typeof(MethodInfo) })
            ?? throw new Exception($"Cannot get method {nameof(StubHelper.DeVirtualizeMethod)} from type {nameof(StubHelper)}");

        public static DynamicMethod GenerateStubForDirectCall(MethodBase method)
        {
            var returnType = method.IsConstructor ? typeof(void) : (method as MethodInfo).ReturnType;
            var signatureParamTypes = new List<Type>();
            
            if (!method.IsStatic)
            {
                var thisType = method.DeclaringType ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
                if (thisType.IsValueType)
                {
                    thisType = thisType.MakeByRefType();
                }

                signatureParamTypes.Add(thisType);
            }

            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_call", method),
                returnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);
        
#if TRACE
            Console.WriteLine("\n" + method);
#endif
            
            var ilGenerator = stub.GetILGenerator();

            if (method.GetMethodBody() == null || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (var i = 0; i < signatureParamTypes.Count; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }

                if (method.IsConstructor)
                    ilGenerator.Emit(OpCodes.Call, (ConstructorInfo)method);
                else
                    ilGenerator.Emit(OpCodes.Call, (MethodInfo)method);

                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            if (method.IsConstructor)
                ilGenerator.Emit(OpCodes.Ldtoken, (ConstructorInfo)method);
            else
                ilGenerator.Emit(OpCodes.Ldtoken, (MethodInfo)method);

            ilGenerator.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);

            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(method.IsStatic || method.IsForValueType() ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, GetIndexOfMatchingShim);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimReplacementMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimDelegateTarget);
            for (var i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, method.IsConstructor ? typeof(void) : (method as MethodInfo).ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);

            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);
            // ilGenerator.Emit(OpCodes.Call, s_createRewriterMethod);
            // ilGenerator.Emit(OpCodes.Call, s_rewriteMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Setup stack and make indirect call
            for (var i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            
            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            
            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualCall(MethodInfo method, TypeInfo constrainedType)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (constrainedType == null) throw new ArgumentNullException(nameof(constrainedType));
            
            var thisType = constrainedType.MakeByRefType();
            var methodDeclaringType = method.DeclaringType ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            var actualMethod = StubHelper.DeVirtualizeMethod(constrainedType, method);
            var actualMethodDeclaringType = actualMethod.DeclaringType ?? throw new Exception($"Method {actualMethod.Name} does not have a {nameof(MethodBase.DeclaringType)}");

            var signatureParamTypes = new List<Type>();
            signatureParamTypes.Add(thisType);
            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_callvirt", method),
                method.ReturnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);
            
#if TRACE
            Console.WriteLine("\n" + method);
#endif
            
            var ilGenerator = stub.GetILGenerator();

            if ((actualMethod.GetMethodBody() == null && !actualMethod.IsAbstract) || StubHelper.IsIntrinsic(actualMethod))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (var i = 0; i < signatureParamTypes.Count; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }

                ilGenerator.Emit(OpCodes.Call, actualMethod);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, actualMethod);
            ilGenerator.Emit(OpCodes.Ldtoken, actualMethodDeclaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(method.IsForValueType() ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, GetIndexOfMatchingShim);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimReplacementMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimDelegateTarget);
            for (var i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, method.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);
            
            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(methodDeclaringType.IsInterface ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Setup stack and make indirect call
            for (var i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
                if (i == 0)
                {
                    if (!constrainedType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Ldind_Ref);
                        signatureParamTypes[i] = constrainedType;
                    }
                    else
                    {
                        if (actualMethodDeclaringType != constrainedType)
                        {
                            ilGenerator.Emit(OpCodes.Ldobj, constrainedType);
                            ilGenerator.Emit(OpCodes.Box, constrainedType);
                            signatureParamTypes[i] = actualMethodDeclaringType;
                        }
                    }
                }
            }
            
            ilGenerator.Emit(OpCodes.Ldloc_0);

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_0);
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualCall(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            
            var declaringType = method.DeclaringType ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            var thisType = declaringType.IsInterface ? typeof(object) : declaringType;

            var signatureParamTypes = new List<Type>();
            signatureParamTypes.Add(thisType);
            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_callvirt", method),
                method.ReturnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

#if TRACE
            Console.WriteLine("\n" + method);
#endif
            
            var ilGenerator = stub.GetILGenerator();

            if ((method.GetMethodBody() == null && !method.IsAbstract) || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (var i = 0; i < signatureParamTypes.Count; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }

                ilGenerator.Emit(OpCodes.Callvirt, method);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, method);
            ilGenerator.Emit(OpCodes.Ldtoken, declaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Resolve virtual method to object type
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, DeVirtualizeMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(method.IsForValueType() ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, GetIndexOfMatchingShim);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimReplacementMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, GetShimDelegateTarget);
            for (var i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, method.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);
            
            // Rewrite resolved method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(declaringType.IsInterface ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Setup stack and make indirect call
            for (var i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            
            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
        }

        public static DynamicMethod GenerateStubForObjectInitialization(ConstructorInfo constructor)
        {
            var thisType = constructor.DeclaringType ?? throw new Exception($"Method {constructor.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            
            if (thisType.IsValueType)
            {
                thisType = thisType.MakeByRefType();
            }

            var signatureParamTypes = new List<Type>();
            signatureParamTypes.Add(thisType);
            signatureParamTypes.AddRange(constructor.GetParameters().Select(p => p.ParameterType));

            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_newobj", constructor),
                constructor.DeclaringType,
                signatureParamTypes.Skip(1).ToArray(),
                StubHelper.GetOwningModule(),
                true);
            
            var ilGenerator = stub.GetILGenerator();

            if (constructor.GetMethodBody() == null || StubHelper.IsIntrinsic(constructor))
            {
                // Constructor has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (var i = 0; i < signatureParamTypes.Count - 1; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }
            
                ilGenerator.Emit(OpCodes.Newobj, constructor);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(IntPtr));
            ilGenerator.DeclareLocal(constructor.DeclaringType);
            ilGenerator.DeclareLocal(typeof(ConstructorInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, constructor);
            ilGenerator.Emit(OpCodes.Ldtoken, constructor.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);

            // NEW
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Call, GetIndexOfMatchingShim);
            ilGenerator.Emit(OpCodes.Stloc_3);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            
            ilGenerator.Emit(OpCodes.Call, GetShimReplacementMethod);
            ilGenerator.Emit(OpCodes.Stloc_S, 4);
            ilGenerator.Emit(OpCodes.Ldloc_S, 4);
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Call, GetShimDelegateTarget);
            for (var i = 0; i < signatureParamTypes.Count - 1; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, constructor.DeclaringType, signatureParamTypes.Skip(1).ToArray(), null);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);
            // END NEW
            
            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            
            // ++
            if (constructor.DeclaringType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, (byte)1);
                // ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Initobj, constructor.DeclaringType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldtoken, constructor.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, GetTypeFromHandle);
                
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                ilGenerator.Emit(OpCodes.Call, GetUninitializedObject);
#else
                ilGenerator.Emit(OpCodes.Call, GetUninitializedObject);
#endif
                // ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Stloc_1);
            }
            // ++
            
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_S, 4);

            if (constructor.DeclaringType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldloca, 0);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldloc_1);
            }
            
            // Setup stack and make indirect call
            for (var i = 0; i < signatureParamTypes.Count - 1; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }
            
            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Ldloc, 4);
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
            
            /*
            ParameterInfo[] parameters = constructor.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();

            var forValueType = constructor.DeclaringType.IsValueType;
            
            if (forValueType)
                signatureParamTypes.Add(constructor.DeclaringType.MakeByRefType());
            else
                signatureParamTypes.Add(constructor.DeclaringType);

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));

            parameterTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_ctor_{0}_{1}", constructor.DeclaringType, constructor.Name),
                constructor.DeclaringType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            ilGenerator.DeclareLocal(constructor.DeclaringType);
            ilGenerator.DeclareLocal(typeof(ConstructorInfo));
            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
            ilGenerator.Emit(OpCodes.Stloc_1);

            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod(nameof(StubHelper.GetIndexOfMatchingShim), new []{typeof(MethodBase), typeof(object)}));
            ilGenerator.Emit(OpCodes.Stloc_3);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod(nameof(StubHelper.GetShimReplacementMethod)));
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod(nameof(StubHelper.GetMethodPointer)));
            ilGenerator.Emit(OpCodes.Stloc, 4);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod(nameof(StubHelper.GetShimDelegateTarget)));
            for (int i = 0; i < signatureParamTypes.Count - 1; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc, 4);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, constructor.DeclaringType, signatureParamTypes.Skip(1).ToArray(), null);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);

            ilGenerator.MarkLabel(rewriteLabel);
            if (forValueType)
            {
                ilGenerator.Emit(OpCodes.Ldloca, 0);
                ilGenerator.Emit(OpCodes.Initobj, constructor.DeclaringType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                ilGenerator.Emit(OpCodes.Call, typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject)));
                ilGenerator.Emit(OpCodes.Stloc_0);
            }
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
            ilGenerator.Emit(OpCodes.Stloc_1);

            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod(nameof(MethodRewriter.CreateRewriter), new Type[] { typeof(MethodBase), typeof(bool) }));
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod(nameof(MethodRewriter.Rewrite)));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_2);
            int count = signatureParamTypes.Count;
            if (forValueType)
                ilGenerator.Emit(OpCodes.Ldloca, 0);
            else
                ilGenerator.Emit(OpCodes.Ldloc_0);
            count = count - 1;
            for (int i = 0; i < count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), signatureParamTypes.ToArray(), null);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
            */
        }

        public static DynamicMethod GenerateStubForDirectLoad(MethodBase method)
        {
            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_ldftn", method),
                typeof(IntPtr),
                Array.Empty<Type>(),
                StubHelper.GetOwningModule(),
                true);
            
            var ilGenerator = stub.GetILGenerator();

            if (method.GetMethodBody() == null || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                if (method.IsConstructor)
                    ilGenerator.Emit(OpCodes.Ldftn, (ConstructorInfo)method);
                else
                    ilGenerator.Emit(OpCodes.Ldftn, (MethodInfo)method);

                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            if (method.IsConstructor)
                ilGenerator.Emit(OpCodes.Ldtoken, (ConstructorInfo)method);
            else
                ilGenerator.Emit(OpCodes.Ldtoken, (MethodInfo)method);

            ilGenerator.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);

            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualLoad(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var declaringType = method.DeclaringType ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            
            var stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_ldvirtftn", method),
                typeof(IntPtr),
                new Type[] { declaringType.IsInterface ? typeof(object) : declaringType },
                StubHelper.GetOwningModule(),
                true);
            
            var ilGenerator = stub.GetILGenerator();

            if ((method.GetMethodBody() == null && !method.IsAbstract) || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                ilGenerator.Emit(OpCodes.Ldarg, 0);
                ilGenerator.Emit(OpCodes.Ldvirtftn, method);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(MethodInfo));

            var rewriteLabel = ilGenerator.DefineLabel();
            var returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, method);
            ilGenerator.Emit(OpCodes.Ldtoken, declaringType);
            ilGenerator.Emit(OpCodes.Call, GetMethodFromHandle);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Resolve virtual method to object type
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, DeVirtualizeMethod);

            // Rewrite resolved method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(declaringType.IsInterface ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, CreateRewriter);
            ilGenerator.Emit(OpCodes.Call, Rewrite);

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, GetMethodPointer);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            
            return stub;
        }
    }
}