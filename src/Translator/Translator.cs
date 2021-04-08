using System;
using System.Collections.Generic;
using Parser;

namespace Translator
{
    public abstract class Translator
    {
        protected readonly IEnumerable<Instruction> instr_list;

        protected Translator(IEnumerable<Instruction> instr_list)
        {
            this.instr_list = instr_list;
        }

        public static IEnumerable<Instruction> instructionsFromFile(string path)
        {
            Algorithm algo = Interpreter.algorithmFromSrcCode(path, false, false, "c-sharp-translated");
            return algo.getInstructions();
        }

        public abstract string[] translate();

        protected string[] instr2Lines(IEnumerable<Instruction> instructions)
        {
            List<string> lines = new List<string>();
            foreach (Instruction instr in instructions)
            {
                if (instr is Declaration declaration)
                {
                    lines.Add(declarationTr(declaration));
                }
                else if (instr is Assignment assignment)
                {
                    lines.Add(assignmentTr(assignment));
                }
                else if (instr is VoidFunctionCall void_function_call)
                {
                    lines.Add(voidFunctionCallTr(void_function_call));
                }
                else if (instr is WhileLoop while_loop)
                {
                    lines.AddRange(whileTr(while_loop));
                }
                else if (instr is ForLoop for_loop)
                {
                    lines.AddRange(forTr(for_loop));
                }
                else if (instr is IfCondition if_condition)
                {
                    lines.AddRange(ifTr(if_condition));
                }
                else if (instr is FunctionDef function_def)
                {
                    lines.AddRange(functionDefTr(function_def));
                }
                else if (instr is Tracing _)
                {
                }
                else
                {
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

        protected static string normalizeExpression(string expr_string)
        {
            // purge first
            expr_string = StringUtils.normalizeWhiteSpaces(expr_string);
            
            printTranslator("input line: " + expr_string);
            
            // matching parentheses & brackets
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '(', ')'));
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '[', ']'));
            expr_string = StringUtils.removeRedundantMatchingDelimiters(expr_string, '(', ')');
            
            printTranslator("dynamic list ?");
            return expr_string;
        }

        public static void printTranslator(params object[] args)
        {
            if (!Global.getSetting("translator debug")) return;
            
            // default settings
            int max_call_name_length = 30;
            int num_new_method_separators = 25;
            bool enable_function_depth = true;
            int num_function_depth_chars = 4;
            string prefix = "+ TRANSLATE";
            
            // print the args nicely
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            StringUtils.nicePrintFunction(max_call_name_length, num_new_method_separators, enable_function_depth,
                num_function_depth_chars, prefix, args);
        }
    }
}
