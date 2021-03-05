using System;
using System.Collections.Generic;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// The <see cref="Interpreter"/> class is used to interpret and run your code.
    /// For example, to run source code from a source file, follow the following steps:
    /// <para/>* Call <see cref="readSourceCode"/> to get a purged code
    /// <para/>* Call <see cref="RawInstruction.code2RawInstructions"/> on the purged code
    /// <para/>* Call <see cref="buildInstructions"/> on the generated raw instructions
    /// <para/>* Create an <see cref="Algorithm"/> using the generated instructions
    /// <para/>* Call the <see cref="runAlgorithm"/> function on the generated <see cref="Algorithm"/>
    /// You an also use the <see cref="algorithmFromSrcCode"/>, then call the <see cref="runAlgorithm"/> on it
    /// </summary>
    static class Interpreter
    {
        /// <summary>
        /// This function is used to read a raw Aquila source code text-based file and
        /// purge the code (remove comments and all unnecessary spaces and tabs)
        /// </summary>
        /// <param name="path"> path to your source code file</param>
        /// <returns> list of lines containing your purged code</returns>
        private static List<string> readSourceCode(string path)
        {
            List<string> lines = Parser.readLines(path);
            Debugging.print("lines read");
            lines = StringUtils.purgeLines(lines); // same as "lines = StringUtils.purgeLines(lines);"
            Debugging.print("lines purged");

            return lines;
        }

        /// <summary>
        /// Build <see cref="Instruction"/>s from <see cref="RawInstruction"/>s
        /// </summary>
        /// <param name="raw_instructions"> list of <see cref="RawInstruction"/>s</param>
        /// <returns> list of corresponding <see cref="Instruction"/>s</returns>
        public static List<Instruction> buildInstructions(List<RawInstruction> raw_instructions)
        {
            List<Instruction> instructions = new List<Instruction>();
            foreach (RawInstruction raw_instruction in raw_instructions)
            {
                Instruction instruction = raw_instruction.toInstr();
                instructions.Add(instruction);
            }

            return instructions;
        }
        
        /// <summary>
        /// Take source code and generate the corresponding <see cref="Algorithm"/>
        /// </summary>
        /// <param name="path"> path to your source code</param>
        /// <param name="print_src"> printTrace the generated purged code</param>
        /// <param name="pretty_print"> pretty_print the <see cref="RawInstruction"/>s (useful to check nested-instruction priorities)</param>
        /// <param name="default_name"> give a name to your <see cref="Algorithm"/></param>
        /// <returns> the generated <see cref="Algorithm"/></returns>
        public static Algorithm algorithmFromSrcCode(string path, bool print_src = false, bool pretty_print = false, string default_name = "no-name-given")
        {
            // read code
            List<string> lines = readSourceCode(path);

            if (print_src) StringUtils.printStringList(lines, true);

            // extract macros
            Dictionary<string, string> macros = Parser.getMacroPreprocessorValues(lines);

            // Parse code into RawInstructions
            List<RawInstruction> raw_instructions = RawInstruction.code2RawInstructions(lines);
            Debugging.print("raw_instructions done");
            
            // Pretty-printTrace code
            foreach (RawInstruction instruction in raw_instructions)
            {
                if (pretty_print) instruction.prettyPrint();
            }

            // Build instructions from the RawInstructions
            List<Instruction> instructions = buildInstructions(raw_instructions);

            string algo_name = macros.ContainsKey("name") ? macros["name"] : default_name;
            Algorithm algo = new Algorithm(algo_name, instructions);

            return algo;
        }

        /// <summary>
        /// Run an <see cref="Algorithm"/> and return ints return value (if none is given: <see cref="NullVar"/>
        /// </summary>
        /// <param name="algo"> input <see cref="Algorithm"/></param>
        /// <returns> return value of the <see cref="Algorithm"/></returns>
        public static Variable runAlgorithm(Algorithm algo)
        {
            Variable return_value = algo.run();
            return return_value;
        }

        /// <summary>
        /// Checks if input lines from the <see cref="Interpreter.interactiveMode"/> are executable.
        /// <para/>Examples:
        /// <para/>* declare int w // this is executable
        /// <para/>* if ($x &lt; 4) // this is not executable
        /// <para/>* for (declare i 0, $i &lt; 5, $i = $i + 1)
        /// <para/> - printTrace($i)
        /// <para/> - print_endl()
        /// <para/> - end-for // this is executable
        /// </summary>
        /// <param name="lines"> input list of lines</param>
        /// <returns> executable lines or not ? (bool value)</returns>
        private static bool executableLines(List<string> lines)
        {
            short depth = 0;
            foreach (string line in lines)
            {
                foreach (KeyValuePair<string,string> nested_instruction_flag in Global.nested_instruction_flags)
                {
                    if (line.StartsWith(nested_instruction_flag.Key))
                    {
                        depth++;
                        break;
                    }

                    if (line == nested_instruction_flag.Value)
                    {
                        depth--;
                        break;
                    }
                }
            }

            return depth == 0;
        }

        /// <summary>
        /// Get the interactive interpreter command prefix (debug, trace_debug related)
        /// <para/>Looking at <see cref="Global.debug"/> and <see cref="Global.trace_debug"/>
        /// </summary>
        /// <returns> command input prefix</returns>
        private static string getInteractivePrefix()
        {
            if (Global.debug && Global.trace_debug) return " (debug | trace)";
            if (Global.debug) return " (debug)";
            if (Global.trace_debug) return " (trace)";
            return " ";
        }

        /// <summary>
        /// The interactive mode gives you a shell-like environment in which
        /// you can code in Aquila. It is important to note that comments are not supported
        /// if written on the same line as code. You comment lines have to start with the "//"
        /// symbol. Multiple-line comments are not supported
        /// <para/>There are special keywords that only exist in the interpreter mode:
        /// <para/>* exit -> exits the interpreter mode
        /// <para/>* clear -> clears the console output
        /// <para/>* var var_name -> prints info about the variable named "var_name"
        /// <para/>* vars -> prints all the existing variables
        /// <para/>* $var_name -> prints the value of a variable
        /// <para/>* debug -> switch the debugging mode (true to false, false to true)
        /// </summary>
        public static void interactiveMode(List<string> exec_lines = null)
        {
            Context.enabled = false; // disable context checks
            bool new_line = true;
            List<string> current_lines = new List<string>();

            // exec mode
            bool exec_mode = exec_lines != null;

            Console.WriteLine(" [ - Aquila Interactive Interpreter - ]");
            Console.WriteLine(" [?] Type \"help\" to get a list of all the interactive-mode-only commands");
            Console.WriteLine(" [?] See https://github.com/Nicolas-Reyland/Aquila/blob/main/Aquila_Documentation_unfinished.pdf for some unfinished documentation about Aquila itself");
            if (exec_mode) Console.WriteLine(" [!] Exec mode enabled. There are executables lines saved. Use the \"exec\" command to run them");

            Context.setStatus(Context.StatusEnum.instruction_main_loop);
            Context.setInfo("Interactive Mode");

            while (true)
            {
                if (new_line) Console.Write(getInteractivePrefix() + " > ");
                else Console.Write(getInteractivePrefix() + " - ");
                string input = Console.ReadLine();

                input = StringUtils.purgeLine(input);

                if (input == "" || input.StartsWith("//")) continue;

                if (input == "exit")
                {
                    Console.WriteLine("Exiting.");
                    return;
                }

                if (!processInterpreterInput(input)) continue; // command should not be executed, then continue

		        if (input == "exec_info")
		        {
		            if (!exec_mode)
		            {
			            Console.WriteLine(" [X] Exec mode disabled. You have to have pre-defined executable lines to enable this mode");
		            }
		            else
		            {
			            Console.WriteLine(" Executable lines:");
			            foreach (string line in exec_lines)
			            {
				            Console.WriteLine("  " + line);
			            }
                    }
                    continue;
		        }

                if (input == "exec")
                {
                    if (!exec_mode)
                    {
                        Console.WriteLine(" [X] Exec mode disabled. You have to have pre-defined executable lines to enable this mode");
                        continue;
                    }
                    executeLines(exec_lines, false);
                }
                else
                {
                    current_lines.Add(input);
                    if (executableLines(current_lines))
                    {
                        executeLines(current_lines);
                        new_line = true;
                    }
                    else
                    {
                        new_line = false;
                    }
                }
            }
        }

        /// <summary>
        /// Execute the input lines
        /// </summary>
        /// <param name="lines"> input lines</param>
        /// <param name="clear_lines"> clear the input lines at the end of execution</param>
        private static void executeLines(List<string> lines, bool clear_lines = true)
        {
            // execute line here
            try
            {
                List<RawInstruction> raw_instructions = RawInstruction.code2RawInstructions(lines);
                List<Instruction> instructions = buildInstructions(raw_instructions);
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
                    
            if (clear_lines) lines.Clear();
        }

        /// <summary>
        /// Process the input from the interpreter.
        /// If the input is one of the interactive-mode-only commands, the command should not
        /// be executed by the interpreter but will be executed by this function manually.
        /// </summary>
        /// <param name="input"> command line from the interactive-mode</param>
        /// <returns> should the command be executed ?</returns>
        private static bool processInterpreterInput(string input)
        {
            if (input == "help")
            {
                Console.WriteLine("All existing interactive-mode-only commands:");
                foreach (string command in new string[]
                {
                    "help", "exit", "reset_env", "clear", // base interactive-mode commands
                    "exec", "exec_info", // exec commands
                    "debug", "trace_debug", // debugging (enable/disable) commands
                    "eval %expr", // expr
                    "var %var_name", "vars", "$%var_name", // variable commands
                    "trace_info", "trace_uniq_stacks", "rewind %n %var_name", "peek_event $%traced_value", // trace commands
                    "get_context", "set_status", "set_info_null", "reset_context", // context commands -> CONTEXT DISABLED IN INTERACTIVE MODE LMAO
                    //"",
                })
                {
                    Console.WriteLine("  -> " + command);
                }

                return false;
            }
            
            if (input == "clear")
            {
                Console.Clear();
                return false;
            }

            if (input.StartsWith("eval "))
            {
                Variable var_ = Expression.parse(input.Substring(5));
                Console.WriteLine(var_.ToString());
                return false;
            }

            if (input.StartsWith("var "))
            {
                string var_name = input.Substring(4);
                var_name = StringUtils.purgeLine(var_name);
                if (var_name != "")
                {
                    Variable var_ = Global.variables[var_name];
                    Console.WriteLine("name     : " + var_.getName());
                    Console.WriteLine("type     : " + var_.getTypeString());
                    Console.WriteLine("value    : " + var_.ToString());
                    Console.WriteLine("assigned : " + var_.assigned);
                }
                return false;
            }

            if (input == "vars")
            {
                foreach (KeyValuePair<string, Variable> variable in Global.variables)
                {
                    Console.Write(variable.Key + " : ");
                    Console.WriteLine(variable.Value.ToString());
                }
                return false;
            }

            if (input[0] == '$' && Global.variables.ContainsKey(input.Substring(1)))
            {
                Console.WriteLine(Global.variables[input.Substring(1)].ToString());
                return false;
            }

            if (input == "debug")
            {
                Global.debug = !Global.debug;
                return false;
            }

            if (input == "trace_debug")
            {
                Global.trace_debug = !Global.trace_debug;
                return false;
            }

            if (input == "trace_info")
            {
                Console.WriteLine("var tracers:");
                foreach (VarTracer tracer in Global.var_tracers)
                {
                    Console.WriteLine(" - var     : " + (tracer.getTracedObject() as Variable).getName());
                    Console.WriteLine(" - stack   : " + tracer.getStackCount());
                    Console.WriteLine(" - updated : " + tracer.last_stack_count);
                }
                Console.WriteLine("func tracers:");
                foreach (FuncTracer tracer in Global.func_tracers)
                {
                    Console.WriteLine(" - func  : " + tracer.getTracedObject());
                    Console.WriteLine(" - stack : " + tracer.getStackCount());
                }

                return false;
            }

            if (input == "trace_uniq_stacks")
            {
                Global.var_tracers[0].printEventStack();
                return false;
            }

            if (input.StartsWith("rewind"))
            {
                string[] splitted = input.Split(' ');
                if (splitted.Length == 3)
                {
                    if (!Int32.TryParse(splitted[1], out int n))
                    {
                        Console.WriteLine("cannot read n");
                        return false;
                    } 

                    string var_name = splitted[2];

                    Variable var_ = Expression.parse(var_name);
                    if (!var_.isTraced())
                    {
                        Console.WriteLine("Variable is not traced! Use the \"trace\" instruction to start tracing variables");
                        return false;
                    }
                    
                    var_.tracer.rewind(n);
                    return false;
                }
                
                Console.WriteLine("split count does not match 3");
                return false;
            }

            if (input.StartsWith("peek_trace"))
            {
                if (input.Length < 11) return false;
                Variable var_ = Expression.parse(input.Substring(11));
                if (!var_.isTraced())
                {
                    Console.WriteLine("Variable is not traced! Use the \"trace\" instruction to start tracing variables");
                    return false;
                }
                Alteration alter = var_.tracer.peekEvent().alter;
                Console.WriteLine("name         : " + alter.name);
                Console.WriteLine("variable     : " + alter.affected.getName());
                Console.WriteLine("main value   : " + StringUtils.dynamic2Str(alter.main_value));
                Console.WriteLine("num minor    : " + alter.minor_values.Length);
                Console.WriteLine("minor values : " + StringUtils.dynamic2Str(alter.minor_values));
                Console.WriteLine("stack count  : " + var_.tracer.getStackCount());
                Console.WriteLine("updated      : " + var_.tracer.last_stack_count);

                return false;
            }

            if (input == "reset_env")
            {
                Global.variables.Clear();
                Global.var_tracers.Clear();
                Global.func_tracers.Clear();
                Global.usable_variables.Clear();
                Console.WriteLine(" [!] Global Environment reset");

                return false;
            }

            if (input == "get_context")
            {
                int status = Context.getStatus();
                Context.StatusEnum status_quote = (Context.StatusEnum) status;
                object info = Context.getInfo();
                bool blocked = Context.isBlocked();
                bool enabled = Context.enabled;
                Console.WriteLine("status  : " + status);
                Console.WriteLine("quote   : " + status_quote);
                Console.WriteLine("info    : " + info);
                Console.WriteLine("blocked : " + blocked);
                Console.WriteLine("enabled : " + enabled);

                return false;
            }

            if (input.StartsWith("set_status "))
            {
                int status = Int32.Parse(input.Substring(11));
                Context.setStatus((Context.StatusEnum) status);
                
                return false;
            }

            if (input == "set_info_null")
            {
                Context.setInfo(null);
                return false;
            }

            if (input == "reset_context")
            {
                Context.reset();
                return false;
            }
            
            return true;
        }
    }
}
