using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable SuggestVarOrType_Elsewhere

namespace Parser
{
    /// <summary>
    /// <see cref="Debugging"/> is used for assertions and logging (Enabled with the <see cref="Global.settings"/>["debug"] parameter).
    /// </summary>
    public static class Debugging
    {
        /// <summary>
        /// Same as "System.Diagnostics.Debug.Assert", but can be called anywhere.
        /// <param name="condition"> if condition is false, raises and exception</param>
        /// <param name="custom_exception"> If the Assertion fails and this is not null, it will be raised instead of the generic Exception</param>
        /// </summary>
        public static void assert(bool condition, Exception custom_exception = null)
        {
            // is the condition ok ?
            if (condition) return;
            // the assertion failed
            if (custom_exception != null) throw custom_exception; // custom_exception has been given ?
            // throw generic error
            StackTrace stack_trace = new StackTrace();
            string call_name = stack_trace.GetFrame(1).GetMethod().Name;
            throw new Exception(call_name + " (" + Global.current_line_index + "): Debugging.Assertion Error. CUSTOM ERROR");
        }

        /// <summary>
        /// Outputs the args to the stdout stream if <see cref="Global.settings"/>["debug"] is set to true
        /// </summary>
        /// <param name="args"> consecutive calls of the ".ToString()" method of these will be printed</param>
        public static void print(params object[] args)
        {
            // if not in debugging mode, return
            if (!Global.getSetting("debug")) return;

            // default settings
            int max_call_name_length = 30;
            int num_new_method_separators = 25;
            bool enable_function_depth = true;
            int num_function_depth_chars = 4;
            string prefix = "+ DEBUG";

            // print the args nicely
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            StringUtils.nicePrintFunction(max_call_name_length, num_new_method_separators, enable_function_depth,
                num_function_depth_chars, prefix, args);
        }
    }

    /// <summary>
    /// The Parser class is used to transform string expressions into
    /// instances of <see cref="Variable"/> and other similar objects. It is the main class used to
    /// transform a text-based algorithm into an instance of <see cref="Algorithm"/>
    /// </summary>
    internal static class Parser
    {
        /// <summary>
        /// This function is used to read a raw Aquila source code text-based file and
        /// purge the code (remove comments and all unnecessary spaces and tabs)
        /// </summary>
        /// <param name="path"> lib_path to your source code file</param>
        /// <returns> list of lines containing your purged code</returns>
        internal static Dictionary<int, string> readSourceCode(string path)
        {
            Dictionary<int, string> lines = readLines(path);
            print(lines.Count + " lines read");
            lines = StringUtils.normalizeWhiteSpacesInStrings(lines);
            print($"lines normalized. remaining: {lines.Count}");
            foreach (var pair in lines)
            {
                Global.stdoutWriteLine($"{pair.Key}: {pair.Value}");
            }
            lines = StringUtils.refactorCodeShortcuts(lines, new []{('[', ']'), ('(', ')')});
            print($"lines refactored. remaining: {lines.Count}");
            foreach (var pair in lines)
            {
                Global.stdoutWriteLine($"{pair.Key}: {pair.Value}");
            }

            return lines;
        }
        
        /// <summary>
        /// Read a text-based pseudo-code file. Removes all the comments using
        /// the strings in <see cref="Global"/>: <see cref="Global.single_line_comment_string"/>, <see cref="Global.multiple_lines_comment_string"/>.
        /// The multiple-line comment closing tag is the calculated mirror of the <see cref="Global.multiple_lines_comment_string"/> string.
        /// </summary>
        /// <param name="path"> relative or full path to file</param>
        /// <returns> List of the lines, without the '\n' char or comments</returns>
        private static Dictionary<int, string> readLines(string path)
        {
            // read file to end
            StreamReader file = new StreamReader(path);
            string full = file.ReadToEnd();
            file.Close();

            string filtered = StringUtils.removeComments(full);

            // create new dict
            string[] splitted = filtered.Split('\n');
            Dictionary<int, string> lines = new Dictionary<int, string>();
            for (int i = 0; i < splitted.Length; i++)
            {
                lines.Add(i + 1, splitted[i]);
            }

            return lines;
        }

        /// <summary>
        /// Extract all the macro preprocessor values from an Aquila algorithm.
        /// These values are always preceded by a '#' char
        /// </summary>
        /// <param name="lines"> lines of code</param>
        /// <returns> (macro_name, string_value) dict</returns>
        public static List<(string, string)> getMacroPreprocessorValues(List<string> lines)
        {
            List<(string, string)> macros = new List<(string, string)>();
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                {
                    string macro_name = line.Split(' ')[0].Substring(1);
                    string macro_value = line.Length > macro_name.Length + 1 ? line.Substring(macro_name.Length + 2) : null; // [2] = [1 for '#' (start)] + [1 for ' ' (end)]
                    macros.Add((macro_name, macro_value));
                }
            }

