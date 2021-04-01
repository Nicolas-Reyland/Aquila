using System;
using System.Collections.Generic;

namespace Parser
{
    public abstract class Translator
    {
        private readonly List<Instruction> _instr_list;

        protected Translator(List<Instruction> instr_list)
        {
            _instr_list = instr_list;
        }

        public string[] fromFile(string path)
        {
            Algorithm algo = Interpreter.algorithmFromSrcCode(path, false, false, "c-sharp-translated");
            return translate(algo.getInstructions());
        }

        public abstract string[] translate(IEnumerable<Instruction> input_lines);

        protected string[] instr2Lines(IEnumerable<Instruction> instr_list)
        {
            List<string> lines = new List<string>();
            foreach (Instruction instr in instr_list)
            {
                switch (instr)
                {
                    case Declaration declaration:
                        lines.Add(declarationTr(declaration));
                        break;
                    case Assignment assignment:
                        lines.Add(assignmentTr(assignment));
                        break;
                    case VoidFunctionCall void_function_call:
                        lines.Add(voidFunctionCallTr(void_function_call));
                        break;
                    case WhileLoop while_loop:
                        lines.AddRange(whileTr(while_loop));
                        break;
                    case ForLoop for_loop:
                        lines.AddRange(forTr(for_loop));
                        break;
                    case IfCondition if_condition:
                        lines.AddRange(ifTr(if_condition));
                        break;
                    case FunctionDef function_def:
                        lines.AddRange(functionDefTr(function_def));
                        break;
                    case Tracing:
                        break;
                    default:
                        throw new NotImplementedException("Instruction: " + instr + " is not supported.");
                }
            }

            return lines.ToArray();
        }

        protected abstract string declarationTr(Declaration declaration);

        protected abstract string assignmentTr(Assignment assignment);

        protected abstract string voidFunctionCallTr(VoidFunctionCall void_function_call);

        protected abstract string[] whileTr(WhileLoop loop);

        protected abstract string[] forTr(ForLoop loop);

        protected abstract string[] ifTr(IfCondition if_condition);

        protected abstract string[] functionDefTr(FunctionDef function);

        protected static string normalizeExpression(string expr)
        {
            return expr;
        }
    }
}