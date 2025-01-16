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
using Pose.IL.DebugHelpers;

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

    public static async Task DoWork3Async()
    {
        Console.WriteLine("Here 3.1");
        await Task.Delay(10);
        Console.WriteLine("Here 3.2");
    }
    
    public static async Task<int> DoWork1Async()
    {
      return GetInt();
    }

    private static Type GetStateMachineType(MethodBase method)
    {
        var stateMachineType = method
            ?.GetCustomAttribute<AsyncStateMachineAttribute>()
            ?.StateMachineType;

        return stateMachineType;
    }

    private static (MethodInfo StartMethod, MethodInfo CreateMethod, PropertyInfo TaskProperty, MethodInfo OriginalMethod) GetMethods(MethodInfo method)
    {
        var originalMethod = method;
        var originalMethodReturnType =
            originalMethod.ReturnType.IsGenericType
                ? originalMethod.ReturnType.GetGenericArguments()[0]
                : typeof(void);
        
        const string startMethodName = nameof(AsyncTaskMethodBuilder<int>.Start);
        var startMethod = (originalMethodReturnType == typeof(void)
            ? typeof(AsyncTaskMethodBuilder).GetMethod(startMethodName)
            : typeof(AsyncTaskMethodBuilder<>).MakeGenericType(originalMethodReturnType).GetMethod(startMethodName)) ?? throw new Exception($"Cannot get {startMethodName} method");
        
        const string taskPropertyName = nameof(AsyncTaskMethodBuilder<int>.Task);
        var taskProperty = (originalMethodReturnType == typeof(void)
            ? typeof(AsyncTaskMethodBuilder).GetProperty(taskPropertyName)
            : typeof(AsyncTaskMethodBuilder<>).MakeGenericType(originalMethodReturnType).GetProperty(taskPropertyName)) ?? throw new Exception($"Cannot get {taskPropertyName} property");
        
        const string createMethodName = nameof(AsyncTaskMethodBuilder<int>.Create);
        var createMethod = (originalMethodReturnType == typeof(void)
            ? typeof(AsyncTaskMethodBuilder).GetMethod(createMethodName)
            : typeof(AsyncTaskMethodBuilder<>).MakeGenericType(originalMethodReturnType).GetMethod(createMethodName)) ?? throw new Exception($"Cannot get {createMethodName} method");

        return (startMethod, createMethod, taskProperty, originalMethod);
    }
    
    private static void RunAsync<TReturnType>(Type owningType, MethodInfo method) where TReturnType : class
    {
        var (startMethod, createMethod, taskProperty, _) = GetMethods(method);
        
        var stateMachineType = GetStateMachineType(method);
        var rewrittenStateMachine = RewriteMoveNext(stateMachineType);
        var stateMachineInstance = Activator.CreateInstance(rewrittenStateMachine);
        
        var builderField = rewrittenStateMachine.GetField("<>t__builder") ?? throw new Exception("Cannot get builder field");
        builderField.SetValue(stateMachineInstance, createMethod.Invoke(null, Array.Empty<object>()));
        
        var stateField = rewrittenStateMachine.GetField("<>1__state") ?? throw new Exception("Cannot get state field");
        stateField.SetValue(stateMachineInstance, -1);
        
        var genericMethod = startMethod.MakeGenericMethod(rewrittenStateMachine);
        var builder = builderField.GetValue(stateMachineInstance);
        
        genericMethod.Invoke(builder, new object[] { stateMachineInstance });

        var task = taskProperty.GetValue(builder) as TReturnType ?? throw new Exception("Cannot get task");
    }
    
    private static MethodBase RewriteAsync(Type owningType, MethodInfo method)
    {
        var (startMethod, createMethod, taskProperty, originalMethod) = GetMethods(method);

        var stateMachine = GetStateMachineType(method);
        var typeWithRewrittenMoveNext = RewriteMoveNext(stateMachine);

        var moveNextMethodInfo = typeWithRewrittenMoveNext.GetMethod(nameof(IAsyncStateMachine.MoveNext));

        if (moveNextMethodInfo != null)
        {
            var rewrittenOriginalMethod = new DynamicMethod(
                name: StubHelper.CreateStubNameFromMethod("impl", originalMethod),
                returnType: originalMethod.ReturnType,
                parameterTypes: originalMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
                m: originalMethod.Module,
                skipVisibility: true
            );

            var methodBody = originalMethod.GetMethodBody() ?? throw new MethodRewriteException($"Method {moveNextMethodInfo.Name} does not have a body");
            var locals = methodBody.LocalVariables;
            
            var ilGenerator = rewrittenOriginalMethod.GetILGenerator();

            foreach (var local in locals)
            {
                if (locals[0].LocalType == stateMachine)
                {
                    // References to the original state machine must be re-targeted to the rewritten state machine
                    ilGenerator.DeclareLocal(typeWithRewrittenMoveNext, local.IsPinned);
                }
                else
                {
                    ilGenerator.DeclareLocal(local.LocalType, local.IsPinned);
                }
            }
            
            var constructorInfo = typeWithRewrittenMoveNext.GetConstructors()[0];
            ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            
            ilGenerator.Emit(OpCodes.Call, createMethod);
            
            var builderField = typeWithRewrittenMoveNext.GetField("<>t__builder") ?? throw new Exception("Cannot get builder field");
            ilGenerator.Emit(OpCodes.Stfld, builderField);
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            var stateField = typeWithRewrittenMoveNext.GetField("<>1__state") ?? throw new Exception("Cannot get state field");
            ilGenerator.Emit(OpCodes.Stfld, stateField);
            
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldflda, builderField);
            ilGenerator.Emit(OpCodes.Ldloca_S, 0);
            
            var genericMethod = startMethod.MakeGenericMethod(typeWithRewrittenMoveNext);
            ilGenerator.Emit(OpCodes.Call, genericMethod);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldflda, builderField);
            
            ilGenerator.Emit(OpCodes.Call, taskProperty.GetMethod);
            
            ilGenerator.Emit(OpCodes.Ret);
            
