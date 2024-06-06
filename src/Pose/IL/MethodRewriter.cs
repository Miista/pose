using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Mono.Reflection;
using Pose.Exceptions;
using Pose.Extensions;
using Pose.Helpers;
using Pose.IL.DebugHelpers;

namespace Pose.IL
{
    internal class MethodRewriter
    {
        private static readonly List<OpCode> IgnoredOpCodes = new List<OpCode> { OpCodes.Endfilter, OpCodes.Endfinally };

        private static readonly MethodInfo Unbox =
            typeof(Unsafe).GetMethod(nameof(Unsafe.Unbox))
            ?? throw new Exception($"Cannot get method {nameof(Unsafe.Unbox)} from type {nameof(Unsafe)}");

        private readonly MethodBase _method;
        private readonly Type _owningType;
        private readonly bool _isInterfaceDispatch;
        
        private int _exceptionBlockLevel;
        private TypeInfo _constrainedType;

        private MethodRewriter(MethodBase method, Type owningType, bool isInterfaceDispatch)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _owningType = owningType ?? throw new ArgumentNullException(nameof(owningType));
            _isInterfaceDispatch = isInterfaceDispatch;
        }

        public static MethodRewriter CreateRewriter(MethodBase method, bool isInterfaceDispatch)
        {
            return new MethodRewriter(
                method: method,
                owningType: method.DeclaringType,
                isInterfaceDispatch: isInterfaceDispatch
            );
        }

