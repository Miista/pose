using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mono.Reflection;
using Pose.Exceptions;
using Pose.Extensions;
using Pose.Helpers;
using Pose.IL;

namespace Pose.Sandbox
{
  public class Program
  {
    internal static class StaticClass
    {
      public static int GetInt()
      {
        Console.WriteLine("(Static) Here");
        return 1;
      }
    }

    public static int GetInt() =>
      StaticClass.GetInt();

    public static async Task<int> DoWork2Async()
    {
        Console.WriteLine("Here");
        var x = await Task.FromResult(1);
        Console.WriteLine("Here 2");
        Console.WriteLine(x);
        return x;

        // Console.WriteLine("Here");
        // await Task.Delay(10000);
        // int result = GetInt();
        //
        // return await Task.FromResult(result);
    }

    public static async Task<int> DoWork1Async()
    {
      return GetInt();
    }

    private static Type GetStateMachineType<TOwningType>(string methodName)
    {
        var stateMachineType = typeof(TOwningType)
            .GetMethod(methodName)
            ?.GetCustomAttribute<AsyncStateMachineAttribute>()
            ?.StateMachineType;

        return stateMachineType;
    }
    
    private static void RunAsync<TOwningType>(string methodName)
    {
        var stateMachine = GetStateMachineType<TOwningType>(methodName);
        var copyType = CopyType(stateMachine);

        var methodInfo = copyType.GetMethod(nameof(IAsyncStateMachine.MoveNext));

        if (methodInfo != null)
        {

            var instance = Activator.CreateInstance(copyType);
            var builderField = copyType.GetField("<>t__builder") ?? throw new Exception("Cannot get builder field");
            builderField.SetValue(instance, AsyncTaskMethodBuilder<int>.Create());
            var stateField = copyType.GetField("<>1__state") ?? throw new Exception("Cannot get state field");
            stateField.SetValue(instance, -1);
            var startMethod = typeof(AsyncTaskMethodBuilder<int>).GetMethod(nameof(AsyncTaskMethodBuilder<int>.Start)) ?? throw new Exception("Cannot get start method");
            var genericMethod = startMethod.MakeGenericMethod(copyType);
            genericMethod.Invoke(builderField.GetValue(instance), new object[] { instance });

            var builder = builderField.GetValue(instance);
            var taskProperty = typeof(AsyncTaskMethodBuilder<int>).GetProperty("Task") ?? throw new Exception("Cannot get task property");
            var task = taskProperty.GetValue(builder) as Task<int> ?? throw new Exception("Cannot get task");
            var result = task.Result;

            Console.WriteLine(result);
        }

        Console.WriteLine("SUCCESS!");
    }

    
    private static MethodBase RewriteAsync<TOwningType>(string methodName)
    {
        var stateMachine = GetStateMachineType<TOwningType>(methodName);
        var copyType = CopyType(stateMachine);

        var methodInfo = copyType.GetMethod(nameof(IAsyncStateMachine.MoveNext));

        if (methodInfo != null)
        {

            var dynamicMethod = new DynamicMethod(
                name: StubHelper.CreateStubNameFromMethod("impl", methodInfo),
                returnType: methodInfo.ReturnType,
                parameterTypes: methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                m: StubHelper.GetOwningModule(),
                skipVisibility: true
            );

            var methodBody = methodInfo.GetMethodBody() ?? throw new MethodRewriteException($"Method {_method.Name} does not have a body");
            var locals = methodBody.LocalVariables;
            
            var ilGenerator = dynamicMethod.GetILGenerator();
            
            foreach (var local in locals)
            {
                ilGenerator.DeclareLocal(local.LocalType, local.IsPinned);
            }
            
            ilGenerator.Emit(OpCodes.Newobj, copyType);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            
            if (methodInfo.ReturnType == typeof(void))
            {
                var setResultMethod = typeof(AsyncTaskMethodBuilder).GetMethod(nameof(AsyncTaskMethodBuilder.SetResult));
                ilGenerator.Emit(OpCodes.Call, setResultMethod);
            }
            else
            {
                var setResultMethod = typeof(AsyncTaskMethodBuilder<>).MakeGenericType(methodInfo.ReturnType).GetMethod(nameof(AsyncTaskMethodBuilder.SetResult));
                ilGenerator.Emit(OpCodes.Call, setResultMethod);
            }
            
            ilGenerator.Emit(OpCodes.Stfld, copyType.GetField("<>t__builder"));
            
            var instance = Activator.CreateInstance(copyType);
            var builderField = copyType.GetField("<>t__builder") ?? throw new Exception("Cannot get builder field");
            builderField.SetValue(instance, AsyncTaskMethodBuilder<int>.Create());
            var stateField = copyType.GetField("<>1__state") ?? throw new Exception("Cannot get state field");
            stateField.SetValue(instance, -1);
            var startMethod = typeof(AsyncTaskMethodBuilder<int>).GetMethod(nameof(AsyncTaskMethodBuilder<int>.Start)) ?? throw new Exception("Cannot get start method");
            var genericMethod = startMethod.MakeGenericMethod(copyType);
            genericMethod.Invoke(builderField.GetValue(instance), new object[] { instance });

            var builder = builderField.GetValue(instance);
            var taskProperty = typeof(AsyncTaskMethodBuilder<int>).GetProperty("Task") ?? throw new Exception("Cannot get task property");
            var task = taskProperty.GetValue(builder) as Task<int> ?? throw new Exception("Cannot get task");
            var result = task.Result;

            Console.WriteLine(result);
        }

        Console.WriteLine("SUCCESS!");
    }
    
