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
        private RawInstruction(string instr)
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
             * variable modification
             * for loop
             * while loop
             * if statement
             * void function call
             */
            
            Debugging.print("from raw instr to instr: \"", raw_instr._instr, "\"");
            
            // split instruction
            List<string> instr = StringUtils.splitStringKeepingStructureIntegrity(raw_instr._instr, ' ', Global.base_delimiters);
            
            // variable declaration
            if (instr[0].StartsWith("declare"))
            {
                if (instr.Count < 3 || instr.Count > 4)
                {
                    throw Global.aquilaError();
                }
                
                string var_type = instr[1];
                string var_name = instr[2];
                // get variable value (custom or default)
                Variable variable = instr.Count == 4
                    ? Parser.value2Variable(var_type, instr[3])
                    : (Variable) Global.default_values_by_var_type[var_type];

                return new Declaration(var_name, variable);
            }
            
            // variable modification
            if (instr[0] == "var" && instr[2] == "=")
            {
                Debugging.assert(instr.Count > 3);
                string var_name = "var " + instr[1];
                instr.RemoveAt(0); // remove "var"
                instr.RemoveAt(0); // remove "name"
                instr.RemoveAt(0); // remove "="
                // reunite the right side of the "=" sign
                string assignment_string = StringUtils.reuniteBySymbol(instr);
                // get the Expresion
                Expression assignment = new Expression(assignment_string);
                return new Assignment(var_name, assignment);
            }

            // for loop
            if (instr[0] == "for")
            {
                Debugging.assert(raw_instr._is_nested);
                Debugging.assert(instr[1].StartsWith("(") && instr[1].EndsWith(")"));
                List<string> sub_instr =
                    StringUtils.splitStringKeepingStructureIntegrity(instr[1].Substring(1, instr[1].Length - 2), ',', Global.base_delimiters);
                sub_instr = StringUtils.purgeLines(sub_instr);
                Debugging.print(sub_instr);
                Debugging.assert(sub_instr.Count == 3);

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
            
            // while loop
            if (instr[0] == "while")
            {
                // syntax check
                Debugging.assert(instr.Count == 2);

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
            
            
            // if statement
            if (instr[0] == "if")
            {
                // syntax check
                Debugging.assert(instr.Count == 2);
                
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
            
            // void function call (no return value, or return value not used)
            if (instr[0].Contains('('))
            {
                Debugging.assert(instr.Count == 1);
                string function_name = instr[0].Split('(')[0]; // extract function name
                Functions.assertFunctionExists(function_name); // assert function exists
                Debugging.print("expr_string for function call ", instr[0]);

                Expression function = new Expression(instr[0]);
                return new VoidFunctionCall(function);
            }

            Debugging.print("not recognized line ", raw_instr._instr);
            throw Global.aquilaError();
        }
    }
}