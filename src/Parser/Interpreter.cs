using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

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
    internal static class Interpreter
    {
        /// <summary>
        /// This function is used to read a raw Aquila source code text-based file and
        /// purge the code (remove comments and all unnecessary spaces and tabs)
        /// </summary>
        /// <param name="path"> path to your source code file</param>
        /// <returns> list of lines containing your purged code</returns>
        private static Dictionary<int, string> readSourceCode(string path)
        {
            Dictionary<int, string> lines = Parser.readLines(path);
            Debugging.print(lines.Count + " lines read");
            lines = StringUtils.purgeLines(lines); // same as "lines = StringUtils.purgeLines(lines);"
            Debugging.print("lines purged. remaining: " + lines.Count);

            return lines;
        }

        /// <summary>
        /// Build <see cref="Instruction"/>s from <see cref="RawInstruction"/>s
        /// </summary>
        /// <param name="raw_instructions"> list of <see cref="RawInstruction"/>s</param>
        /// <returns> list of corresponding <see cref="Instruction"/>s</returns>
        private static List<Instruction> buildInstructions(List<RawInstruction> raw_instructions)
        {
            Context.setStatus(Context.StatusEnum.building_instructions);
            Context.setInfo(raw_instructions);

            List<Instruction> instructions = raw_instructions.Select(raw_instruction => raw_instruction.toInstr()).ToList();

            Context.reset();
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
            Context.setStatus(Context.StatusEnum.read_purge);
            Context.setInfo(path);
            Dictionary<int, string> lines = readSourceCode(path);
            Context.reset();

            if (print_src) StringUtils.printStringList(lines.Select(pair => pair.Value).ToList(), true);

            // extract macros
            Context.setStatus(Context.StatusEnum.macro_preprocessing);
            Context.setInfo(null);
            
            Dictionary<string, string> macros = Parser.getMacroPreprocessorValues(lines.Select(pair => pair.Value).ToList());
            foreach (var pair in macros)
            {
                Parser.handleMacro(pair.Key, pair.Value);
            }
            
            Context.reset();

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
                foreach (var nested_instruction_flag in Global.nested_instruction_flags)
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
        /// <para/>Looking at <see cref="Global.getSetting"/>("debug") and <see cref="Global.getSetting"/>("trace debug")
        /// </summary>
        /// <returns> command input prefix</returns>
        private static string getInteractivePrefix()
        {
	    bool debug = Global.getSetting("debug");
	    bool trace_debug = Global.getSetting("trace debug");
            if (debug && trace_debug) return " (debug | trace)";
            if (debug) return " (debug)";
            if (trace_debug) return " (trace)";
            return " ";
        }

        /// <summary>
        /// The interactive mode gives you a shell-like environment in which
        /// you can code in Aquila. It is important to note that comments are not supported
        /// if written on the same line as code. You comment lines have to start with the "//"
        /// symbol. Multiple-line comments are not supported
        /// </summary>
        public static void interactiveMode(List<string> exec_lines = null, bool greeting = true)
        {
            Global.setSetting("fail on context assertions", false); // disable context checks
            bool new_line = true;
            List<string> current_lines = new List<string>();

            // exec mode
            bool exec_mode = exec_lines != null;

            if (greeting)
            {
                Console.WriteLine(" [ - Aquila Interactive Interpreter - ]");
                Console.WriteLine(" [?] Type \"help\" to get a list of all the interactive-mode-only commands");
                Console.WriteLine(" [?] See https://github.com/Nicolas-Reyland/Aquila/blob/main/Aquila_Documentation_unfinished.pdf for some unfinished documentation about Aquila itself");
                if (exec_mode) Console.WriteLine(" [!] Exec mode enabled. There are executables lines saved. Use the \"exec\" command to run them");
            }

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
                int temp_i = 0;
                List<RawInstruction> raw_instructions = RawInstruction.code2RawInstructions(lines.ToDictionary(_ => temp_i++, x => x));
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
        internal static bool processInterpreterInput(string input)
        {
            switch (input)
            {
                case "help":
                {
                    Console.WriteLine("All existing interactive-mode-only commands:");
                    // ReSharper disable once RedundantExplicitArrayCreation
                    foreach (string command in new string[]
                    {
                        "help", "exit", "reset_env", "clear", // base interactive-mode
                        "exec", "exec_info", // exec
                        "debug", "trace_debug", // debugging (enable/disable)
                        "eval %expr", // expr
                        "var %var_name", "vars", "$%var_name", // variables
                        // ReSharper disable once StringLiteralTypo
                        "funcs", "type", // functions
                        "trace_info", "trace_uniq_stacks", "rewind %n %var_name", "peek_event $%traced_value", // trace
                        "get_context", "set_status", "set_info_null", "reset_context", // context
                        "scope_info", // scope
                    })
                    {
                        Console.WriteLine("  -> " + command);
                    }

                    return false;
                }
                case "clear":
                    Console.Clear();
                    return false;
                case "vars":
                {
                    int i = 0;
                    foreach (var dict in Global.getCurrentDictList())
                    {
                        foreach (var pair in dict)
                        {
                            Console.Write(new string('\t', i));
                            Console.Write(pair.Key + " : ");
                            Console.WriteLine(pair.Value.ToString());
                        }

                        i++;
                    }
                    
                    return false;
                }
                // ReSharper disable once StringLiteralTypo
                case "funcs":
                {
                    foreach (var pair in Functions.user_functions)
                    {
                        Console.WriteLine(" * " + pair.Key + (pair.Value.isRec() ? " : [rec] " : " : ") + "(" + pair.Value.func_args.Count + ") -> " + pair.Value.getType());
                    }

                    return false;
                }
                case "debug":
                    Global.setSetting("debug", !Global.getSetting("debug"));
                    return false;
                case "trace_debug":
                    Global.setSetting("trace debug", !Global.getSetting("trace debug"));
                    return false;
                case "trace_info":
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
                case "trace_uniq_stacks":
                    Global.var_tracers[0].printEventStack();
                    return false;
                case "reset_env":
                    Global.resetEnv();
                    Console.WriteLine(" [!] Global Environment reset");

                    return false;
                case "get_context":
                {
                    int status = Context.getStatus();
                    Context.StatusEnum status_quote = (Context.StatusEnum) status;
                    object info = Context.getInfo();
                    bool blocked = Context.isFrozen();
                    bool enabled = Global.getSetting("fail on context assertions");
                    Console.WriteLine("status  : " + status);
                    Console.WriteLine("quote   : " + status_quote);
                    Console.WriteLine("info    : " + info);
                    Console.WriteLine("blocked : " + blocked);
                    Console.WriteLine("asserts : " + enabled);

                    return false;
                }
                case "set_info_null":
                    Context.setInfo(null);
                    return false;
                case "reset_context":
                    Context.reset();
                    return false;
                case "type":
                    Console.WriteLine("type of NullVar: " + typeof(NullVar));
                    Console.WriteLine("type of Variable: " + typeof(Variable));
                    Console.WriteLine("type of Integer: " + typeof(Integer));

                    return false;
                case "scope_info":
                    Console.WriteLine("local scope depth: " + Global.getLocalScopeDepth());
                    Console.WriteLine("main scope depth: " + Global.getMainScopeDepth());

                    return false;
            }

            if (input[0] == '$' && Global.variableExistsInCurrentScope(input.Substring(1)))
            {
                Console.WriteLine(Global.variableFromName(input.Substring(1)).ToString());
                return false;
            }

            if (input.StartsWith("set_status "))
            {
                int status = Int32.Parse(input.Substring(11));
                Context.setStatus((Context.StatusEnum) status);
                
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
                    Variable var_ = Global.variableFromName(var_name);
                    Console.WriteLine("name     : " + var_.getName());
                    Console.WriteLine("type     : " + var_.getTypeString());
                    Console.WriteLine("value    : " + var_);
                    Console.WriteLine("assigned : " + var_.assigned);
                }
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
            
            return true;
        }
    }
}