    public static async Task Main(string[] args)
    {
      Shim shim1 = Shim.Replace(() => StaticClass.GetInt()).With(() =>
      {
        Console.WriteLine("This actually works!!!");
        return 15;
      });

      Shim shim2 = Shim.Replace(() => GetInt()).With(() =>
      {
        Console.WriteLine("This actually works!!!");
        return 15;
      });

      // int result = await DoWork2Async();
      // Console.WriteLine($"Result 3: {result}");

      try
      {
          RewriteAsync<Program>(nameof(DoWork2Async));
      }
      catch (Exception e)
      {
        Console.WriteLine("FAILED!" + e.Message);
      }
      
      // Console.WriteLine("Fields");

      // try
      // {
      //     await PoseContext.Isolate(
      //         async () =>
      //         {
      //             int result = await DoWork2Async();
      //             Console.WriteLine($"Result 3: {result}");
      //         }, shim1, shim2);
      // }
      // catch (Exception e)
      // {
      //     Console.WriteLine(e);
      //     throw;
      // }
    }
    
    public static Type CopyType(Type stateMachine)
    {
        var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsyncAssembly"), AssemblyBuilderAccess.RunAndCollect);
        var mb = ab.DefineDynamicModule("AsyncModule");
        // var containerBuilder = mb.DefineType("AsyncMethodContainer", TypeAttributes.Class | TypeAttributes.Public);
        var tb = mb.DefineType($"{stateMachine.Name}__Rewrite", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
        tb.AddInterfaceImplementation(typeof(IAsyncStateMachine));
  
        var fields = stateMachine.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .ToList()
            .Select(f => tb.DefineField(f.Name, f.FieldType, FieldAttributes.Public))
            .ToArray();
        
        var fieldDict = fields.ToDictionary(f => f.Name);

        stateMachine.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .ToList()
            .ForEach(m =>
            {
                Console.WriteLine(m.Name);
                var _exceptionBlockLevel = 0;
                TypeInfo _constrainedType = null;
                
                var parameters = m.GetParameters().Select(p => p.ParameterType).ToArray();
                var meth = tb.DefineMethod(m.Name, MethodAttributes.Public | MethodAttributes.Virtual, m.ReturnType, parameters);

                // var methodRewriter = MethodRewriter.CreateRewriter(m, false);
                // var rewritten = methodRewriter.Rewrite();

                // generator.Emit(OpCodes.Call, (MethodInfo) rewritten);
                var methodBody = m.GetMethodBody() ?? throw new MethodRewriteException($"Method {m.Name} does not have a body");
                var locals = methodBody.LocalVariables;
                var targetInstructions = new Dictionary<int, Label>();
                var handlers = new List<ExceptionHandler>();
                
                var ilGenerator = meth.GetILGenerator();
                var instructions = m.GetInstructions();

                ilGenerator.Emit(OpCodes.Ldstr, "Hello World");
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
                
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
                
                foreach (var instruction in instructions)
                {
#if TRACE
                    Console.WriteLine(instruction);
#endif

                    // EmitILForExceptionHandlers(ref _exceptionBlockLevel, ilGenerator, instruction, handlers);

                    if (targetInstructions.TryGetValue(instruction.Offset, out var label))
                        ilGenerator.MarkLabel(label);

                    if (new []{ OpCodes.Endfilter, OpCodes.Endfinally }.Contains(instruction.OpCode)) continue;

                    switch (instruction.OpCode.OperandType)
                    {
                        case OperandType.InlineNone:
                            ilGenerator.Emit(instruction.OpCode);
                            break;
                        case OperandType.InlineI:
                            ilGenerator.Emit(instruction.OpCode, (int)instruction.Operand);
                            break;
                        case OperandType.InlineI8:
                            ilGenerator.Emit(instruction.OpCode, (long)instruction.Operand);
                            break;
                        case OperandType.ShortInlineI:
                            if (instruction.OpCode == OpCodes.Ldc_I4_S)
                                ilGenerator.Emit(instruction.OpCode, (sbyte)instruction.Operand);
                            else
                                ilGenerator.Emit(instruction.OpCode, (byte)instruction.Operand);
                            break;
                        case OperandType.InlineR:
                            ilGenerator.Emit(instruction.OpCode, (double)instruction.Operand);
                            break;
                        case OperandType.ShortInlineR:
                            ilGenerator.Emit(instruction.OpCode, (float)instruction.Operand);
                            break;
                        case OperandType.InlineString:
                            ilGenerator.Emit(instruction.OpCode, (string)instruction.Operand);
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
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

                            // 'Leave' instructions must be emitted if we are rewriting an async method.
                            // Otherwise the rewritten method will always start from the beginning every time.
                            if (opCode == OpCodes.Leave)
                            {
                                ilGenerator.Emit(opCode, targetLabel);
                                continue;
                            }
            
                            // Check if 'Leave' opcode is being used in an exception block,
                            // only emit it if that's not the case
                            if (opCode == OpCodes.Leave && _exceptionBlockLevel > 0) continue;

                            ilGenerator.Emit(opCode, targetLabel);
                            break;
                        case OperandType.InlineSwitch:
                            var switchInstructions = (Instruction[])instruction.Operand;
                            var targetLabels = new Label[switchInstructions.Length];
                            for (var i = 0; i < switchInstructions.Length; i++)
                                targetLabels[i] = targetInstructions[switchInstructions[i].Offset];
                            ilGenerator.Emit(instruction.OpCode, targetLabels);
                            break;
                        case OperandType.ShortInlineVar:
                        case OperandType.InlineVar:
                            var index = 0;
                            if (instruction.OpCode.Name.Contains("loc"))
                            {
                                index = ((LocalVariableInfo)instruction.Operand).LocalIndex;
                            }
                            else
                            {
                                index = ((ParameterInfo)instruction.Operand).Position;
                                index += 1;
                            }

                            if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                                ilGenerator.Emit(instruction.OpCode, (byte)index);
                            else
                                ilGenerator.Emit(instruction.OpCode, (ushort)index);
                            break;
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                        case OperandType.InlineField:
                        case OperandType.InlineMethod:
                            var memberInfo = (MemberInfo)instruction.Operand;
                            if (memberInfo.MemberType == MemberTypes.Field)
                            {
                                if (instruction.OpCode == OpCodes.Ldflda && ((FieldInfo)instruction.Operand).DeclaringType.Name == stateMachine.Name)
                                {
                                    var name = ((FieldInfo) instruction.Operand).Name;
                                    
                                    if (fieldDict.TryGetValue(name, out var field))
                                    {
                                        ilGenerator.Emit(OpCodes.Ldflda, field);
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception($"Cannot find field {name}");
                                    }
                                }
                                
                                if (instruction.OpCode == OpCodes.Stfld && ((FieldInfo) instruction.Operand).DeclaringType.Name == stateMachine.Name)
                                {
                                    var name = ((FieldInfo) instruction.Operand).Name;
                                    
                                    if (fieldDict.TryGetValue(name, out var field))
                                    {
                                        ilGenerator.Emit(OpCodes.Stfld, field);
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception($"Cannot find field {name}");
                                    }
                                }
                                
                                if (instruction.OpCode == OpCodes.Ldfld && ((FieldInfo) instruction.Operand).DeclaringType.Name == stateMachine.Name)
                                {
                                    var name = ((FieldInfo) instruction.Operand).Name;
                                    
                                    if (fieldDict.TryGetValue(name, out var field))
                                    {
                                        ilGenerator.Emit(OpCodes.Ldfld, field);
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception($"Cannot find field {name}");
                                    }
                                }
                                
                                ilGenerator.Emit(instruction.OpCode, memberInfo as FieldInfo);
                            }
                            else if (memberInfo.MemberType == MemberTypes.TypeInfo
                                     || memberInfo.MemberType == MemberTypes.NestedType)
                            {
                                if (instruction.OpCode == OpCodes.Constrained)
                                {
                                    _constrainedType = memberInfo as TypeInfo;
                                    continue;
                                }

                                ilGenerator.Emit(instruction.OpCode, memberInfo as TypeInfo);
                            }
                            else if (memberInfo.MemberType == MemberTypes.Constructor)
                            {
                                throw new NotSupportedException();
                                // var constructorInfo = memberInfo as ConstructorInfo;
                                //
                                // if (constructorInfo.InCoreLibrary())
                                // {
                                //     // Don't attempt to rewrite inaccessible constructors in System.Private.CoreLib/mscorlib
                                //     if (ShouldForward(constructorInfo)) goto forward;
                                // }
                                //
                                // if (instruction.OpCode == OpCodes.Call)
                                // {
                                //     ilGenerator.Emit(OpCodes.Ldtoken, (ConstructorInfo)memberInfo);
                                //     ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(constructorInfo));
                                //     return;
                                // }
                                //
                                // if (instruction.OpCode == OpCodes.Newobj)
                                // {
                                //     //ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                                //     ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForObjectInitialization(constructorInfo));
                                //     return;
                                // }
                                //
                                // if (instruction.OpCode == OpCodes.Ldftn)
                                // {
                                //     //ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                                //     ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(constructorInfo));
                                //     return;
                                // }
                                //
                                // // If we get here, then we haven't accounted for an opcode.
                                // // Throw exception to make this obvious.
                                // throw new NotSupportedException(instruction.OpCode.Name);
                                //
                                // forward:
                                // ilGenerator.Emit(instruction.OpCode, constructorInfo);
                            }
                            else if (memberInfo.MemberType == MemberTypes.Method)
                            {
                                var methodInfo = memberInfo as MethodInfo;
                                
                                if (methodInfo.InCoreLibrary())
                                {
                                    // Don't attempt to rewrite inaccessible methods in System.Private.CoreLib/mscorlib
                                    if (ShouldForward(methodInfo)) goto forward;
                                }

                                if (instruction.OpCode == OpCodes.Call)
                                {
                                    if (methodInfo.IsGenericMethod
                                        && methodInfo.DeclaringType.IsGenericType
                                        && methodInfo.DeclaringType.GetGenericTypeDefinition() == typeof(AsyncTaskMethodBuilder<>)
                                            && methodInfo.Name == "AwaitUnsafeOnCompleted")
                                    {
                                        // The call is to AwaitUnsafeOnCompleted which must have the correct generic arguments
                                        var taskAwaiterArgument = methodInfo.GetGenericArguments()[0];
                                        methodInfo = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(taskAwaiterArgument, tb);
                                    }
                                    
                                    ilGenerator.Emit(OpCodes.Call, methodInfo);
                                    // ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(methodInfo));
                                    continue;
                                }

                                if (instruction.OpCode == OpCodes.Callvirt)
                                {
                                    if (_constrainedType != null)
                                    {
                                        ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualCall(methodInfo, _constrainedType));
                                        _constrainedType = null;
                                        continue;
                                    }

                                    ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
                                    continue;
                                }

                                if (instruction.OpCode == OpCodes.Ldftn)
                                {
                                    ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(methodInfo));
                                    continue;
                                }

                                if (instruction.OpCode == OpCodes.Ldvirtftn)
                                {
                                    ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualLoad(methodInfo));
                                    continue;
                                }

                                forward:
                                ilGenerator.Emit(instruction.OpCode, methodInfo);
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                            break;
                        default:
                            throw new NotSupportedException(instruction.OpCode.OperandType.ToString());
                    }
                }

      
                ilGenerator.Emit(OpCodes.Ret);

                Console.WriteLine();
                Console.WriteLine();
            });
        
        return tb.CreateType();
    }
    
    private static bool ShouldForward(MethodBase member)
    {
        var declaringType = member.DeclaringType ?? throw new Exception($"Type {member.Name} does not have a {nameof(MethodBase.DeclaringType)}");

        if (declaringType.Namespace == typeof(AsyncTaskMethodBuilder).Namespace)
        {
            if (declaringType.Name == "AsyncMethodBuilderCore") return false;
            if (declaringType.Name == typeof(AsyncTaskMethodBuilder<>).Name) return false;
        }

        // Don't attempt to rewrite inaccessible constructors in System.Private.CoreLib/mscorlib
        if (!declaringType.IsPublic) return true;
        if (!member.IsPublic && !member.IsFamily && !member.IsFamilyOrAssembly) return true;

        return false;
    }
    
    private static void EmitILForExceptionHandlers(ref int _exceptionBlockLevel, ILGenerator ilGenerator, Instruction instruction, IReadOnlyCollection<ExceptionHandler> handlers)
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
  }
}