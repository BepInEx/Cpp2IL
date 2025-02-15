﻿using System;
using Cpp2IL.Core.Analysis.ResultModels;
using LibCpp2IL;
using Mono.Cecil.Cil;
using Instruction = Iced.Intel.Instruction;

namespace Cpp2IL.Core.Analysis.Actions.Important
{
    public class DivideRegByGlobalAction : BaseAction
    {
        private LocalDefinition? _op1;
        private string? _regName;
        private float _globalValue;
        private LocalDefinition? _localMade;
        private ulong _globalAddr;

        public DivideRegByGlobalAction(MethodAnalysis context, Instruction instruction) : base(context, instruction)
        {
            _globalAddr = instruction.MemoryDisplacement64;
            
            _regName = Utils.GetRegisterNameNew(instruction.Op0Register);
            _op1 = context.GetLocalInReg(_regName);

            _globalValue = BitConverter.ToSingle(LibCpp2IlMain.Binary!.GetRawBinaryContent(), (int) LibCpp2IlMain.Binary!.MapVirtualAddressToRaw(_globalAddr));

            _localMade = context.MakeLocal(Utils.SingleReference, reg: _regName);
        }

        public override Mono.Cecil.Cil.Instruction[] ToILInstructions(MethodAnalysis context, ILProcessor processor)
        {
            throw new System.NotImplementedException();
        }

        public override string? ToPsuedoCode()
        {
            return $"{_localMade?.Type} {_localMade?.Name} = {_op1?.GetPseudocodeRepresentation()} / {_globalValue}";
        }

        public override string ToTextSummary()
        {
            return $"Divides {_op1} by the constant value at 0x{_globalAddr:X} in the binary, which is {_globalValue}, and stores the result in new local {_localMade} in register {_regName}";
        }

        public override bool IsImportant()
        {
            return true;
        }
    }
}