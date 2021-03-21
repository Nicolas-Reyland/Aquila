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
    /// The Context defines where we are at runtime, and gives additional info (<see cref="Context.getInfo"/>).
    /// The info is often the <see cref="Instruction"/> that is being executed.*
    /// If the <see cref="Algorithm"/> execution time hasn't been reached yet,
    /// the info type can vary (string, List, etc.)
    /// </summary>
    public static class Context
    {
        /// <summary>
        /// Status list:
        /// <para/>*  0: undefined
        /// <para/>*  1: reading &amp; purging code
        /// <para/>*  2: processing macros
        /// <para/>*  3: building raw instructions
        /// <para/>*  4: building instructions
        /// <para/>*  5: in the main algorithm main loop
        /// <para/>*  6: in a function main loop
        /// <para/>*  7: executing a trace instruction
        /// <para/>*  8: in a while loop
        /// <para/>*  9: in a for loop
        /// <para/>* 10: in an if statement
        /// <para/>* 11: executing a declaration instruction
        /// <para/>* 12: executing a assignment instruction
        /// <para/>* 13: executing a void function
        /// <para/>* 14: executing a value function
        /// <para/>* 15: algorithm main loop finished
        /// </summary>
        private static int _status = (int) StatusEnum.undefined;
        /// <summary>
        /// Stack of the previous status'. An algorithm being naturally of a
        /// recursive nature, we need a stack to store the status'
        /// </summary>
        private static readonly Stack<int> previous_status = new Stack<int>();
        /// <summary>
        /// Additional information about the current state.
        /// <para/> This could be an <see cref="Instruction"/> or a <see cref="FunctionCall"/>
        /// </summary>
        private static object _info;
        /// <summary>
        /// Stack of the previous infos. An algorithm being naturally of a
        /// recursive nature, we need a stack to store the infos
        /// </summary>
        private static readonly Stack<object> previous_info = new Stack<object>();
        /// <summary>
        /// Is the context blocked ? (see <see cref="Tracer"/> &amp; <see cref="Functions.swapFunction"/>)
        /// </summary>
        private static bool _frozen; // default: false
        /// <summary>
        /// Enum of all the existing status'
        /// </summary>
        public enum StatusEnum
        {
            undefined,                  // 0
            read_purge,                 // 1
            macro_preprocessing,        // 2
            building_raw_instructions,  // 3
            building_instructions,      // 4
            instruction_main_loop,      // 5
            trace_execution,            // 6
            while_loop_execution,       // 7
            for_loop_execution,         // 8
            if_execution,               // 9
            declaration_execution,      // 10
            assignment_execution,       // 11
            predefined_function_call,   // 12
            user_function_call,         // 13
            instruction_main_finished   // 14
        }

        // status
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> current status</returns>
        public static int getStatus() => _status;
        /// <summary>
        /// Used for verifying set/reset calls
        /// </summary>
        /// <returns> status stack count</returns>
        public static int getStatusStackCount() => previous_status.Count;
        /// <summary>
        /// check for status
        /// </summary>
        /// <param name="status_enum"> supposed status</param>
        /// <returns> is the supposed status the actual status ?</returns>
        public static bool statusIs(StatusEnum status_enum) => _status == (int) status_enum;
        /// <summary>
        /// set the new status
        /// </summary>
        /// <param name="new_status"> new status</param>
        private static void setStatus(int new_status)
        {
            previous_status.Push(_status);
            _status = new_status;
        }
        /// <summary>
        /// set the new status
        /// </summary>
        /// <param name="status_enum"> new status</param>
        public static void setStatus(StatusEnum status_enum)
        {
            if (_frozen) return;
            int status_int = (int) status_enum;
            setStatus(status_int);
        }
        /// <summary>
        /// reset the status to the last one
        /// </summary>
        /// <exception cref="InvalidOperationException"> there is no last status (the current one is the first one set)</exception>
        private static void resetStatus()
        {
            if (previous_status.Count == 0)
            {
                throw new InvalidOperationException("No previous Context state");
            }

            _status = previous_status.Pop();
        }

        // info
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> current info</returns>
        public static dynamic getInfo() => _info;
        /// <summary>
        /// set the new info
        /// </summary>
        /// <param name="new_info"> new info</param>
        public static void setInfo(object new_info)
        {
            if (_frozen) return;
            previous_info.Push(_info);
            _info = new_info;
        }
        /// <summary>
        /// reset the info to the last one
        /// </summary>
        /// <exception cref="InvalidOperationException"> there is no last info (the current one is the first one set)</exception>
        private static void resetInfo()
        {
            if (previous_info.Count == 0)
            {
                throw new InvalidOperationException("No previous Context info");
            }

            _info = previous_info.Pop();
        }

        // freeze
        /// <summary>
        /// freeze the context (cannot be changed) (&amp; thus all the existing <see cref="VarTracer"/>s)
        /// </summary>
        public static void freeze()
        {
            if (Global.getSetting("flame mode")) return;
            Debugging.assert(!_frozen);
            _frozen = true;
        }

        public static bool tryFreeze()
        {
            if (_frozen) return false;
            freeze();
            return true;
        }
        /// <summary>
        /// unfreeze the status
        /// <para/>Exception will be raised if the context is not frozen
        /// </summary>
        public static void unfreeze()
        {
            if (Global.getSetting("flame mode")) return;
            Debugging.assert(_frozen);
            _frozen = false;
        }
        /// <summary>
        /// is the context frozen ?
        /// </summary>
        public static bool isFrozen() => _frozen;

        // all
        /// <summary>
        /// Reset the Context to its previous state.
        /// Rests the <see cref="_status"/> &amp; <see cref="_info"/>
        /// </summary>
        /// <exception cref="Exception"> there is not the same amount of stacked previous status' &amp; infos</exception>
        public static void reset()
        {
            if (_frozen) return;
            if (previous_info.Count != previous_status.Count) throw new Exception("inconsistent use of reset");
            resetStatus();
            resetInfo();
        }
        /// <summary>
        /// Assert that the status is the one given as input.
        /// Does not assert if:
        /// <para/>* the Context is not <see cref="Global.settings"/>["fail on context assertions"]
        /// <para/>* the Context is <see cref="_frozen"/>
        /// </summary>
        /// <param name="supposed"> wanted status</param>
        /// <exception cref="Exception"> the status is not the input status</exception>
        public static void assertStatus(StatusEnum supposed)
        {
            if (Global.getSetting("fail on context assertions") && !_frozen && (int) supposed != _status) // not sure about not being blocked ?
            {
                throw new Exception("Context Assertion Error. Supposed: " + supposed + " but actual: " + _status);
            }
        }
        /// <summary>
        /// Reset the whole Context to zero
        /// </summary>
        public static void resetContext()
        {
            _status = (int) StatusEnum.undefined;
            _info = null;
            _frozen = false;
            previous_status.Clear();
            previous_info.Clear();
        }
    }

    /// <summary>
    /// <see cref="Debugging"/> is used for assertions and logging (Enabled with the <see cref="Global.settings"/>["debug"] parameter).
    /// </summary>
    static class Debugging
    {
        /// <summary>
        /// Same as"System.Diagnostics.Debug.Assert, but can be called anywhere.
        /// <param name="condition"> if condition is false, raises and exception</param>
        /// </summary>
        public static void assert(bool condition)
        {
            if (!condition)
            {
                StackTrace stack_trace = new StackTrace();
                string call_name = stack_trace.GetFrame(1).GetMethod().Name;
                throw new Exception(call_name + " (" + Global.current_line_index + "): Debugging.Assertion Error. CUSTOM ERROR");
            }
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
    static class Parser
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
            Dictionary<int, string> lines = new Dictionary<int, string>();
            StreamReader file = new StreamReader(path);

            string full = file.ReadToEnd();
            file.Close();

            string sl_flag = Global.single_line_comment_string; // single line comment string
            string ml_open_flag = Global.multiple_lines_comment_string; // multiple lines opening comment string
            char[] char_array = ml_open_flag.ToCharArray();
            Array.Reverse( char_array );
            string ml_close_flag = new string( char_array ); // multiple lines closing comment string

            bool in_comment = false; // are we currently in a multiple-line comment
            int last_valid_index = 0; // for adding to lines
            int remaining = full.Length + 1; // remaining chars, for SubStrings

            int line_index = 1; // start at 0, bc will be incremented before the line is appended (debut of for loop)

            string constructed_line = "";
            int ml_comment_line_index_start = -1;
            int ml_section_count = 1;

            for (int i = 0; i < full.Length; i++)
            {
                remaining--;
                bool add_eol = true;
                if (full[i] == '\n')
                {
                    if (line_index++ == ml_comment_line_index_start)
                    {
                        constructed_line += full.Substring(last_valid_index, i - last_valid_index - 1);
                        Debugging.print("adding ml comment concatenated line: \"" + constructed_line + "\"");
                        lines.Add(line_index, constructed_line);
                        constructed_line = ""; // reset constructed line
                        //i += i - last_valid_index;
                        add_eol = false;
                        last_valid_index += ml_section_count; // idk why ??
                        ml_section_count = 1;
                    }
                } // next line starts
                
                if (!in_comment)
                {
                    // multiple-line comment start
                    if (ml_open_flag.Length < remaining && full.Substring(i, ml_open_flag.Length) == ml_open_flag)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        /*if (ml_comment_line_index_start < line_index)
                        {
                            Debugging.print("adding line of code ", line_index, " : ", line_of_valid_code);
                            lines.Add(line_index, line_of_valid_code);
                            ml_comment_line_index_start = line_index;
                        }
                        else
                        {*/
                        Debugging.print("debut of line section: ", line_index, " : ", line_of_valid_code);
                        ml_section_count++;
                        constructed_line += line_of_valid_code;

                        in_comment = true;
                        Debugging.print("in multiple line comment");
                    }
                    // single line comment start
                    else if (sl_flag.Length < remaining && full.Substring(i, sl_flag.Length) == sl_flag)
                    {
                        if (!full.Substring(i).Contains("\n")) break; // EOF incoming, no more data
                        int next_line_index = full.IndexOf('\n', i) + 1;
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        line_index++; // increment index
                        Debugging.print("adding line of code ", line_index, " : ", line_of_valid_code);
                        lines.Add(line_index, line_of_valid_code);
                        last_valid_index = next_line_index; // new last_valid_index (in future)
                        remaining -= next_line_index - i; // decrement remaining
                        i = next_line_index; // updating i
                        Debugging.print("jumped to ", i);
                    }
                    // end-of-line (EOL)
                    else if (full[i] == '\n' && add_eol)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        Debugging.print("adding line of code eol ", line_index, " : ", line_of_valid_code);
                        lines.Add(line_index, line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                    }
                    // end-of-file (EOF)
                    else if (i == full.Length - 1) // in case the file just ends there (could check EOF ?)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index);
                        Debugging.print("adding last line of code ", line_index, " : ", line_of_valid_code);
                        lines.Add(line_index, line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                    }
                }
                else if (ml_close_flag.Length < remaining && full.Substring(i, ml_close_flag.Length) == ml_close_flag)
                {
                    in_comment = false;
                    last_valid_index = i + ml_close_flag.Length;
                    ml_comment_line_index_start = line_index;
                    Debugging.print("out of multiple line comment");
                }
            }

            if (in_comment) throw Global.aquilaError(); // InvalidCommentException

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
        public static Dictionary<string, string> getMacroPreprocessorValues(List<string> lines)
        {
            Dictionary<string, string> macros = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                {
                    string macro_name = line.Split(' ')[0].Substring(1);
                    string macro_value = line.Substring(macro_name.Length + 2); // [2] = [1 for '#' (start)] + [1 for ' ' (end)]
                    macros.Add(macro_name, macro_value);
                }
            }

            return macros;
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
	        Global.setSetting("debug", false);
	        Global.setSetting("trace debug", false);
            Global.setSetting("allow tracing in frozen context", true);
            Global.setSetting("flame mode", true); // can set to false bc "allow tracing in frozen context" is set to true. but to be sure: true

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

            Console.WriteLine(args.Length > 0 ? args[0] : "");

            string src_code = args.Length == 1 ? args[0] : "merge sort.aq"; // "Leibniz-Gregory PI approximation.aq" // "test.aq" // "bubble sort.aq" // "rule 110.aq";
            bool interactive_after_execution = args.Length == 0;

            Algorithm algo = Interpreter.algorithmFromSrcCode(src_code);
            try
            {
                Variable return_value = Interpreter.runAlgorithm(algo);
                Console.WriteLine("returned value: " + return_value);

                if (interactive_after_execution)
                {
                    Console.WriteLine("Interactive interpreter after execution:");
                    Interpreter.interactiveMode(exec_lines, false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Error caught. Interactive interpreter instance given for debugging.");
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
