using System;
using System.Collections.Generic;

namespace Parser
{
    public class PythonTranslator : Translator
    {
        //
        public PythonTranslator(List<Instruction> instr_list) : base(instr_list)
        {
        }

        protected override string declarationTr(Declaration declaration)
        {
            throw new NotImplementedException();
        }

        protected override string assignmentTr(Assignment assignment)
        {
            throw new NotImplementedException();
        }

        protected override string voidFunctionCallTr(VoidFunctionCall void_function_call)
        {
            throw new NotImplementedException();
        }

        protected override string[] whileTr(WhileLoop loop)
        {
            throw new NotImplementedException();
        }

        protected override string[] forTr(ForLoop loop)
        {
            throw new NotImplementedException();
        }

        protected override string[] ifTr(IfCondition if_condition)
        {
            throw new NotImplementedException();
        }

        protected override string[] functionDefTr(FunctionDef function)
        {
            throw new NotImplementedException();
        }
    }
}