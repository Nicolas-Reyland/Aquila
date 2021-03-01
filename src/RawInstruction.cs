using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// <see cref="RawInstruction"/>s are used to build a sketch of the given pseudo-code Algorithm.
    /// They only store the instructions as strings. One <see cref="RawInstruction"/> by base-line.
    /// A base-line is a line that is at the root of the algorithm. Lines that come
    /// after an if statement or after a for loop are not base-lines.
    /// They are stored in the corresponding <see cref="RawInstruction"/>'s <see cref="_sub_instr_list"/>, and if <see cref="_is_nested"/> is true.
    /// The <see cref="RawInstruction"/>s are the first approach to parsing a pseudo-code program
    /// into an executable <see cref="Algorithm"/> object.
    /// <para/>List of the attributes:
    /// <para/>* <see cref="_instr"/> : string
    /// <para/>* <see cref="_is_nested"/> : bool
    /// <para/>* <see cref="_sub_instr_list"/> : List(RawInstruction)
    /// </summary>
    public class RawInstruction
    {
        /// <summary>
        /// The line of pseudo-code represented by the <see cref="RawInstruction"/>
        /// </summary>
        private readonly string _instr;
        
        /// <summary>
        /// Nested <see cref="RawInstruction"/>s are for-loops, if-statements, etc.
        /// </summary>
        private bool _is_nested;
        
        /// <summary>
        /// If nested (<seealso cref="_is_nested"/>), holds the nested instructions that follow the instruction.
        /// </summary>
        private List<RawInstruction> _sub_instr_list;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instr"> instruction</param>
        public RawInstruction(string instr)
        {
            this._instr = instr;
        }

        /// <summary>
        /// Print the instruction and its <see cref="_sub_instr_list"/> if there are any.
        /// Indents the code according to the nested-depth to increase readability
        /// </summary>
        /// <param name="depth"> indentation level</param>
        public void prettyPrint(uint depth = 0)
        {
            for (uint i = 0; i < depth; i++) { Console.Write("\t"); }
            Console.WriteLine(this._instr);
            
            if (!_is_nested) return;
            foreach (RawInstruction sub_instr in _sub_instr_list)
            {
                sub_instr.prettyPrint(depth + 1);
            }
        }
        
        /// <summary>
        /// Transform a string list into a <see cref="RawInstruction"/>. Takes into account
        /// nested instructions. This method creates the base algorithm structure, using
        /// the <see cref="_is_nested"/> and <see cref="_sub_instr_list"/> attributes.
        /// </summary>
        /// <param name="lines"> list of strings</param>
        /// <returns> list of RawInstructions</returns>
        public static List<RawInstruction> code2RawInstructions(List<string> lines)
        {
            int index = 0;
            List<RawInstruction> instructions = new List<RawInstruction> ();

            while (index < lines.Count) // using while loop bc index will be modified
            {
                string line = lines.ElementAt(index);
                Debugging.print("doing line ", line);
                if (line.StartsWith("#")) // macro preprocessor line
                {
                    index++;
                    continue;
                }

                RawInstruction instr = new RawInstruction (line);

                foreach (KeyValuePair<string, string> val in Global.nested_instruction_flags)
                {
                    if (line.StartsWith(val.Key + " "))
                    {
                        Debugging.print("FOUND " + val.Key);
                        instr._is_nested = true;
                        int end_index =
                            StringUtils.findCorrespondingElementIndex(lines, index + 1, val.Key, val.Value);
                        List<string> sub_lines = lines.GetRange(index + 1, end_index - index - 1);

                        instr._sub_instr_list = code2RawInstructions(sub_lines);
                        Debugging.print(index, " - ", end_index);
                        index = end_index;
                    }
                }
                
                instructions.Add(instr);
                index++;
            }

            return instructions;
        }

        /// <summary>
        /// Calls <see cref="RawInstruction.rawInstr2Instr"/> on itself
        /// </summary>
        /// <returns> corresponding <see cref="Instruction"/></returns>
        public Instruction toInstr()
        {
            return rawInstr2Instr(this);
        }
        
        /// <summary>
        /// Transforms a <see cref="RawInstruction"/> into an <see cref="Instruction"/>.
        /// <para/>The order of operations is:
        /// <para/>* variable declaration
        /// <para/>* variable modification
        /// <para/>* for loop
        /// <para/>* while loop
        /// <para/>* if statement
        /// <para/>* void function call
        /// </summary>
        /// <param name="raw_instr"> a <see cref="RawInstruction"/></param>
        /// <returns> teh corresponding <see cref="Instruction"/></returns>
        /// <exception cref="Global.aquilaError"></exception>
        private static Instruction rawInstr2Instr(RawInstruction raw_instr)
        {
            /* Order of operations:
             * variable declaration
             * variable assignment
             * for loop
             * while loop
             * if statement
             * void function call
             */
            
            Debugging.print("from raw instr to instr: \"", raw_instr._instr, "\"");

            // split instruction
            List<string> instr = StringUtils.splitStringKeepingStructureIntegrity(raw_instr._instr, ' ', Global.base_delimiters);

            Debugging.print("trace ?");
            // variable tracing
            if (instr[0] == "trace")
            {
                List<Expression> traced_vars = new List<Expression>();
                for (int i = 1; i < instr.Count; i++)
                {
                    traced_vars.Add(new Expression(instr[i]));
                }

                return new Tracing(traced_vars);
            }

            Debugging.print("declare ?");
            // variable declaration
            if (instr[0] == "declare")
            {
                // "declare type name" or "declare name value"
                if (instr.Count < 3 || instr.Count > 4)
                {
                    throw Global.aquilaError();
                }
                
                // all types
                string[] type_list = {"int", "float", "bool", "list"};

                bool type_declared = false;
                
                if (instr.Count == 4) // declare type name value
                {
                    if (instr[1] == "auto")
                    {
                        instr.RemoveAt(1);
                    }
                    else
                    {
                        type_declared = true;
                        Debugging.assert(type_list.Contains(instr[1])); // UnknownTypeError
                    }
                }
                else if (instr[1] == "auto")
                {
                    throw Global.aquilaError(); // cannot "declare auto var_name"
                }

                // if instr[1] is type name
                if (type_list.Contains(instr[1]))
                {
                    Expression default_value = Global.default_values_by_var_type[instr[1]];
                    if (type_declared)
                    {
                        return new Declaration(instr[2], new Expression(instr[3]), instr[1], true);
                    }
                    return new Declaration(instr[2], default_value, "auto", false);
                }

                // case is: "declare var_name value"
                string var_name = instr[1];
                string var_value = instr[2];

                return new Declaration(var_name, new Expression(var_value));
            }

            Debugging.print("assignment ?");
            // variable assignment
            if (instr.Count > 1 && instr[1] == "=" && (instr[0][0] == '$' || instr[0].Contains("(")))
            {
                Debugging.assert(instr.Count > 2); // syntax ?unfinished line?
                string var_designation = instr[0];
                instr.RemoveAt(0); // remove "$name"
                instr.RemoveAt(0); // remove "="
                // reunite all on the right side of the "=" sign
                string assignment_string = StringUtils.reuniteBySymbol(instr);
                // get the Expresion
                Expression assignment = new Expression(assignment_string);
                return new Assignment(var_designation, assignment);
            }
            
            Debugging.print("for loop ?");
            // for loop
            if (instr[0] == "for")
            {
                Debugging.assert(raw_instr._is_nested); // syntax???
                Debugging.assert(instr[1].StartsWith("(") && instr[1].EndsWith(")")); // syntax
                List<string> sub_instr =
                    StringUtils.splitStringKeepingStructureIntegrity(instr[1].Substring(1, instr[1].Length - 2), ',', Global.base_delimiters);
                sub_instr = StringUtils.purgeLines(sub_instr);
                Debugging.print(sub_instr);
                Debugging.assert(sub_instr.Count == 3); // syntax

                // start
                Instruction start = new RawInstruction(sub_instr[0]).toInstr();

                // stop
                Expression condition = new Expression(sub_instr[1]);
                
                // step
                Instruction step = new RawInstruction(sub_instr[2]).toInstr();

                // instr
                List<Instruction> loop_instructions = new List<Instruction>();
                foreach (RawInstruction loop_instr in raw_instr._sub_instr_list)
                {
                    loop_instructions.Add(rawInstr2Instr(loop_instr));
                }

                return new ForLoop(start, condition, step, loop_instructions);

            }

            Debugging.print("while loop ?");
            // while loop
            if (instr[0] == "while")
            {
                // syntax check
                Debugging.assert(instr.Count == 2); // syntax

                // condition expression
                Expression condition = new Expression(instr[1]);
                
                // instr
                List<Instruction> loop_instructions = new List<Instruction>();
                foreach (RawInstruction loop_instr in raw_instr._sub_instr_list)
                {
                    loop_instructions.Add(rawInstr2Instr(loop_instr));
                }

                return new WhileLoop(condition, loop_instructions);
            }
            
            Debugging.print("if statement ?");
            // if statement
            if (instr[0] == "if")
            {
                // syntax check
                Debugging.assert(instr.Count == 2); // syntax
                
                // condition expression
                Expression condition = new Expression(instr[1]);
                
                // instr
                List<Instruction> if_instructions = new List<Instruction>();
                List<Instruction> else_instructions = new List<Instruction>();
                bool if_section = true;
                foreach (RawInstruction loop_instr in raw_instr._sub_instr_list)
                {
                    if (if_section)
                    {
                        if (loop_instr._instr == "else")
                        {
                            if_section = false;
                            continue;
                        }
                        if_instructions.Add(rawInstr2Instr(loop_instr));
                    }
                    else
                    {
                        else_instructions.Add(rawInstr2Instr(loop_instr));
                    }
                }

                return new IfCondition(condition, if_instructions, else_instructions);
            }

            Debugging.print("function call ?");
            // void function call (no return value, or return value not used)
            if (instr[0].Contains('('))
            {
                // syntax checks
                Debugging.assert(instr.Count == 1); // syntax
                Debugging.assert(instr[0][instr[0].Length - 1] == ')'); // syntax
                
                // function name
                string function_name = instr[0].Split('(')[0]; // extract function name
                Functions.assertFunctionExists(function_name); // assert function exists
                Debugging.print("expr_string for function call ", instr[0]);
                // extract args
                string exprs = instr[0].Substring(function_name.Length + 1); // + 1 : '('
                exprs = exprs.Substring(0, exprs.Length - 1); // ')'
                List<string> arg_expr_str = StringUtils.splitStringKeepingStructureIntegrity(exprs, ',', Global.base_delimiters);

                // no args ?
                if (arg_expr_str.Count == 1 && StringUtils.purgeLine(arg_expr_str[0]) == "")
                {
                    return new VoidFunctionCall(function_name);
                }
                
                List<Expression> arg_exprs = arg_expr_str.Select(x => new Expression(x)).ToList();
                object[] args = arg_exprs.Select(x => (object) x).ToArray();

                return new VoidFunctionCall(function_name, args);
            }

            Debugging.print("!unrecognized line: \"", raw_instr._instr, "\"");
            throw Global.aquilaError();
        }
    }
}