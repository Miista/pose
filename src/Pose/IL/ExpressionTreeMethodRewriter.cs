using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Reflection;
using Pose.Exceptions;
using Pose.Helpers;

namespace Pose.IL
{
    internal class ExpressionTreeMethodRewriter
    {
        private static readonly List<OpCode> IgnoredOpCodes = new List<OpCode> { OpCodes.Endfilter, OpCodes.Endfinally };

        private static readonly MethodInfo Unbox =
            typeof(Unsafe).GetMethod(nameof(Unsafe.Unbox))
            ?? throw new Exception($"Cannot get method {nameof(Unsafe.Unbox)} from type {nameof(Unsafe)}");

        private readonly MethodBase _method;
        private readonly Type _owningType;
        private readonly bool _isInterfaceDispatch;

        private readonly object _instance;
        
        private int _exceptionBlockLevel;
        private TypeInfo _constrainedType;

        private ExpressionTreeMethodRewriter(MethodBase method, Type owningType, bool isInterfaceDispatch, object instance)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _owningType = owningType ?? throw new ArgumentNullException(nameof(owningType));
            _isInterfaceDispatch = isInterfaceDispatch;
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public static ExpressionTreeMethodRewriter CreateRewriter(MethodBase method, bool isInterfaceDispatch, object instance)
        {
            return new ExpressionTreeMethodRewriter(
                method: method,
                owningType: method.DeclaringType,
                isInterfaceDispatch: isInterfaceDispatch,
                instance: instance
            );
        }
        
