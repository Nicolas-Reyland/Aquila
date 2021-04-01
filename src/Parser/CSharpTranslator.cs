using System.Collections.Generic;
using System.Linq;

namespace Parser
{
    public class CSharpTranslator : Translator
    {
        public CSharpTranslator(List<Instruction> instr_list) : base(instr_list)
        {
        }

        public override string[] translate(IEnumerable<Instruction> input_lines)
        {
            return instr2Lines(input_lines);
        }

        protected override string declarationTr(Declaration declaration)
        {
            dynamic[] info = declaration.getTranslatorInfo();
            string
                var_name = info[0],
                var_type = info[1],
                var_expr = ((Expression) info[2]).expr;
            var_expr = normalizeExpression(var_expr);
            return var_type + " " + var_name + " = " + var_expr;
        }

        protected override string assignmentTr(Assignment assignment)
        {
            dynamic[] info = assignment.getTranslatorInfo();
            string
                var_name = info[0],
                var_expr = ((Expression) info[1]).expr;
            var_expr = normalizeExpression(var_expr);
            return var_name + " = " + var_expr;
        }

        protected override string voidFunctionCallTr(VoidFunctionCall void_function_call)
        {
            dynamic[] info = void_function_call.getTranslatorInfo();
            string function_name = info[0];
            object[] args = info[1];
            List<string> expr_args = args.Select(t => ((Expression) t).expr).ToList();
            expr_args = expr_args.Select(normalizeExpression).ToList();
            
            return function_name + "(" + StringUtils.reuniteBySymbol(expr_args, ", ") + ")";
        }

        protected override string[] whileTr(WhileLoop loop)
        {
            dynamic[] info = loop.getTranslatorInfo();
            string condition = normalizeExpression(((Expression) info[0]).expr);
            string[] instructions = instr2Lines((List<Instruction>) info[1]);
            var loop_str_list = new List<string> {"while (" + condition + ") {"};
            loop_str_list.AddRange(instructions);
            loop_str_list.Add("}");
            
            return loop_str_list.ToArray();
        }

        protected override string[] forTr(ForLoop loop)
        {
            dynamic[] info = loop.getTranslatorInfo();
            string
                start = instr2Lines(new List<Instruction>{(Instruction) info[0]})[0],
                stop = normalizeExpression(info[1]),
                step = instr2Lines(new List<Instruction>{(Instruction) info[2]})[0];
            string[] instructions = instr2Lines((List<Instruction>) info[3]);
            var loop_str_list = new List<string> {"for (" + start + "; " + step + "; " + stop + ") {"};
            loop_str_list.AddRange(instructions);
            loop_str_list.Add("}");
            
            return loop_str_list.ToArray();
        }

        protected override string[] ifTr(IfCondition if_condition)
        {
            dynamic[] info = if_condition.getTranslatorInfo();
            string condition = ((Expression) info[0]).expr;
            string[] if_instructions = instr2Lines((List<Instruction>) info[1]);
            string[] else_instructions = instr2Lines((List<Instruction>) info[2]);
            var if_instr_list = new List<string> {"if (" + condition + ") {"};
            if_instr_list.AddRange(if_instructions);
            if_instr_list.Add("}");
            if (else_instructions.Length > 0)
            {
                if_instr_list.Add("} else {");
                if_instr_list.AddRange(else_instructions);
                if_instr_list.Add("}");
            }

            return if_instr_list.ToArray();
        }

        protected override string[] functionDefTr(FunctionDef function)
        {
            dynamic[] info = function.getTranslatorInfo();
            string name = (string) info[0];
            string type = (string) info[1];
            List<string> args = (List<string>) info[2];
            string[] instructions = instr2Lines((List<Instruction>) info[3]);
            // bool recursive = (bool) info[4];
            List<string> func_def_list = new List<string> {
                "public " + 
                type + " " +
                name + "(" +
                StringUtils.reuniteBySymbol(args, ", ")
                 + ") {"
            };
            func_def_list.AddRange(instructions);
            func_def_list.Add("}");

            return func_def_list.ToArray();
        }
    }
}