            return macros;
        }

        /// <summary>
        /// Handle a preprocessor macro.
        /// </summary>
        /// <param name="key"> the macro key (after the '#')</param>
        /// <param name="value"> the macro value (after the key)</param>
        public static void handleMacro(string key, string value)
        {
            Debugging.print("macro key: \"" + key + "\"" +
                            (value == null
                                ? " with no value"
                                : " with value \"" + value + "\""));
            bool bool_value;
            switch (key)
            {
                /* -- Global -- */
                case "setting":
                    Debugging.print("settings config in macro handling");
                    // ReSharper disable once ConvertToNullCoalescingCompoundAssignment
                    value = value ?? ""; // not using "??=" bc of Unity C# version
                    value = StringUtils.normalizeWhiteSpaces(value);
                    var list = StringUtils.splitStringKeepingStructureIntegrity(value, ' ', Global.base_delimiters);
                    if (list.Count != 2)
                    {
                        Debugging.print("The setting macro takes exactly two args: key & value");
                        return;
                    }

                    string setting_key = list[0];
                    string setting_value = list[1];
                    setting_key = StringUtils.removeRedundantMatchingDelimiters(setting_key, '(', ')');
                    setting_key = StringUtils.removeRedundantMatchingDelimiters(setting_key, '[', ']');

                    try
                    {
                        Debugging.print($"setting {setting_key} to {setting_value}");
                        bool_value = StringUtils.string2BoolValue(setting_value ?? "true");
                        Global.setSetting(setting_key, bool_value);
                    }
                    catch (KeyNotFoundException)
                    {
                        Debugging.print($"Setting {setting_key} does not exist.");
                    }
                    catch (ArgumentException)
                    {
                        Debugging.print("invalid boolean value: " + setting_value);
                    }

                    break;
                /* --- Code Vultus --- */
                case "name": // set the algorithm name
                    //TODO
                    break;
                /* --- load libraries --- */
                case "load":
                    string lib_path = value;
                    if (!File.Exists(value)) throw new FileNotFoundException($"Library file not found: \"{lib_path}\"");
                    Interpreter.loadLibrary(value);
                    break;
                /* --- debugging --- */
                case "debug": // manually en/dis-able debugging
                    bool_value = StringUtils.string2BoolValue(value ?? "true");
                    Global.setSetting(key, bool_value);
                    break;
                case "trace_debug":// manually en/dis-able tracing-debugging
                    bool_value = StringUtils.string2BoolValue(value ?? "true");
                    Global.setSetting(key.Replace('_', ' '), bool_value);
                    break;
                /* --- automatic/restrictive tracing --- */
                case "trace_all":
                    bool_value = StringUtils.string2BoolValue(value ?? "true");
                    Global.setSetting(key.Replace('_', ' '), bool_value);
                    break;
                case "trace_type": // e.g. "#trace_type list float" would only trace the list and float types (should be used with "trace_all") -> does not automatically trace, but restricts the tracing
                    //TODO
                    break;
                /* --- global variable behaviour --- */
                case "force_explicit_typing": // cannot use "auto" keyword (or implicit "auto" keyword)
                    //TODO
                    break;
                case "overwrite_variables": // // manually en/dis-able the "overwrite" keyword for variable overwriting
                    //TODO
                    break;
                case "safe_declaration": // manually en/dis-able the "safe" keyword for declarations
                    //TODO
                    break;
                /* --- list behaviour --- */
                case "force_mono_type_list": // lists can only hold one data type
                    //TODO
                    break;
                case "list_as_array": // lists as arrays -> disable insert, remove, append, etc. (every length-modifying function) //! should create function to init an array with type for this
                    //TODO
                    break;
                default:
                    throw new AquilaExceptions.UnknownMacroError($"Macro keyword \"{key}\" is unknown");
            }
        }