        public Delegate Rewrite()
        {
            State state = new State(_method);
            var instructions = _method.GetInstructions();
            var targets = new Dictionary<int, LabelTarget>();

            var ifTargets = instructions
                .Where(i => (i.Operand as Instruction) != null)
                .Select(i => (i.Operand as Instruction));

            foreach (Instruction instruction in ifTargets)
                targets.TryAdd(instruction.Offset, Expression.Label());

            foreach (var instruction in instructions)
            {
                Console.WriteLine(instruction);

                if (targets.TryGetValue(instruction.Offset, out LabelTarget label))
                {
                    state.Body.Add(
                        Expression.Label(label)
                    );
                }

                if (instruction.OpCode == OpCodes.Nop ||
                    instruction.OpCode == OpCodes.Ldobj ||
                    instruction.OpCode.Name.Contains("Ldind"))
                {
                    // Do nothing
                }
                else if (instruction.OpCode == OpCodes.Pop)
                {
                    state.Stack.Pop();
                }
                else if (instruction.OpCode == OpCodes.Ldnull)
                {
                    TransformLdNull(state);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4 ||
                    instruction.OpCode == OpCodes.Ldc_I4_S)
                {
                    // TransformLdC(state, Convert.ToInt32((sbyte)instruction.Operand));
                    TransformLdC(state, instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I8 ||
                    instruction.OpCode == OpCodes.Ldc_R4 ||
                    instruction.OpCode == OpCodes.Ldc_R8)
                {
                    TransformLdC(state, instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_0)
                {
                    TransformLdC(state, 0);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_1)
                {
                    TransformLdC(state, 1);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_2)
                {
                    TransformLdC(state, 2);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_3)
                {
                    TransformLdC(state, 3);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_4)
                {
                    TransformLdC(state, 4);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_5)
                {
                    TransformLdC(state, 5);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_6)
                {
                    TransformLdC(state, 6);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_7)
                {
                    TransformLdC(state, 7);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_8)
                {
                    TransformLdC(state, 8);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4_M1)
                {
                    TransformLdC(state, -1);
                }
                else if (instruction.OpCode == OpCodes.Ldstr)
                {
                    TransformLdStr(state, (string)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldarg ||
                        instruction.OpCode == OpCodes.Ldarg_S ||
                        instruction.OpCode == OpCodes.Ldarga ||
                        instruction.OpCode == OpCodes.Ldarga_S)
                {
                    int index = ((ParameterInfo)instruction.Operand).Position;
                    TransformLdArg(state, _method.IsStatic ? index : index + 1);
                }
                else if (instruction.OpCode == OpCodes.Ldarg_0)
                {
                    TransformLdArg(state, 0);
                }
                else if (instruction.OpCode == OpCodes.Ldarg_1)
                {
                    TransformLdArg(state, 1);
                }
                else if (instruction.OpCode == OpCodes.Ldarg_2)
                {
                    TransformLdArg(state, 2);
                }
                else if (instruction.OpCode == OpCodes.Ldarg_3)
                {
                    TransformLdArg(state, 3);
                }
                else if (instruction.OpCode == OpCodes.Ldloc ||
                        instruction.OpCode == OpCodes.Ldloc_S ||
                        instruction.OpCode == OpCodes.Ldloca ||
                        instruction.OpCode == OpCodes.Ldloca_S)
                {
                    TransformLdLoc(state, ((LocalVariableInfo)instruction.Operand).LocalIndex);
                }
                else if (instruction.OpCode == OpCodes.Ldloc_0)
                {
                    TransformLdLoc(state, 0);
                }
                else if (instruction.OpCode == OpCodes.Ldloc_1)
                {
                    TransformLdLoc(state, 1);
                }
                else if (instruction.OpCode == OpCodes.Ldloc_2)
                {
                    TransformLdLoc(state, 2);
                }
                else if (instruction.OpCode == OpCodes.Ldloc_3)
                {
                    TransformLdLoc(state, 3);
                }
                else if (instruction.OpCode == OpCodes.Starg ||
                        instruction.OpCode == OpCodes.Starg_S)
                {
                    int index = ((ParameterInfo)instruction.Operand).Position;
                    TransformStArg(state, _method.IsStatic ? index : index + 1);
                }
                else if (instruction.OpCode == OpCodes.Stloc ||
                        instruction.OpCode == OpCodes.Stloc_S)
                {
                    TransformStLoc(state, ((LocalVariableInfo)instruction.Operand).LocalIndex);
                }
                else if (instruction.OpCode == OpCodes.Stloc_0)
                {
                    TransformStLoc(state, 0);
                }
                else if (instruction.OpCode == OpCodes.Stloc_1)
                {
                    TransformStLoc(state, 1);
                }
                else if (instruction.OpCode == OpCodes.Stloc_2)
                {
                    TransformStLoc(state, 2);
                }
                else if (instruction.OpCode == OpCodes.Stloc_3)
                {
                    TransformStLoc(state, 3);
                }
                else if (instruction.OpCode == OpCodes.Add ||
                        instruction.OpCode == OpCodes.Add_Ovf ||
                        instruction.OpCode == OpCodes.Add_Ovf_Un)
                {
                    TransformAdd(state, instruction.OpCode.Name.Contains("Ovf"));
                }
                else if (instruction.OpCode == OpCodes.Sub ||
                        instruction.OpCode == OpCodes.Sub_Ovf ||
                        instruction.OpCode == OpCodes.Sub_Ovf_Un)
                {
                    TransformSub(state, instruction.OpCode.Name.Contains("Ovf"));
                }
                else if (instruction.OpCode == OpCodes.Mul ||
                        instruction.OpCode == OpCodes.Mul_Ovf ||
                        instruction.OpCode == OpCodes.Mul_Ovf_Un)
                {
                    TransformMul(state, instruction.OpCode.Name.Contains("Ovf"));
                }
                else if (instruction.OpCode == OpCodes.Div ||
                        instruction.OpCode == OpCodes.Div_Un)
                {
                    TransformDiv(state);
                }
                else if (instruction.OpCode == OpCodes.Rem ||
                        instruction.OpCode == OpCodes.Rem_Un)
                {
                    TransformRem(state);
                }
                else if (instruction.OpCode == OpCodes.Ceq)
                {
                    TransformCeq(state);
                }
                else if (instruction.OpCode == OpCodes.Neg)
                {
                    TransformNeg(state);
                }
                else if (instruction.OpCode == OpCodes.Cgt ||
                        instruction.OpCode == OpCodes.Cgt_Un)
                {
                    TransformCgt(state);
                }
                else if (instruction.OpCode == OpCodes.Clt ||
                        instruction.OpCode == OpCodes.Clt_Un)
                {
                    TransformClt(state);
                }
                else if (instruction.OpCode == OpCodes.Shl)
                {
                    TransformShl(state);
                }
                else if (instruction.OpCode == OpCodes.Shr ||
                        instruction.OpCode == OpCodes.Shr_Un)
                {
                    TransformShr(state);
                }
                else if (instruction.OpCode == OpCodes.And)
                {
                    TransformAnd(state);
                }
                else if (instruction.OpCode == OpCodes.Or)
                {
                    TransformOr(state);
                }
                else if (instruction.OpCode == OpCodes.Xor)
                {
                    TransformXor(state);
                }
                else if (instruction.OpCode == OpCodes.Not)
                {
                    TransformNot(state);
                }
                else if (instruction.OpCode == OpCodes.Throw)
                {
                    TransformThrow(state);
                }
                else if (instruction.OpCode == OpCodes.Rethrow)
                {
                    TransformRethrow(state);
                }
                else if (instruction.OpCode == OpCodes.Castclass)
                {
                    TransformCastClass(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Dup)
                {
                    TransformDup(state);
                }
                else if (instruction.OpCode == OpCodes.Conv_I ||
                        instruction.OpCode == OpCodes.Conv_U)
                {
                    TransformConv(state, typeof(IntPtr), false);
                }
                else if (instruction.OpCode == OpCodes.Conv_I1 ||
                        instruction.OpCode == OpCodes.Conv_I2 ||
                        instruction.OpCode == OpCodes.Conv_I4 ||
                        instruction.OpCode == OpCodes.Conv_U1 ||
                        instruction.OpCode == OpCodes.Conv_U2 ||
                        instruction.OpCode == OpCodes.Conv_U4)
                {
                    TransformConv(state, typeof(int), false);
                }
                else if (instruction.OpCode == OpCodes.Conv_I8 ||
                        instruction.OpCode == OpCodes.Conv_U8)
                {
                    TransformConv(state, typeof(long), false);
                }
                else if (instruction.OpCode == OpCodes.Conv_R4 ||
                        instruction.OpCode == OpCodes.Conv_R_Un)
                {
                    TransformConv(state, typeof(float), false);
                }
                else if (instruction.OpCode == OpCodes.Conv_R8)
                {
                    TransformConv(state, typeof(double), false);
                }
                else if (instruction.OpCode == OpCodes.Conv_Ovf_I ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U_Un)
                {
                    TransformConv(state, typeof(IntPtr), true);
                }
                else if (instruction.OpCode == OpCodes.Conv_Ovf_I1 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I1_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I2 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I2_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I4 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I4_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U1 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U1_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U2 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U2_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U4 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U4_Un)
                {
                    TransformConv(state, typeof(int), true);
                }
                else if (instruction.OpCode == OpCodes.Conv_Ovf_I8 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_I8_Un ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U8 ||
                        instruction.OpCode == OpCodes.Conv_Ovf_U8_Un)
                {
                    TransformConv(state, typeof(long), true);
                }
                else if (instruction.OpCode == OpCodes.Box)
                {
                    TransformBox(state);
                }
                else if (instruction.OpCode == OpCodes.Unbox ||
                        instruction.OpCode == OpCodes.Unbox_Any)
                {
                    TransformUnbox(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Newarr)
                {
                    TransformNewarr(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldelem ||
                        instruction.OpCode == OpCodes.Ldelem_I ||
                        instruction.OpCode == OpCodes.Ldelem_I1 ||
                        instruction.OpCode == OpCodes.Ldelem_I2 ||
                        instruction.OpCode == OpCodes.Ldelem_I4 ||
                        instruction.OpCode == OpCodes.Ldelem_I8 ||
                        instruction.OpCode == OpCodes.Ldelem_U1 ||
                        instruction.OpCode == OpCodes.Ldelem_U2 ||
                        instruction.OpCode == OpCodes.Ldelem_U4 ||
                        instruction.OpCode == OpCodes.Ldelem_R4 ||
                        instruction.OpCode == OpCodes.Ldelem_R8 ||
                        instruction.OpCode == OpCodes.Ldelem_Ref ||
                        instruction.OpCode == OpCodes.Ldelema)

                {
                    TransformLdelem(state);
                }
                else if (instruction.OpCode == OpCodes.Stelem ||
                        instruction.OpCode == OpCodes.Stelem_I ||
                        instruction.OpCode == OpCodes.Stelem_I1 ||
                        instruction.OpCode == OpCodes.Stelem_I2 ||
                        instruction.OpCode == OpCodes.Stelem_I4 ||
                        instruction.OpCode == OpCodes.Stelem_I8 ||
                        instruction.OpCode == OpCodes.Stelem_R4 ||
                        instruction.OpCode == OpCodes.Stelem_R8 ||
                        instruction.OpCode == OpCodes.Stelem_Ref)

                {
                    TransformStelem(state);
                }
                else if (instruction.OpCode == OpCodes.Ldtoken)
                {
                    TransformLdtoken(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldlen)
                {
                    TransformLdlen(state);
                }
                else if (instruction.OpCode == OpCodes.Br ||
                        instruction.OpCode == OpCodes.Br_S)

                {
                    TransformBr(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Brtrue ||
                        instruction.OpCode == OpCodes.Brtrue_S)
                {
                    TransformBrTrue(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Brfalse ||
                        instruction.OpCode == OpCodes.Brfalse_S)
                {
                    TransformBrFalse(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Beq ||
                        instruction.OpCode == OpCodes.Beq_S)

                {
                    TransformBeq(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Bne_Un ||
                        instruction.OpCode == OpCodes.Bne_Un_S)

                {
                    TransformBne(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Bge ||
                        instruction.OpCode == OpCodes.Bge_S ||
                        instruction.OpCode == OpCodes.Bge_Un ||
                        instruction.OpCode == OpCodes.Bge_Un_S)

                {
                    TransformBge(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Bgt ||
                        instruction.OpCode == OpCodes.Bgt_S ||
                        instruction.OpCode == OpCodes.Bgt_Un ||
                        instruction.OpCode == OpCodes.Bgt_Un_S)

                {
                    TransformBgt(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Ble ||
                        instruction.OpCode == OpCodes.Ble_S ||
                        instruction.OpCode == OpCodes.Ble_Un ||
                        instruction.OpCode == OpCodes.Ble_Un_S)

                {
                    TransformBle(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Blt ||
                        instruction.OpCode == OpCodes.Blt_S ||
                        instruction.OpCode == OpCodes.Blt_Un ||
                        instruction.OpCode == OpCodes.Blt_Un_S)

                {
                    TransformBlt(state, (Instruction)instruction.Operand, targets);
                }
                else if (instruction.OpCode == OpCodes.Newobj)
                {
                    TransformNewobj(state, (ConstructorInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Initobj)
                {
                    TransformInitobj(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Isinst)
                {
                    TransformIsInst(state, (Type)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldfld ||
                        instruction.OpCode == OpCodes.Ldflda)
                {
                    TransformLdfld(state, (FieldInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldsfld ||
                        instruction.OpCode == OpCodes.Ldsflda)
                {
                    TransformLdsfld(state, (FieldInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Stfld)
                {
                    TransformStfld(state, (FieldInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Stsfld)
                {
                    TransformStsfld(state, (FieldInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Call ||
                        instruction.OpCode == OpCodes.Callvirt)
                {
                    TransformCall(state, (MemberInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ldftn ||
                        instruction.OpCode == OpCodes.Ldvirtftn)
                {
                    TransformLdftn(state, (MethodInfo)instruction.Operand);
                }
                else if (instruction.OpCode == OpCodes.Ret)
                {
                    TransformRet(state);
                }
                else
                {
                    throw new NotImplementedException(instruction.OpCode.Name);
                }
            }

            var body = Expression.Block(
                state.Variables,
                state.Body
            );

            return Expression.Lambda(body, state.Arguments).Compile();
        }

        private void TransformLdNull(State state)
        {
            state.Stack.Push(Expression.Constant(null));
        }

        private void TransformLdC(State state, object v)
        {
            state.Stack.Push(Expression.Constant(v, v.GetType()));
        }

        private void TransformLdStr(State state, string str)
        {
            state.Stack.Push(Expression.Constant(str, typeof(string)));
        }

        private void TransformLdArg(State state, int index)
        {
            state.Stack.Push(state.Arguments[index]);
        }

        private void TransformStArg(State state, int index)
        {
            state.Stack.Push(
                Expression.Assign(
                    state.Arguments[index],
                    state.Stack.Pop()
                )
            );
        }

        private void TransformLdLoc(State state, int index)
        {
            state.Stack.Push(state.Variables[index]);
        }

        private void TransformStLoc(State state, int index)
        {
            state.Body.Add(
                Expression.Assign(
                    state.Variables[index],
                    state.Stack.Pop()
                )
            );
        }

        private void TransformAdd(State state, bool @checked)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            if (@checked)
            {
                state.Stack.Push(
                    Expression.AddChecked(
                        left, right
                    )
                );
            }
            else
            {
                state.Stack.Push(
                    Expression.Add(
                        left, right
                    )
                );
            }
        }

        private void TransformSub(State state, bool @checked)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            if (@checked)
            {
                state.Stack.Push(
                    Expression.SubtractChecked(
                        left, right
                    )
                );
            }
            else
            {
                state.Stack.Push(
                    Expression.Add(
                        left, right
                    )
                );
            }
        }

        private void TransformMul(State state, bool @checked)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            if (@checked)
            {
                state.Stack.Push(
                    Expression.MultiplyChecked(
                        left, right
                    )
                );
            }
            else
            {
                state.Stack.Push(
                    Expression.Multiply(
                        left, right
                    )
                );
            }
        }

        private void TransformDiv(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.Divide(
                    left, right
                )
            );
        }

        private void TransformRem(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.Modulo(
                    left, right
                )
            );
        }

        private void TransformNeg(State state)
        {
            state.Stack.Push(
                Expression.Negate(
                    state.Stack.Pop()
                )
            );
        }

        private void TransformCeq(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.Equal(left, right)
            );
        }

        private void TransformCgt(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.GreaterThan(left, right)
            );
        }

        private void TransformClt(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.LessThan(left, right)
            );
        }

        private void TransformShl(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.LeftShift(left, right)
            );
        }

        private void TransformShr(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.RightShift(left, right)
            );
        }

        private void TransformAnd(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.And(
                    left, right
                )
            );
        }

        private void TransformOr(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.Or(
                    left, right
                )
            );
        }

        private void TransformXor(State state)
        {
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Stack.Push(
                Expression.ExclusiveOr(
                    left, right
                )
            );
        }

        private void TransformNot(State state)
        {
            state.Stack.Push(
                Expression.Not(state.Stack.Pop())
            );
        }

        private void TransformThrow(State state)
        {
            state.Body.Add(
                Expression.Throw(state.Stack.Pop())
            );
        }

        private void TransformRethrow(State state)
        {
            state.Body.Add(
                Expression.Rethrow()
            );
        }

        private void TransformCastClass(State state, Type type)
        {
            state.Stack.Push(
                Expression.Convert(
                    state.Stack.Pop(),
                    type
                )
            );
        }

        private void TransformDup(State state)
        {
            var value = state.Stack.Peek();
            if (value.NodeType == ExpressionType.NewArrayBounds ||
                value.NodeType == ExpressionType.NewArrayInit)
            {
                value = state.Stack.Pop();
                var variable = Expression.Variable(value.Type);
                state.Variables.Add(variable);

                state.Body.Add(
                    Expression.Assign(variable, value)
                );

                state.Stack.Push(variable);
                state.Stack.Push(variable);
                return;
            }

            state.Stack.Push(value);
        }

        private void TransformConv(State state, Type type, bool @checked)
        {
            if (@checked)
            {
                state.Stack.Push(
                    Expression.ConvertChecked(
                        state.Stack.Pop(),
                        type
                    )
                );
            }
            else
            {
                state.Stack.Push(
                    Expression.Convert(
                        state.Stack.Pop(),
                        type
                    )
                );
            }
        }

        private void TransformBox(State state)
        {
            state.Stack.Push(
                Expression.Convert(
                    state.Stack.Pop(),
                    typeof(object)
                )
            );
        }

        private void TransformUnbox(State state, Type type)
        {
            state.Stack.Push(
                Expression.Unbox(
                    state.Stack.Pop(),
                    type
                )
            );
        }

        private void TransformNewarr(State state, Type type)
        {
            state.Stack.Push(
                Expression.NewArrayBounds(
                    type,
                    state.Stack.Pop()
                )
            );
        }

        private void TransformLdelem(State state)
        {
            var index = state.Stack.Pop();
            var array = state.Stack.Pop();
            state.Stack.Push(
                Expression.ArrayIndex(
                    array,
                    index
                )
            );
        }

        private void TransformStelem(State state)
        {
            var value = state.Stack.Pop();
            var index = state.Stack.Pop();
            var array = state.Stack.Pop();

            state.Body.Add(
                Expression.Assign(
                    Expression.ArrayAccess(
                        array,
                        new[] { index }
                    ),
                    value
                )
            );
        }

        private void TransformLdtoken(State state, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                state.Stack.Push(
                    Expression.Constant(fieldInfo.FieldHandle)
                );
            }
            else if (memberInfo is Type type)
            {
                state.Stack.Push(
                    Expression.Constant(type.TypeHandle)
                );
            }
            else if (memberInfo is MethodBase methodBase)
            {
                state.Stack.Push(
                    Expression.Constant(methodBase.MethodHandle)
                );
            }
        }

        private void TransformLdlen(State state)
        {
            state.Stack.Push(
                Expression.ArrayLength(
                    state.Stack.Pop()
                )
            );
        }

        private void TransformBr(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            state.Body.Add(
                Expression.Goto(labelTarget)
            );
        }

        private void TransformBrTrue(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var value = state.Stack.Pop();

            if (value.Type == typeof(bool))
            {
                state.Body.Add(
                    Expression.IfThen(
                        Expression.IsTrue(value),
                        Expression.Goto(labelTarget)
                    )
                );

                return;
            }

            state.Body.Add(
                Expression.IfThen(
                    Expression.NotEqual(
                        value,
                        Expression.Constant(null)
                    ),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBrFalse(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var value = state.Stack.Pop();

            if (value.Type == typeof(bool))
            {
                state.Body.Add(
                    Expression.IfThen(
                        Expression.IsFalse(value),
                        Expression.Goto(labelTarget)
                    )
                );

                return;
            }

            state.Body.Add(
                Expression.IfThen(
                    Expression.Equal(
                        value,
                        Expression.Constant(null)
                    ),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBeq(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            state.Body.Add(
                Expression.IfThen(
                    Expression.Equal(state.Stack.Pop(), state.Stack.Pop()),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBne(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            state.Body.Add(
                Expression.IfThen(
                    Expression.NotEqual(state.Stack.Pop(), state.Stack.Pop()),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBge(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Body.Add(
                Expression.IfThen(
                    Expression.GreaterThanOrEqual(left, right),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBgt(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Body.Add(
                Expression.IfThen(
                    Expression.GreaterThan(left, right),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBle(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Body.Add(
                Expression.IfThen(
                    Expression.LessThanOrEqual(left, right),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformBlt(State state, Instruction instruction, Dictionary<int, LabelTarget> targets)
        {
            LabelTarget labelTarget = targets[instruction.Offset];
            var right = state.Stack.Pop();
            var left = state.Stack.Pop();

            state.Body.Add(
                Expression.IfThen(
                    Expression.LessThan(left, right),
                    Expression.Goto(labelTarget)
                )
            );
        }

        private void TransformNewobj(State state, ConstructorInfo constructorInfo)
        {
            List<Expression> args = new List<Expression>();
            for (int i = 0; i < constructorInfo.GetParameters().Length; i++)
            {
                args.Add(
                    state.Stack.Pop()
                );
            }

            args.Reverse();

            var shim = PoseContext.Shims.FirstOrDefault(s => s.Original == constructorInfo);

            if (shim == null)
            {
                state.Stack.Push(
                    Expression.New(
                        constructorInfo,
                        args
                    )
                );
            }
            else
            {
                state.Stack.Push(
                    Expression.Invoke(
                        shim.ReplacementExpression
                    )
                );
            }

        }

        private void TransformInitobj(State state, Type type)
        {
            state.Stack.Pop();
        }

        private void TransformIsInst(State state, Type type)
        {
            state.Stack.Push(
                Expression.TypeAs(
                    state.Stack.Pop(),
                    type
                )
            );
        }

        private void TransformLdfld(State state, FieldInfo fieldInfo)
        {
            state.Stack.Push(
                Expression.MakeMemberAccess(state.Stack.Pop(), fieldInfo)
            );
        }

        private void TransformLdsfld(State state, FieldInfo fieldInfo)
        {
            state.Stack.Push(
                Expression.MakeMemberAccess(null, fieldInfo)
            );
        }

        private void TransformStfld(State state, FieldInfo fieldInfo)
        {
            var val = state.Stack.Pop();
            var obj = state.Stack.Pop();
            state.Body.Add(
                Expression.Assign(
                    Expression.MakeMemberAccess(obj, fieldInfo),
                    val
                )
            );
        }

        private void TransformStsfld(State state, FieldInfo fieldInfo)
        {
            state.Body.Add(
                Expression.Assign(
                    Expression.MakeMemberAccess(null, fieldInfo),
                    state.Stack.Pop()
                )
            );
        }

        private void TransformCall(State state, MemberInfo memberInfo)
        {
            List<Expression> args = new List<Expression>();
            if (memberInfo is ConstructorInfo constructorInfo)
            {
                for (int i = 0; i < constructorInfo.GetParameters().Length; i++)
                {
                    args.Add(
                        state.Stack.Pop()
                    );
                }

                args.Reverse();
                state.Body.Add(
                    Expression.New(constructorInfo, args)
                );

                return;
            }

            MethodInfo methodInfo = memberInfo as MethodInfo;
            for (int i = 0; i < methodInfo.GetParameters().Length; i++)
            {
                args.Add(
                    state.Stack.Pop()
                );
            }

            args.Reverse();
            Expression instance = (methodInfo.IsStatic) ? null : state.Stack.Pop();

            var matchingShim = PoseContext.Shims.FirstOrDefault(s => s.Original == memberInfo);

            // The method is void, thus we should not add anything to the stack.
            if (methodInfo.ReturnType == typeof(void))
            {
                if (matchingShim == null)
                {
                    // Call the regular method
                    state.Body.Add(
                        Expression.Call(instance, methodInfo, args)
                    );
                }
                else
                {
                    state.Body.Add(
                        Expression.Invoke(
                            matchingShim.ReplacementExpression,
                            !methodInfo.IsStatic ? args.Prepend(instance) : args
                        )
                    );
                }
            }
            else
            {
                if (matchingShim == null)
                {
                    // Call the regular method
                    state.Stack.Push(
                        Expression.Call(instance, methodInfo, args)
                    );
                }
                else
                {
                    if (matchingShim.ReplacementExpression.NodeType != ExpressionType.Lambda)
                        throw new Exception("The replacement expression must be a lambda expression");

                    if (matchingShim.IsInstanceSpecific)
                    {
                        var matchVar = Expression.Variable(typeof(bool), "match");
                        var parameterExpression = Expression.Parameter(_instance.GetType(), "instance");
                        // var lambdaExpression =
                        //     Expression.Lambda(
                        //         Expression.Block(
                        //             new[] { matchVar },
                        //             Expression.IfThenElse(
                        //
                        //                 // Check whether the given instance matches the shim's expected instance 
                        //                 Expression.ReferenceEqual(
                        //                     Expression.Constant(matchingShim.Instance),
                        //                     instance
                        //                 ),
                        //                 Expression.Assign(matchVar, Expression.Constant(true)),
                        //                 Expression.Assign(matchVar, Expression.Constant(false))
                        //             ),
                        //             matchVar
                        //         ),
                        //         parameterExpression
                        //     );
                        var lambdaExpression =
                            Expression.Lambda(
                                Expression.Block(
                                    new[] { matchVar },
                                    Expression.IfThenElse(
                        
                                        // Check whether the given instance matches the shim's expected instance 
                                        Expression.ReferenceEqual(
                                            Expression.Constant(matchingShim.Instance),
                                            instance
                                        ),
                                        Expression.Assign(matchVar, Expression.Constant(true)),
                                        Expression.Assign(matchVar, Expression.Constant(false))
                                    ),
                                    matchVar
                                ),
                                state.Arguments
                            );
                        var result = lambdaExpression.Compile().DynamicInvoke(_instance);

                        if (result is not bool instanceMatchesShimExpectedInstance) throw new Exception("Don't know how to handle this...");

                        if (instanceMatchesShimExpectedInstance)
                        {
                            // Invoke the shim
                            state.Stack.Push(
                                Expression.Invoke(
                                    matchingShim.ReplacementExpression,
                                    !methodInfo.IsStatic ? args.Prepend(instance) : args
                                )
                            );
                        }
                        else
                        {
                            // Call the regular method
                            state.Stack.Push(
                                Expression.Call(instance, methodInfo, args)
                            );
                        }
                    }
                    else
                    {
                        // The shim is not instance specific
                        state.Stack.Push(
                            Expression.Invoke(
                                matchingShim.ReplacementExpression,
                                !methodInfo.IsStatic ? args.Prepend(instance) : args
                            )
                        );
                    }
                }
            }
        }

        private void TransformLdftn(State state, MethodInfo methodInfo)
        {
            state.Stack.Push(
                Expression.Constant(methodInfo.MethodHandle.GetFunctionPointer())
            );
        }

        private void TransformRet(State state)
        {
            if (state.Stack.Count() > 0)
            {
                state.Body.Add(
                    state.Stack.Pop()
                );
            }
        }
    }
}