        public MethodBase Rewrite()
        {
            var parameterTypes = new List<Type>();
            if (!_method.IsStatic)
            {
                var thisType = _isInterfaceDispatch ? typeof(object) : _owningType;
                if (!_isInterfaceDispatch && _owningType.IsValueType)
                {
                    thisType = thisType.MakeByRefType();
                }

                parameterTypes.Add(thisType);
            }

            parameterTypes.AddRange(_method.GetParameters().Select(p => p.ParameterType));
            var returnType = _method.IsConstructor ? typeof(void) : (_method as MethodInfo).ReturnType;

            var dynamicMethod = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("impl", _method),
                returnType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            var methodBody = _method.GetMethodBody() ?? throw new MethodRewriteException($"Method {_method.Name} does not have a body");
            var locals = methodBody.LocalVariables;
            var targetInstructions = new Dictionary<int, Label>();
            var handlers = new List<ExceptionHandler>();

            var ilGenerator = dynamicMethod.GetILGenerator();
            var instructions = _method.GetInstructions();

            foreach (var clause in methodBody.ExceptionHandlingClauses)
            {
                var handler = new ExceptionHandler
                {
                    Flags = clause.Flags,
                    CatchType = clause.Flags == ExceptionHandlingClauseOptions.Clause ? clause.CatchType : null,
                    TryStart = clause.TryOffset,
                    TryEnd = clause.TryOffset + clause.TryLength,
                    FilterStart = clause.Flags == ExceptionHandlingClauseOptions.Filter ? clause.FilterOffset : -1,
                    HandlerStart = clause.HandlerOffset,
                    HandlerEnd = clause.HandlerOffset + clause.HandlerLength
                };
                handlers.Add(handler);
            }

            foreach (var local in locals)
            {
                ilGenerator.DeclareLocal(local.LocalType, local.IsPinned);
            }

            var ifTargets = instructions
                .Where(i => i.Operand is Instruction)
                .Select(i => i.Operand as Instruction);

            foreach (var ifInstruction in ifTargets)
            {
                if (ifInstruction == null) throw new Exception("The impossible happened");
                
                targetInstructions.TryAdd(ifInstruction.Offset, ilGenerator.DefineLabel());
            }

            var switchTargets = instructions
                .Where(i => i.Operand is Instruction[])
                .Select(i => i.Operand as Instruction[]);
            
            foreach (var switchInstructions in switchTargets)
            {
                if (switchInstructions == null) throw new Exception("The impossible happened");
                
                foreach (var instruction in switchInstructions)
                    targetInstructions.TryAdd(instruction.Offset, ilGenerator.DefineLabel());
            }

#if TRACE
            Console.WriteLine("\n" + _method);
#endif

            foreach (var instruction in instructions)
            {
#if TRACE
                Console.WriteLine(instruction);
#endif

                EmitILForExceptionHandlers(ilGenerator, instruction, handlers);

                if (targetInstructions.TryGetValue(instruction.Offset, out var label))
                    ilGenerator.MarkLabel(label);

                if (IgnoredOpCodes.Contains(instruction.OpCode)) continue;

                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineNone:
                        EmitILForInlineNone(ilGenerator, instruction);
                        break;
                    case OperandType.InlineI:
                        EmitILForInlineI(ilGenerator, instruction);
                        break;
                    case OperandType.InlineI8:
                        EmitILForInlineI8(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineI:
                        EmitILForShortInlineI(ilGenerator, instruction);
                        break;
                    case OperandType.InlineR:
                        EmitILForInlineR(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineR:
                        EmitILForShortInlineR(ilGenerator, instruction);
                        break;
                    case OperandType.InlineString:
                        EmitILForInlineString(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        EmitILForInlineBrTarget(ilGenerator, instruction, targetInstructions);
                        break;
                    case OperandType.InlineSwitch:
                        EmitILForInlineSwitch(ilGenerator, instruction, targetInstructions);
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        EmitILForInlineVar(ilGenerator, instruction);
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                        EmitILForInlineMember(ilGenerator, instruction);
                        break;
                    default:
                        throw new NotSupportedException(instruction.OpCode.OperandType.ToString());
                }
            }

#if TRACE
            var ilBytes = ilGenerator.GetILBytes();
            var browsableDynamicMethod = new BrowsableDynamicMethod(dynamicMethod, new DynamicMethodBody(ilBytes, locals));
            Console.WriteLine("\n" + dynamicMethod);

            foreach (var instruction in browsableDynamicMethod.GetInstructions())
            {
                Console.WriteLine(instruction);
            }
#endif
            return dynamicMethod;
        }

        private void EmitILForExceptionHandlers(ILGenerator ilGenerator, Instruction instruction, IReadOnlyCollection<ExceptionHandler> handlers)
        {
            var tryBlocks = handlers.Where(h => h.TryStart == instruction.Offset).GroupBy(h => h.TryEnd);
            foreach (var tryBlock in tryBlocks)
            {
                ilGenerator.BeginExceptionBlock();
                _exceptionBlockLevel++;
            }

            var filterBlock = handlers.FirstOrDefault(h => h.FilterStart == instruction.Offset);
            if (filterBlock != null)
            {
                ilGenerator.BeginExceptFilterBlock();
            }

            var handler = handlers.FirstOrDefault(h => h.HandlerEnd == instruction.Offset);
            if (handler != null)
            {
                if (handler.Flags == ExceptionHandlingClauseOptions.Finally)
                {
                    // Finally blocks are always the last handler
                    ilGenerator.EndExceptionBlock();
                    _exceptionBlockLevel--;
                }
                else if (handler.HandlerEnd == handlers.Where(h => h.TryStart == handler.TryStart && h.TryEnd == handler.TryEnd).Max(h => h.HandlerEnd))
                {
                    // We're dealing with the last catch block
                    ilGenerator.EndExceptionBlock();
                    _exceptionBlockLevel--;
                }
            }

            var catchOrFinallyBlock = handlers.FirstOrDefault(h => h.HandlerStart == instruction.Offset);
            if (catchOrFinallyBlock != null)
            {
                if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Clause)
                {
                    ilGenerator.BeginCatchBlock(catchOrFinallyBlock.CatchType);
                }
                else if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Filter)
                {
                    ilGenerator.BeginCatchBlock(null);
                }
                else if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Finally)
                {
                    ilGenerator.BeginFinallyBlock();
                }
                else
                {
                    // No support for fault blocks
                    throw new NotSupportedException();
                }
            }
        }

        private void EmitThisPointerAccessForBoxedValueType(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Call, Unbox.MakeGenericMethod(_method.DeclaringType));
        }

        private void EmitILForInlineNone(ILGenerator ilGenerator, Instruction instruction)
        {
            ilGenerator.Emit(instruction.OpCode);
            if (_isInterfaceDispatch && _owningType.IsValueType && instruction.OpCode == OpCodes.Ldarg_0)
            {
                EmitThisPointerAccessForBoxedValueType(ilGenerator);
            }
        }

        private void EmitILForInlineI(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (int)instruction.Operand);

        private void EmitILForInlineI8(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (long)instruction.Operand);

        private void EmitILForShortInlineI(ILGenerator ilGenerator, Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Ldc_I4_S)
                ilGenerator.Emit(instruction.OpCode, (sbyte)instruction.Operand);
            else
                ilGenerator.Emit(instruction.OpCode, (byte)instruction.Operand);
        }

        private void EmitILForInlineR(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (double)instruction.Operand);

        private void EmitILForShortInlineR(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (float)instruction.Operand);

        private void EmitILForInlineString(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (string)instruction.Operand);

        private void EmitILForInlineBrTarget(ILGenerator ilGenerator, Instruction instruction, IReadOnlyDictionary<int, Label> targetInstructions)
        {
            var targetLabel = targetInstructions[(instruction.Operand as Instruction).Offset];

            var opCode = instruction.OpCode;

            // Offset values could change and not be short form anymore
            if (opCode == OpCodes.Br_S) opCode = OpCodes.Br;
            else if (opCode == OpCodes.Brfalse_S) opCode = OpCodes.Brfalse;
            else if (opCode == OpCodes.Brtrue_S) opCode = OpCodes.Brtrue;
            else if (opCode == OpCodes.Beq_S) opCode = OpCodes.Beq;
            else if (opCode == OpCodes.Bge_S) opCode = OpCodes.Bge;
            else if (opCode == OpCodes.Bgt_S) opCode = OpCodes.Bgt;
            else if (opCode == OpCodes.Ble_S) opCode = OpCodes.Ble;
            else if (opCode == OpCodes.Blt_S) opCode = OpCodes.Blt;
            else if (opCode == OpCodes.Bne_Un_S) opCode = OpCodes.Bne_Un;
            else if (opCode == OpCodes.Bge_Un_S) opCode = OpCodes.Bge_Un;
            else if (opCode == OpCodes.Bgt_Un_S) opCode = OpCodes.Bgt_Un;
            else if (opCode == OpCodes.Ble_Un_S) opCode = OpCodes.Ble_Un;
            else if (opCode == OpCodes.Blt_Un_S) opCode = OpCodes.Blt_Un;
            else if (opCode == OpCodes.Leave_S) opCode = OpCodes.Leave;

            // Check if 'Leave' opcode is being used in an exception block,
            // only emit it if that's not the case
            if (opCode == OpCodes.Leave && _exceptionBlockLevel > 0) return;

            ilGenerator.Emit(opCode, targetLabel);
        }

        private void EmitILForInlineSwitch(ILGenerator ilGenerator,
            Instruction instruction, Dictionary<int, Label> targetInstructions)
        {
            var switchInstructions = (Instruction[])instruction.Operand;
            var targetLabels = new Label[switchInstructions.Length];
            for (var i = 0; i < switchInstructions.Length; i++)
                targetLabels[i] = targetInstructions[switchInstructions[i].Offset];
            ilGenerator.Emit(instruction.OpCode, targetLabels);
        }

        private void EmitILForInlineVar(ILGenerator ilGenerator, Instruction instruction)
        {
            var index = 0;
            if (instruction.OpCode.Name.Contains("loc"))
            {
                index = ((LocalVariableInfo)instruction.Operand).LocalIndex;
            }
            else
            {
                index = ((ParameterInfo)instruction.Operand).Position;
                index += _method.IsStatic ? 0 : 1;
            }

            if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                ilGenerator.Emit(instruction.OpCode, (byte)index);
            else
                ilGenerator.Emit(instruction.OpCode, (ushort)index);

            if (_isInterfaceDispatch && _owningType.IsValueType && instruction.OpCode.Name.StartsWith("ldarg") && index == 0)
            {
                EmitThisPointerAccessForBoxedValueType(ilGenerator);
            }
        }

        private void EmitILForType(ILGenerator ilGenerator, Instruction instruction, TypeInfo typeInfo)
        {
            if (instruction.OpCode == OpCodes.Constrained)
            {
                _constrainedType = typeInfo;
                return;
            }

            ilGenerator.Emit(instruction.OpCode, typeInfo);
        }

        private static bool ShouldForward(MethodBase member)
        {
            var declaringType = member.DeclaringType ?? throw new Exception($"Type {member.Name} does not have a {nameof(MethodBase.DeclaringType)}");

            // Don't attempt to rewrite inaccessible constructors in System.Private.CoreLib/mscorlib
            if (!declaringType.IsPublic) return true;
            if (!member.IsPublic && !member.IsFamily && !member.IsFamilyOrAssembly) return true;

            return false;
        }
        
        private void EmitILForConstructor(ILGenerator ilGenerator, Instruction instruction, ConstructorInfo constructorInfo)
        {
            if (constructorInfo.InCoreLibrary())
            {
                // Don't attempt to rewrite inaccessible constructors in System.Private.CoreLib/mscorlib
                if (ShouldForward(constructorInfo)) goto forward;
            }

            if (instruction.OpCode == OpCodes.Call)
            {
                //ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(constructorInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Newobj)
            {
                //ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForObjectInitialization(constructorInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldftn)
            {
                //ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(constructorInfo));
                return;
            }

            // If we get here, then we haven't accounted for an opcode.
            // Throw exception to make this obvious.
            throw new NotSupportedException(instruction.OpCode.Name);

        forward:
            ilGenerator.Emit(instruction.OpCode, constructorInfo);
        }

        private void EmitILForMethod(ILGenerator ilGenerator, Instruction instruction, MethodInfo methodInfo)
        {
            if (methodInfo.InCoreLibrary())
            {
                // Don't attempt to rewrite inaccessible methods in System.Private.CoreLib/mscorlib
                if (ShouldForward(methodInfo)) goto forward;
            }

            if (instruction.OpCode == OpCodes.Call)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Callvirt)
            {
                if (_constrainedType != null)
                {
                    ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualCall(methodInfo, _constrainedType));
                    _constrainedType = null;
                    return;
                }

                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualCall(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldftn)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldvirtftn)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualLoad(methodInfo));
                return;
            }

        forward:
            ilGenerator.Emit(instruction.OpCode, methodInfo);
        }

        private void EmitILForInlineMember(ILGenerator ilGenerator, Instruction instruction)
        {
            var memberInfo = (MemberInfo)instruction.Operand;
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                ilGenerator.Emit(instruction.OpCode, memberInfo as FieldInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.TypeInfo
                || memberInfo.MemberType == MemberTypes.NestedType)
            {
                EmitILForType(ilGenerator, instruction, memberInfo as TypeInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Constructor)
            {
                EmitILForConstructor(ilGenerator, instruction, memberInfo as ConstructorInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Method)
            {
                EmitILForMethod(ilGenerator, instruction, memberInfo as MethodInfo);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}