#if TRACE
            var ilBytes = ilGenerator.GetILBytes();
            var browsableDynamicMethod = new BrowsableDynamicMethod(rewrittenOriginalMethod, new DynamicMethodBody(ilBytes, locals));
            Console.WriteLine("\n" + rewrittenOriginalMethod);

            foreach (var instruction in browsableDynamicMethod.GetInstructions())
            {
                Console.WriteLine(instruction);
            }
#endif
            
            return rewrittenOriginalMethod;
        }

        throw new Exception("Failed to rewrite async method");
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
          var asyncMethod = typeof(Program).GetMethod(nameof(DoWork2Async));
          var methodRewriter = MethodRewriter.CreateRewriter(asyncMethod, false);
          var methodBase = (MethodInfo)methodRewriter.RewriteAsync();
          var @delegate = methodBase.CreateDelegate(typeof(Func<Task<int>>));
          var result = @delegate.DynamicInvoke(new object[0]) as Task<int>;
          
          // RunAsync<Task<int>>(typeof(Program), typeof(Program).GetMethod(nameof(DoWork2Async)));
          // Console.WriteLine("---");
          // RunAsync<Task>(typeof(Program), typeof(Program).GetMethod(nameof(DoWork3Async)));
          // Console.WriteLine("---");
          // var task = (MethodInfo) RewriteAsync(typeof(Program), typeof(Program).GetMethod(nameof(DoWork2Async)));
          // var @delegate = task.CreateDelegate(typeof(Func<Task<int>>));
          // var result = @delegate.DynamicInvoke(new object[0]) as Task<int>;
          // Console.WriteLine("---");
          // @delegate.DynamicInvoke(new object[0]);
          // var result = task.Invoke(null, new object[] { });
          Console.WriteLine(result.Result);
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
    
    public static Type RewriteMoveNext(Type stateMachine)
    {
        var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsyncAssembly"), AssemblyBuilderAccess.RunAndCollect);
        var mb = ab.DefineDynamicModule("AsyncModule");
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
                // Console.WriteLine(m.Name);
                var _exceptionBlockLevel = 0;
                TypeInfo _constrainedType = null;
                
                var parameters = m.GetParameters().Select(p => p.ParameterType).ToArray();
                var meth = tb.DefineMethod(m.Name, MethodAttributes.Public | MethodAttributes.Virtual, m.ReturnType, parameters);

                var methodBody = m.GetMethodBody() ?? throw new MethodRewriteException($"Method {m.Name} does not have a body");
                var locals = methodBody.LocalVariables;
                var targetInstructions = new Dictionary<int, Label>();
                var handlers = new List<ExceptionHandler>();
                
                var ilGenerator = meth.GetILGenerator();
                var instructions = m.GetInstructions();

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
                                    if (methodInfo.DeclaringType.Name == nameof(AsyncTaskMethodBuilder) && methodInfo.Name == nameof(AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted))
                                    {
                                        // The call is to AwaitUnsafeOnCompleted which must have the correct generic arguments
                                        var taskAwaiterArgument = methodInfo.GetGenericArguments()[0];
                                        methodInfo = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(taskAwaiterArgument, tb);
                                    }
                                    else if (methodInfo.IsGenericMethod
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