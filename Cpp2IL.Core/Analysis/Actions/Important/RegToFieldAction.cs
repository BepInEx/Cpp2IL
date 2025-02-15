﻿using System.Collections.Generic;
using System.Diagnostics;
using Cpp2IL.Core.Analysis.ResultModels;
using LibCpp2IL;
using Mono.Cecil.Cil;
using Instruction = Iced.Intel.Instruction;

namespace Cpp2IL.Core.Analysis.Actions.Important
{
    public class RegToFieldAction : AbstractFieldWriteAction
    {
        public readonly IAnalysedOperand? ValueRead;

        //TODO: Fix string literal to field - it's a constant in a field.
        public RegToFieldAction(MethodAnalysis context, Instruction instruction) : base(context, instruction)
        {
            var destRegName = Utils.GetRegisterNameNew(instruction.MemoryBase);
            var destFieldOffset = instruction.MemoryDisplacement32;
            ValueRead = context.GetOperandInRegister(Utils.GetRegisterNameNew(instruction.Op1Register));

            InstanceBeingSetOn = context.GetLocalInReg(destRegName);
            
            if(ValueRead is LocalDefinition loc)
                RegisterUsedLocal(loc);

            if (ValueRead is ConstantDefinition { Value: StackPointer s })
            {
                var offset = s.offset;
                if (context.StackStoredLocals.TryGetValue((int)offset, out var tempLocal))
                    ValueRead = tempLocal;
                else
                    ValueRead = context.EmptyRegConstant;
            }

            if (InstanceBeingSetOn?.Type?.Resolve() == null)
            {
                if (context.GetConstantInReg(destRegName) is {Value: FieldPointer p})
                {
                    InstanceBeingSetOn = p.OnWhat;
                    RegisterUsedLocal(InstanceBeingSetOn);
                    FieldWritten = p.Field;
                }
                
                return;
            }

            RegisterUsedLocal(InstanceBeingSetOn);

            FieldWritten = FieldUtils.GetFieldBeingAccessed(InstanceBeingSetOn.Type, destFieldOffset, false);
        }

        internal RegToFieldAction(MethodAnalysis context, Instruction instruction, FieldUtils.FieldBeingAccessedData fieldWritten, LocalDefinition instanceWrittenOn, LocalDefinition readFrom) : base(context, instruction)
        {
            Debug.Assert(instanceWrittenOn.Type!.IsValueType);
            
            FieldWritten = fieldWritten;
            InstanceBeingSetOn = instanceWrittenOn;
            ValueRead = readFrom;
            
            RegisterUsedLocal(InstanceBeingSetOn);
            RegisterUsedLocal(readFrom);
        }

        public override Mono.Cecil.Cil.Instruction[] ToILInstructions(MethodAnalysis context, ILProcessor processor)
        {
            if (ValueRead == null || InstanceBeingSetOn == null || FieldWritten == null)
                throw new TaintedInstructionException();
            
            var ret = new List<Mono.Cecil.Cil.Instruction>();

            ret.AddRange(InstanceBeingSetOn.GetILToLoad(context, processor));

            var f = FieldWritten;
            while (f.NextChainLink != null)
            {
                ret.Add(processor.Create(OpCodes.Ldfld, processor.ImportReference(f.ImpliedFieldLoad!)));
                f = f.NextChainLink;
            }
            
            ret.AddRange(ValueRead.GetILToLoad(context, processor));

            if (f.FinalLoadInChain == null)
                throw new TaintedInstructionException("Final load in chain is null");
            
            ret.Add(processor.Create(OpCodes.Stfld, processor.ImportReference(f.FinalLoadInChain)));
            
            
            return ret.ToArray();
        }

        public override string ToPsuedoCode()
        {
            return $"{InstanceBeingSetOn?.Name}.{FieldWritten} = {ValueRead?.GetPseudocodeRepresentation()}";
        }

        public override string ToTextSummary()
        {
            return $"[!] Sets the field {FieldWritten} (Type {FieldWritten?.GetFinalType()}) on local {InstanceBeingSetOn} to the value stored in {ValueRead}";
        }

        public override bool IsImportant()
        {
            return true;
        }
    }
}