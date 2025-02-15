﻿using System.Collections.Generic;
using System.Text;
using Cpp2IL.Core.Analysis.ResultModels;
using LibCpp2IL;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Instruction = Iced.Intel.Instruction;

namespace Cpp2IL.Core.Analysis.Actions
{
    public abstract class BaseAction
    {
        private StringBuilder _lineComments = new StringBuilder();
        public Instruction AssociatedInstruction;
        public int IndentLevel;

        private List<LocalDefinition> UsedLocals = new List<LocalDefinition>();
        private List<LocalDefinition> RegisteredLocalsWithoutSideEffects = new List<LocalDefinition>();

        protected bool is32Bit => LibCpp2IlMain.Binary!.is32Bit;
        
        public BaseAction(MethodAnalysis context, Instruction instruction)
        {
            AssociatedInstruction = instruction;
            IndentLevel = context.IndentLevel;
        }

        public abstract Mono.Cecil.Cil.Instruction[] ToILInstructions(MethodAnalysis context, ILProcessor processor);

        public abstract string? ToPsuedoCode();

        public abstract string ToTextSummary();

        public List<LocalDefinition> GetUsedLocals()
        {
            return UsedLocals;
        }

        protected void RegisterUsedLocal(LocalDefinition l)
        {
            UsedLocals.Add(l);
        }
        
        public List<LocalDefinition> GetRegisteredLocalsWithoutSideEffects()
        {
            return RegisteredLocalsWithoutSideEffects;
        }

        protected void RegisterDefinedLocalWithoutSideEffects(LocalDefinition l)
        {
            RegisteredLocalsWithoutSideEffects.Add(l);
        }

        public virtual bool IsImportant()
        {
            return false;
        }

        public string GetSynopsisEntry()
        {
            var comment = GetLineComment();

            if (string.IsNullOrWhiteSpace(comment))
                return ToTextSummary();

            var summary = ToTextSummary();
            
            var newlineCount = 0;
            if (summary.EndsWith("\n"))
            {
                var oldLen = summary.Length;
                summary = summary.TrimEnd('\n');
                newlineCount = oldLen - summary.Length;
            }

            return $"{summary} ; {GetLineComment()}{"\n".Repeat(newlineCount)}";
        }

        protected void AddComment(string comment)
        {
            _lineComments.Append(" - ").Append(comment);
        }

        public string GetLineComment()
        {
            return _lineComments.ToString();
        }

        public virtual bool PseudocodeNeedsLinebreakBefore()
        {
            return false;
        }
    }
}