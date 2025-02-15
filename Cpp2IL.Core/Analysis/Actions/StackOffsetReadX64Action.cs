﻿using Cpp2IL.Core.Analysis.ResultModels;
using Mono.Cecil.Cil;
using Instruction = Iced.Intel.Instruction;

namespace Cpp2IL.Core.Analysis.Actions
{
    public class StackOffsetReadX64Action : BaseAction
    {
        private readonly string _destReg;
        private readonly uint _stackOffset;
        private ConstantDefinition? _constantMade;
        private LocalDefinition localResolved;

        public StackOffsetReadX64Action(MethodAnalysis context, Instruction instruction) : base(context, instruction)
        {
            _destReg = Utils.GetRegisterNameNew(instruction.Op0Register);
            _stackOffset = instruction.MemoryDisplacement32;

            if (context.StackStoredLocals.TryGetValue((int) _stackOffset, out localResolved))
            {
                context.SetRegContent(_destReg, localResolved);
            }
            else
            {
                _constantMade = context.MakeConstant(typeof(StackPointer), new StackPointer(_stackOffset), reg: _destReg);
            }
        }

        public override Mono.Cecil.Cil.Instruction[] ToILInstructions(MethodAnalysis context, ILProcessor processor)
        {
            throw new System.NotImplementedException();
        }

        public override string? ToPsuedoCode()
        {
            throw new System.NotImplementedException();
        }

        public override string ToTextSummary()
        {
            if (localResolved != null)
                return $"Reads local {localResolved} from stack offset {_stackOffset} (0x{_stackOffset:X}) into register {_destReg}";

            return $"Reads unknown value in stack, offset {_stackOffset} (0x{_stackOffset:X}) and stores the pointer in register {_destReg} as new constant {_constantMade!.Name}";
        }
    }
}