        /// <summary>
        /// Outputs the args to the stdout stream if <see cref="Global.settings"/>["debug"] is set to true
        /// </summary>
        /// <param name="args"> consecutive calls of the ".ToString()" method of these will be printed</param>
        public static void print(params object[] args)
        {
            // if not in debugging mode, return
            if (!Global.getSetting("parse debug")) return;

            // default settings
            int max_call_name_length = 30;
            int num_new_method_separators = 25;
            bool enable_function_depth = true;
            int num_function_depth_chars = 4;
            string prefix = "* PARSE";

            // print the args nicely
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            StringUtils.nicePrintFunction(max_call_name_length, num_new_method_separators, enable_function_depth,
                num_function_depth_chars, prefix, args);
        }
    }

    internal static class Program
    {
        // ReSharper disable HeuristicUnreachableCode
        // ReSharper disable once InconsistentNaming
        // ReSharper disable RedundantAssignment

        static void Main(string[] args)
        {
            Global.initVariables();
            //Global.setStdout(new StreamWriter("log.log"));
            // Global.tracer_update_handler_function = test;
            /*string stdin_path = @"C:\Documents\stdin.txt";
            FileStream in_stream = File.Open(stdin_path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(in_stream);
            Global.setStdin(reader);
            string stdout_path = @"C:\Documents\stdout.txt";
            FileStream out_stream =
                File.Open(stdout_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter writer = new StreamWriter(out_stream);
            Global.setStdout(writer);*/

            // ReSharper disable once CommentTypo
            /*string std_lib_path = @"..\..\..\aquila standard lib.aq";
            if (File.Exists(std_lib_path))
            {
                //Parser.handleMacro("load", std_lib_path);
            }*/

	    if (args.Length == 3 && args[0] == "interactive") // interactive path1 path2
	    {
		    Console.WriteLine("Entering cmd-arg mode");
		    // stdin & stdout
		    string stdout_path = args[1], stdin_path = args[2];
		    // settings
		    Global.setSetting("interactive", true);
		    Global.setSetting("debug", false);
		    Global.setSetting("trace debug", false);
		    Global.setSetting("parse debug", false);
		    Global.setSetting("auto trace", true);
		    // redirect stdin/stdout
		    Console.WriteLine("Redirecting outputs & inputs");
		    FileStream in_stream = File.Open(stdin_path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
		    StreamReader reader = new StreamReader(in_stream);
		    Global.setStdin(reader);
		    FileStream out_stream = File.Open(stdout_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
		    StreamWriter writer = new StreamWriter(out_stream);
		    Global.setStdout(writer);
		    // start the interpreter
		    Console.WriteLine("Starting interpreter ...");
		    Interpreter.interactiveMode(rescue_mode:true);
		    // purge & quit
		    Console.WriteLine("Purging & Quitting");
		    File.Delete(stdout_path);
		    File.Delete(stdin_path);
		    Environment.Exit(0);
		    Console.WriteLine("Failed to exit (System.Environment.Exit(0);)");
		    return;
	    }

            Global.setSetting("interactive", true);
            Global.setSetting("parse debug", false);
            Global.setSetting("debug", true);
            Global.setSetting("trace debug", false);
            Global.setSetting("allow tracing in frozen context", false);
            Global.setSetting("flame mode", false); // can set to false bc "allow tracing in frozen context" is set to true. but to be sure: true
            Global.setSetting("implicit declaration in assignment", true);
            Global.setSetting("auto trace", true);

            bool test_run = false;
            
            // ReSharper disable ConditionIsAlwaysTrueOrFalse

            Global.func_tracers.Add(new FuncTracer("list_at"));
            Global.func_tracers.Add(new FuncTracer("swap"));

            if (args.Length > 0) Global.setSetting("interactive", false); // for Atom running!

            List<string> exec_lines = new List<string>
            {
                "for ($i = 0; $i < 1; $i ++)",
                "$min = $i",
                "$j = 4",
                "interactive_call(vars)",
                "end-for"
            };

            if (Global.getSetting("interactive") || args.Length > 0 && args[0] == "interactive")
            {
                Interpreter.interactiveMode(exec_lines);
                return;
            }

            Global.stdoutWriteLine(args.Length > 0 ? args[0] : "");

            string src_code = args.Length == 1 ? args[0] : "merge sort.aq";//"merge sort.aq"; // "Leibniz-Gregory PI approximation.aq" // "test.aq" // "bubble sort.aq" // "rule 110.aq";
            bool interactive_after_execution = args.Length == 0;

            Algorithm algo = Interpreter.algorithmFromSrcCode(src_code);
            try
            {
                //
                if (test_run)
                {
                    Global.stdoutWriteLine("Starting test run...");
                    algo.testRun();
                    foreach (var pair in Global.test_values)
                    {
                        Global.stdoutWriteLine($"{pair.Key} : {StringUtils.dynamic2Str(pair.Value)}");
                    }
                }
                else
                {
                    Global.stdoutWriteLine("Skipped test run");
                }
                
                Global.stdoutWriteLine(" - Main Run -");
                Variable return_value = algo.run();
                Global.stdoutWriteLine("returned value: " + return_value);

                if (interactive_after_execution)
                {
                    Global.stdoutWriteLine("Interactive interpreter after execution:");
                    Interpreter.interactiveMode(exec_lines, false);
                }
            }
            catch (Exception e)
            {
                Global.stdoutWriteLine(e);
                Global.stdoutWriteLine("Error caught. Interactive interpreter instance given for debugging.");
                Global.setSetting("debug", true);
                Global.setSetting("trace debug", true);
                Global.setSetting("parse debug", true);
                Interpreter.interactiveMode(exec_lines, false);
            }

            // var tree = new DataTree();
            // tree.repr();

            // ReSharper restore HeuristicUnreachableCode
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore RedundantAssignment
        }
    }
}
