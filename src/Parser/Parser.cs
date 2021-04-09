﻿using System;
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
        /// Read a text-based pseudo-code file. Removes all the comments using
        /// the strings in <see cref="Global"/>: <see cref="Global.single_line_comment_string"/>, <see cref="Global.multiple_lines_comment_string"/>.
        /// The multiple-line comment closing tag is the calculated mirror of the <see cref="Global.multiple_lines_comment_string"/> string.
        /// </summary>
        /// <param name="path"> relative or full path to file</param>
        /// <returns> List of the lines, without the '\n' char or comments</returns>
        public static Dictionary<int, string> readLines(string path)
        {
            // read file to end
            StreamReader file = new StreamReader(path);
            string full = file.ReadToEnd();
            file.Close();
            
            // get the comment strings
            string sl_flag = Global.single_line_comment_string; // single line comment string
            string ml_open_flag = Global.multiple_lines_comment_string; // multiple lines opening comment string
            char[] char_array = ml_open_flag.ToCharArray();
            Array.Reverse( char_array );
            string ml_close_flag = new string( char_array ); // multiple lines closing comment string

            // setup useful variables
            string filtered = ""; // the filtered code, without comments
            int in_comment = 0; // 0: in code, 1: in single line comment, 2: in multiple lines comment, 3: in multiple lines comment that is actually multiple lines long
            bool line_passed = false; // has a line just passed ? ('\n')

            for (int i = 0; i < full.Length; i++)
            {
                char c = full[i];
                // new line incoming
                if (c == '\n') line_passed = true;

                /* if we are in a comment, we want to check two things:
                 *  - if single-line comment: has the line just passed ?
                 *  - if multiple-lines comment: has the line just passed & are we finished with multiple lines ?
                 * if not in a comment:
                 *  - check if current char is beginning of multiple/single-line(s) comment
                 *  - if not, add the current char to the filtered output
                 */
                if (in_comment == 0) // -> in actual code
                {
                    // single line comment
                    if (i >= sl_flag.Length && full.Substring(i - sl_flag.Length, sl_flag.Length) == sl_flag)
                    {
                        filtered = filtered.Substring(0, filtered.Length - sl_flag.Length);
                        in_comment = 1;
                    }
                    // multiple lines comment
                    else if (i >= ml_open_flag.Length && full.Substring(i - ml_open_flag.Length, ml_open_flag.Length) == ml_open_flag)
                    {
                        filtered = filtered.Substring(0, filtered.Length - ml_open_flag.Length);
                        in_comment = 2;
                    }
                    // in code !
                    else
                    {
                        filtered += c;
                    }
                }
                else if (in_comment == 1) // -> single line comment
                {
                    // line just passed
                    if (line_passed)
                    {
                        in_comment = 0;
                        filtered += "\n"; // add the current char (it will not be added at all if it is not done here)
                    }
                    else
                    {
                        filtered += " ";
                    }
                }
                else // in_comment >= 2 -> multiple lines comment, with line passed or not (== 2 -> not, 3 is passed !)
                {
                    // closing ml comment tag
                    if (i >= ml_close_flag.Length && full.Substring(i - ml_close_flag.Length, ml_close_flag.Length) == ml_close_flag)
                    {
                        filtered += in_comment == 3 ? "\n" : " "; // add correct char to filtered (this char literally replaced the ml comment)
                        filtered += c; // would not be added if not done here
                        in_comment = 0; // back to being in the code
                    }
                    else if (line_passed)
                    {
                        // the ml comment is actually multiple lines long
                        in_comment = 3;
                        filtered += "\n";
                    }
                    else
                    {
                        filtered += " ";
                    }
                }
                
                line_passed = false;
            }

            if (in_comment != 0) throw Global.aquilaError(); // InvalidComment(Tag?)Exception

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
        /// Transforms a string into a <see cref="DynamicList"/> object.
        /// The string should be formatted as follows: "[a,b,c,d]" where
        /// a,b,c and d are either <see cref="Integer"/>s or other <see cref="DynamicList"/>s.
        /// <para/> All the spaces will be removed. They are therefore tolerated anywhere in the input
        /// <para/> Values are separated by commas: ','
        /// <para/> The string starts with an opening bracket and ends with a closing one
        /// </summary>
        /// <param name="s"> string that describes a dynamic list</param>
        /// <returns> The <see cref="DynamicList"/> described by the given string</returns>
        /// <exception cref="Global.aquilaError"></exception>
        public static DynamicList string2DynamicList(string s)
        {
            s = s.Replace(" ", "");
            // ReSharper disable once UseIndexFromEndExpression
            if (s[0] != 91 || s[s.Length - 1] != 93)
            {
                throw Global.aquilaError(); // NoMatchingTagError
            }

            if (s == "[]")
            {
                return new DynamicList();
            }

            s = s.Substring(1, s.Length - 2);

            List<string> splitted = StringUtils.splitStringKeepingStructureIntegrity(s, ',', Global.base_delimiters);
            DynamicList list = new DynamicList();

            foreach (string split in splitted)
            {
                if (split[0] == 91)
                {
                    // ReSharper disable once UseIndexFromEndExpression
                    if (split[split.Length - 1] != 93)
                    {
                        throw Global.aquilaError(); // syntax
                    }
                }
                list.addValue(Expression.parse(split));
            }

            return list;
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
            Debugging.print("setting macro key: \"" + key + "\"" +
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
                /* --- import code --- */
                case "use":
                    //TODO
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
            }
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
            Global.setSetting("interactive", false);
	        Global.setSetting("debug", true);
	        Global.setSetting("trace debug", true);
            Global.setSetting("allow tracing in frozen context", true);
            Global.setSetting("flame mode", true); // can set to false bc "allow tracing in frozen context" is set to true. but to be sure: true
            Global.setSetting("implicit declaration in assignment", true);

            Global.setStdout(new StreamWriter("log.log"));

            //throw new Exception("test");

            // translation
            /*string path = args.Length > 0 ? args[0] : "merge sort.aq";
            IEnumerable<Instruction> instructions = Translator.instructionsFromFile(path);
            CSharpTranslator translator = new CSharpTranslator(instructions);
            string[] output = translator.translate();

            for (int i = 0; i < output.Length; i++)
            {
                Global.stdoutWriteLine(i + ": " + output[i]);
            }*/
            
            // ReSharper disable ConditionIsAlwaysTrueOrFalse

            Global.func_tracers.Add(new FuncTracer("list_at"));
            Global.func_tracers.Add(new FuncTracer("swap"));

            if (args.Length > 0) Global.setSetting("interactive", false); // for Atom running!

            List<string> exec_lines = new List<string>()
            {
                "decl l [1, 5, 2, 9]",
                "decl k [0, 0, 0, 0]",
                "top_down_merge($l, 0, 2, 4, $k)",
                "interactive_call(vars)"
            };
            
            if (Global.getSetting("interactive") || args.Length > 0 && args[0] == "interactive")
            {
                Interpreter.interactiveMode(exec_lines);
                return;
            }

            Global.stdoutWriteLine(args.Length > 0 ? args[0] : "");

            string src_code = args.Length == 1 ? args[0] : @"C:\Users\Nicolas\Code Vultus\Assets\Code Parsing\Aquila scripts\bubble sort.aq";//"merge sort.aq"; // "Leibniz-Gregory PI approximation.aq" // "test.aq" // "bubble sort.aq" // "rule 110.aq";
            bool interactive_after_execution = false;//args.Length == 0;

            Algorithm algo = Interpreter.algorithmFromSrcCode(src_code);
            try
            {
                Variable return_value = Interpreter.runAlgorithm(algo);
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
                Interpreter.interactiveMode(exec_lines, false);
            }

            // ReSharper restore HeuristicUnreachableCode
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore RedundantAssignment
        }
    }
}
