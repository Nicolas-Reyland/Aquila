using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// The <see cref="Interpreter"/> class is used to interpret and run your code.
    /// For example, to run source code from a source file, follow the following steps:
    /// <para/>* Call <see cref="Parser.readSourceCode"/> to get a purged code
    /// <para/>* Call <see cref="RawInstruction.code2RawInstructions"/> on the purged code
    /// <para/>* Create an <see cref="Algorithm"/> using the generated instructions
    /// <para/>* Call the <see cref="Algorithm.run"/> function on the generated <see cref="Algorithm"/>
    /// You an also use the <see cref="algorithmFromSrcCode"/>, then call the <see cref="Algorithm.run"/> on it
    /// </summary>
    public static class Interpreter
    {
        /// <summary>
        /// Take source code and generates the corresponding <see cref="Algorithm"/>
        /// </summary>
        /// <param name="path"> lib_path to your source code</param>
        /// <param name="print_src"> printTrace the generated purged code</param>
        /// <param name="pretty_print"> pretty_print the <see cref="RawInstruction"/>s (useful to check nested-instruction priorities)</param>
        /// <param name="default_name"> give a name to your <see cref="Algorithm"/></param>
        /// <returns> the generated <see cref="Algorithm"/></returns>
        public static Algorithm algorithmFromSrcCode(string path, bool print_src = false, bool pretty_print = false, string default_name = "no-name-given")
        {
            // read code
            Context.setStatus(Context.StatusEnum.read_purge);
            Context.setInfo(path);
            Dictionary<int, string> lines = Parser.readSourceCode(path);
            Context.reset();

            if (print_src) StringUtils.printStringList(lines.Select(pair => pair.Value).ToList(), true);

            // extract macros
            Context.setStatus(Context.StatusEnum.macro_preprocessing);
            Context.setInfo(null);
            
            List<(string, string)> macros = Parser.getMacroPreprocessorValues(lines.Select(pair => pair.Value).ToList());
            foreach (var pair in macros)
            {
                Parser.handleMacro(pair.Item1, pair.Item2);
            }
            
            Context.reset();

            // Parse code into RawInstructions
            List<RawInstruction> raw_instructions = RawInstruction.code2RawInstructions(lines);
            Parser.print("raw_instructions done");
            
            // Pretty-printTrace code
            foreach (RawInstruction instruction in raw_instructions)
            {
                if (pretty_print) instruction.prettyPrint();
            }

            // Build instructions from the RawInstructions
            List<Instruction> instructions = RawInstruction.buildInstructions(raw_instructions);

            string algo_name = default_name;
            Algorithm algo = new Algorithm(algo_name, instructions);

            return algo;
        }

        /// <summary>
        /// Load an Aquila library
        /// </summary>
        /// <param name="lib_path"> lib_path to the library source file</param>
        /// <exception cref="AquilaExceptions.LibraryLoadingFailedError"> Library load failed</exception>
        public static void loadLibrary(string lib_path)
        {
            Debugging.print("loading lib: " + lib_path);
            // generate the Algorithm
            Algorithm algo = algorithmFromSrcCode(lib_path, default_name : $"use-header-file \"{lib_path}\"");
            // try loading the library
            try
            {
                algo.run();
            }
            catch (Exception e)
            {
                throw new AquilaExceptions.LibraryLoadingFailedError($"Unable to load library at \"{lib_path}\" because of: " + e.Message);
            }

            Debugging.print("finished loading lib: " + lib_path);
        }

        /// <summary>
        /// Checks if input lines from the <see cref="Interpreter.interactiveMode"/> are executable.
        /// <para/>Examples:
        /// <para/>* decl int w // this is executable
        /// <para/>* if ($x &lt; 4) // this is not executable
        /// <para/>* for (decl i 0, $i &lt; 5, $i = $i + 1)
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
            string prefix = " ";
            // debugs
	        bool debug = Global.getSetting("debug");
	        bool trace_debug = Global.getSetting("trace debug");
            bool parse_debug = Global.getSetting("parse debug");
            // any ?
            int debug_count = (debug ? 1 : 0) + (trace_debug ? 1 : 0) + (parse_debug ? 1 : 0);
            if (debug_count == 0) return prefix;
            // some debug is enabled
            prefix += "(";
            if (debug) prefix += "debug | ";
            if (trace_debug) prefix += "trace | ";
            if (parse_debug) prefix += "parse | ";

            return prefix.Substring(0, prefix.Length - 3) + ")";
        }

        /// <summary>
        /// The interactive mode gives you a shell-like environment in which
        /// you can code in Aquila.
        /// </summary>
        public static void interactiveMode(List<string> exec_lines = null, bool greeting = true, bool rescue_mode = false)
        {
            Global.setSetting("fail on context assertions", false); // disable context checks
            bool new_line = true;
            List<string> current_lines = new List<string>();

            // exec mode
            bool exec_mode = exec_lines != null;

            if (greeting)
            {
		if (rescue_mode) System.Console.WriteLine("interactive mode greeting");
                Global.stdoutWriteLine(" [ - Aquila Interactive Interpreter - ]");
                Global.stdoutWriteLine(" [?] Type \"help\" to get a list of all the interactive-mode-only commands");
                Global.stdoutWriteLine(" [?] See https://github.com/Nicolas-Reyland/Aquila/blob/main/Aquila_Documentation.pdf for some unfinished documentation about Aquila itself");
                if (exec_mode) Global.stdoutWriteLine(" [!] Exec mode enabled. There are executables lines saved. Use the \"exec\" command to run them");
            }

            Context.setStatus(Context.StatusEnum.instruction_main_loop);
            Context.setInfo("Interactive Mode");

            while (true)
            {
                if (new_line) Global.stdoutWrite(getInteractivePrefix() + " > ");
                else Global.stdoutWrite(getInteractivePrefix() + " - ");
		if (rescue_mode) System.Console.WriteLine("asking for input...");
                string input = Global.stdinReadLine();
		if (rescue_mode)
		{
			System.Console.WriteLine("got input: " + input);
			Global.stdoutWriteLine(input);
		}
                if (StringUtils.normalizeWhiteSpaces(input) == "") continue;
                if (StringUtils.normalizeWhiteSpaces(input) == "") continue;
                input = StringUtils.removeComments(input);
                input = StringUtils.normalizeWhiteSpaces(input);

                if (input == "exit")
                {
                    Global.stdoutWriteLine("Exiting.");
                    return;
                }

                if (!processInterpreterInput(input)) continue; // command should not be executed, then continue

                if (input == "exec_info")
		        {
		            if (!exec_mode)
		            {
			            Global.stdoutWriteLine(" [X] Exec mode disabled. You have to have pre-defined executable lines to enable this mode");
		            }
		            else
		            {
			            Global.stdoutWriteLine(" Executable lines:");
			            foreach (string line in exec_lines)
			            {
				            Global.stdoutWriteLine("  " + line);
			            }
                    }
                    continue;
		        }

                if (input == "exec")
                {
                    if (!exec_mode)
                    {
                        Global.stdoutWriteLine(" [X] Exec mode disabled. You have to have pre-defined executable lines to enable this mode");
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
                List<Instruction> instructions = RawInstruction.buildInstructions(raw_instructions);
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
            }
            catch (Exception e)
            {
                Global.stdoutWriteLine(e);
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
                    Global.stdoutWriteLine("All existing interactive-mode-only commands:");
                    // ReSharper disable once RedundantExplicitArrayCreation
                    foreach (string command in new string[]
                    {
                        "help", "exit", "reset_env", "clear", // base interactive-mode
                        "exec", "exec_info", // exec
                        "debug", "trace_debug", "parse_debug", // debugging (enable/disable)
                        "eval %expr", // expr
                        "var %var_name", "vars", "$%var_name", // variables
                        // ReSharper disable once StringLiteralTypo
                        "funcs", "df_funcs", // functions
                        "type", // ?
                        "trace_info", "trace_uniq_stacks", "rewind %n %var_name", "peek_trace $%traced_value", // trace
                        "get_context", "set_status", "set_info_null", "reset_context", // context
                        "scope_info", // scope
                        "show-settings", // settings
                    })
                    {
                        Global.stdoutWriteLine("  -> " + command);
                    }

                    return false;
                }
                case "clear":
                    if (!Global.getSetting("redirect debug stout & stderr")) Console.Clear();
                    return false;
                case "vars":
                {
                    Global.stdoutWriteLine("Global Variables:");
                    foreach (var pair in Global.getGlobalVariables())
                    {
                        Global.stdoutWriteLine("\t" + pair.Key + " : " + pair.Value);
                    }
                    int i = 0;
                    Global.stdoutWriteLine("Local Scope Variables:");
                    foreach (var dict in Global.getCurrentDictList())
                    {
                        i++;
                        foreach (var pair in dict)
                        {
                            Global.stdoutWrite(new string('\t', i));
                            Global.stdoutWrite(pair.Key + " : ");
                            Global.stdoutWriteLine(pair.Value.ToString());
                        }
                    }
                    
                    return false;
                }
                // ReSharper disable once StringLiteralTypo
                case "funcs":
                {
                    Global.stdoutWriteLine("User defined functions:");
                    foreach (var pair in Functions.user_functions)
                    {
                        Global.stdoutWriteLine(" * " + pair.Key + (pair.Value.isRec() ? " : [rec] " : " : ") + "(" + pair.Value.func_args.Count + ") -> " + pair.Value.getType());
                    }

                    return false;
                }
                case "df_funcs":
                    Global.stdoutWriteLine("Default (predefined) functions:");
                    foreach (var pair in Functions.functions_dict)
                    {
                        MethodInfo method = pair.Value.GetMethodInfo();
                        Global.stdoutWriteLine(" * " + pair.Key + " [?] : (" + method.GetParameters().Length + ") -> " + method.ReturnType);
                    }

                    return false;
                case "debug":
                    Global.setSetting("debug", !Global.getSetting("debug"));
                    return false;
                case "trace_debug":
                    Global.setSetting("trace debug", !Global.getSetting("trace debug"));
                    return false;
                case "parse_debug":
                    Global.setSetting("parse debug", !Global.getSetting("parse debug"));
                    return false;
                case "trace_info":
                {
                    Global.stdoutWriteLine("var tracers:");
                    foreach (VarTracer tracer in Global.var_tracers)
                    {
                        Global.stdoutWriteLine(" - var     : " + (tracer.getTracedObject() as Variable).getName());
                        Global.stdoutWriteLine(" - stack   : " + tracer.getStackCount());
                        Global.stdoutWriteLine(" - updated : " + tracer.last_stack_count);
                    }
                    Global.stdoutWriteLine("func tracers:");
                    foreach (FuncTracer tracer in Global.func_tracers)
                    {
                        Global.stdoutWriteLine(" - func  : " + tracer.getTracedObject());
                        Global.stdoutWriteLine(" - stack : " + tracer.getStackCount());
                    }

                    return false;
                }
                case "trace_uniq_stacks":
                    Global.var_tracers[0].printEventStack();
                    return false;
                case "reset_env":
                    Global.resetEnv();
                    Global.stdoutWriteLine(" [!] Global Environment reset");

                    return false;
                case "get_context":
                {
                    int status = Context.getStatus();
                    Context.StatusEnum status_quote = (Context.StatusEnum) status;
                    object info = Context.getInfo();
                    bool blocked = Context.isFrozen();
                    bool enabled = Global.getSetting("fail on context assertions");
                    Global.stdoutWriteLine("status  : " + status);
                    Global.stdoutWriteLine("quote   : " + status_quote);
                    Global.stdoutWriteLine("info    : " + info);
                    Global.stdoutWriteLine("blocked : " + blocked);
                    Global.stdoutWriteLine("asserts : " + enabled);

                    return false;
                }
                case "set_info_null":
                    Context.setInfo(null);
                    return false;
                case "reset_context":
                    Context.reset();
                    return false;
                case "type":
                    Global.stdoutWriteLine("type of NullVar: " + typeof(NullVar));
                    Global.stdoutWriteLine("type of Variable: " + typeof(Variable));
                    Global.stdoutWriteLine("type of Integer: " + typeof(Integer));

                    return false;
                case "scope_info":
                    Global.stdoutWriteLine("local scope depth: " + Global.getLocalScopeDepth());
                    Global.stdoutWriteLine("main scope depth: " + Global.getMainScopeDepth());

                    return false;
                case "show-settings":
                    foreach (var pair in Global.getSettings())
                    {
                        Global.stdoutWriteLine($" - {pair.Key} : {pair.Value}");
                    }

                    return false;
            }

            if (input[0] == '$' && Global.variableExistsInCurrentScope(input.Substring(1)))
            {
                Global.stdoutWriteLine(Global.variableFromName(input.Substring(1)).ToString());
                return false;
            }

            if (input.StartsWith("set_status "))
            {
                int status = int.Parse(input.Substring(11));
                Context.setStatus((Context.StatusEnum) status);
                
                return false;
            }

            if (input.StartsWith("eval "))
            {
                Variable var_ = Expression.parse(input.Substring(5));
                Global.stdoutWriteLine(var_.ToString());
                return false;
            }

            if (input.StartsWith("var "))
            {
                string var_name = input.Substring(4);
                var_name = StringUtils.normalizeWhiteSpaces(var_name);
                if (var_name == "") return false;
                Variable var_ = Global.variableFromName(var_name);
                Global.stdoutWriteLine("name     : " + var_.getName());
                Global.stdoutWriteLine("type     : " + var_.getTypeString());
                Global.stdoutWriteLine("const    : " + var_.isConst());
                Global.stdoutWriteLine("assigned : " + var_.assigned);
                Global.stdoutWriteLine("value    : " + var_);
                Global.stdoutWriteLine("traced   : " + var_.isTraced());
                Global.stdoutWriteLine("^ mode   : " + var_.trace_mode);
                if (var_ is NumericalValue value) Global.stdoutWriteLine("*source  : " + StringUtils.dynamic2Str(value.source_vars));
                return false;
            }

            if (input.StartsWith("rewind"))
            {
                string[] splitted = input.Split(' ');
                if (splitted.Length == 3)
                {
                    if (!int.TryParse(splitted[1], out int n))
                    {
                        Global.stdoutWriteLine("cannot read n");
                        return false;
                    } 

                    string var_name = splitted[2];

                    Variable var_ = Expression.parse(var_name);
                    if (!var_.isTraced())
                    {
                        Global.stdoutWriteLine("Variable is not traced! Use the \"trace\" instruction to start tracing variables");
                        return false;
                    }
                    
                    var_.tracer.rewind(n);
                    return false;
                }

                Global.stdoutWriteLine("split count does not match 3");
                return false;
            }

            if (input.StartsWith("peek_trace"))
            {
                if (input.Length < 11) return false;
                Variable var_ = Expression.parse(input.Substring(11));
                if (!var_.isTraced())
                {
                    Global.stdoutWriteLine("Variable is not traced! Use the \"trace\" instruction to start tracing variables");
                    return false;
                }

                Alteration alter = var_.tracer.peekEvent().alter;
                Global.stdoutWriteLine("name         : " + alter.name);
                Global.stdoutWriteLine("variable     : " + alter.affected.getName());
                Global.stdoutWriteLine("main value   : " + StringUtils.dynamic2Str(alter.main_value));
                Global.stdoutWriteLine("num minor    : " + alter.minor_values.Length);
                Global.stdoutWriteLine("minor values : " + StringUtils.dynamic2Str(alter.main_value));
                Global.stdoutWriteLine("stack count  : " + var_.tracer.getStackCount());
                Global.stdoutWriteLine("updated      : " + var_.tracer.last_stack_count);

                return false;
            }

            if (input.StartsWith("#"))
            {
                Global.stdoutWriteLine("Handling macro parameter");
                if (!input.Contains(' '))
                {
                    Parser.handleMacro(input.Substring(1), null);
                    return false;
                }

                int index = input.IndexOf(' ');
                // extract the key & value pair
                string value = input.Substring(index);
                input = input.Substring(1, index);
                // purge pair
                value = StringUtils.normalizeWhiteSpaces(value);
                input = StringUtils.normalizeWhiteSpaces(input);
                Parser.handleMacro(input, value);
                return false;
            }

            return true;
        }
    }
}
