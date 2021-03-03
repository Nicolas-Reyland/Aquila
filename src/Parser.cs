using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// 
    /// </summary>
    public static class Context
    {
        /// <summary>
        /// Status list:
        /// <para/>* -1: undefined
        /// <para/>*  0: reading &amp; purging code
        /// <para/>*  1: processing macros
        /// <para/>*  2: parsing code
        /// <para/>*  3: building Instructions
        /// <para/>*  4: running code
        /// <para/>*  5: executing an instruction
        /// <para/>*  6: evaluating a value function
        /// <para/>*  7: executing a void function
        /// <para/>*  8: custom status state used by <see cref="FuncTracer"/> for <see cref="VarTracer"/> stack squeezing
        /// <para/>* 10: executing a declaration
        /// <para/>* 11: executing an assignment
        /// <para/>* 99: running finished
        /// </summary>
        private static int _status = -1;
        private static readonly Stack<int> previous_status = new Stack<int>();
        private static object _info = null;
        private static readonly Stack<object> previous_info = new Stack<object>();
        public static bool enabled = true;

        // status
        public static int getStatus() => _status;

        public static void setStatus(int new_status)
        {
            previous_status.Push(_status);
            _status = new_status;
        }

        public static void resetStatus()
        {
            if (previous_status.Count == 0)
            {
                throw new InvalidOperationException("No previous Context state");
            }

            _status = previous_status.Pop();
        }

        // info
        public static dynamic getInfo() => _info;

        public static void setInfo(object new_info)
        {
            previous_info.Push(_info);
            _info = new_info;
        }

        public static void resetInfo()
        {
            if (previous_info.Count == 0)
            {
                throw new InvalidOperationException("No previous Context info");
            }

            _info = previous_info.Pop();
        }
        
        // all
        public static void reset()
        {
            resetStatus();
            resetInfo();
        }

        public static void assertStatus(int supposed)
        {
            if (enabled && supposed != _status)
            {
                throw new Exception("Context Assertion Error. Supposed: " + supposed + " but actual: " + _status);
            }
        }
    }
    
    /// <summary> All the global variables are stored here. Global variables are the ones that should be accessible every, by everyone in the <see cref="Parser"/> program.
    /// <see cref="Global"/> is a static class. All it's attributes and methods are static too.
    /// 
    /// <para/>List of the attributes:
    /// <para/>* <see cref="variables"/> : Dictionary(string, Variable)
    /// <para/>* <see cref="usable_variables"/> : List(string)
    /// <para/>* <see cref="single_line_comment_string"/> : string
    /// <para/>* <see cref="multiple_lines_comment_string"/>
    /// <para/>* <see cref="current_line_index"/> : int
    /// <para/>* <see cref="nested_instruction_flags"/> : Dictionary(string, string)
    /// <para/>* <see cref="default_values_by_var_type"/> : Dictionary(string, Variable)
    /// <para/>* <see cref="base_delimiters"/> : Dictionary(char, char)
    /// <para/>* <see cref="al_operations"/> : char[]
    /// <para/>* <see cref="trace_debug"/> : bool
    /// <para/>* <see cref="debug"/> : bool
    /// <para/>* <see cref="aquilaError"/> : () -> Exception
    /// <para/>* <see cref="var_tracers"/> : List(VarTracer)
    /// <para/>* <see cref="func_tracers"/> : List(FuncTracer)
    /// <para/>* <see cref="nullEvent"/> : () -> Event
    /// </summary>
    static class Global
    {
        /// <summary>
        /// Those are all the variables that the analysed algorithm uses
        /// </summary>
        public static readonly Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
        
        /// <summary>
        /// All the variables that are not NullVar (thus have a graphical representable value)
        /// </summary>
        public static readonly List<string> usable_variables = new List<string>(); // doesn't support delete_var

        /// <summary>
        /// The <see cref="single_line_comment_string"/> is the string that is prefix to all
        /// the one-line comments
        /// </summary>
        public static readonly string single_line_comment_string = "//";

        /// <summary>
        /// The <see cref="multiple_lines_comment_string"/> is the string that "opens"
        /// the comment-mode. It's mirror "closes" this mode
        /// </summary>
        public static readonly string multiple_lines_comment_string = "/**"; // mirror is closing tag
        
        /// <summary>
        /// When executing the <see cref="Algorithm"/>, this int will be increased as the execution goes line to line.
        /// It is used to know what line is being executed at a given moment in time.
        /// It is really useful for <see cref="Debugging"/> and generally tracking the algorithm state
        /// </summary>
        public static int current_line_index = 0; // disable compiler warning

        /// <summary>
        /// All the nested instructions (for loops, if statements, etc.) have
        /// open-calls (e.g. "for ") and end-calls (e.g. "end-for"). This (string, string) Dictionary
        /// holds the open-calls as key and their corresponding end-call as values
        /// </summary>
        public static readonly Dictionary<string,string> nested_instruction_flags = new Dictionary<string, string>()
        {
            {"for", "end-for"},
            {"while", "end-while"},
            {"if", "end-if"}
        };

        /// <summary>
        /// When declaring a variable, it has to get a value. So a default
        /// value is assigned to the <see cref="Variable"/> internally. You will not be able
        /// to interact with the variable normally. You have to assign it a
        /// 'real' value first. This dictionary's keys are the variable
        /// types, as strings, (e.g. "int"). The values are the default values
        /// </summary>
        public static readonly Dictionary<string, dynamic> default_values_by_var_type = new Dictionary<string, dynamic>()
        {
            {"int", new Expression("0")},
            {"list", new Expression("[]")},
            {"bool", new Expression("false")},
            {"float", new Expression("0f")},
        };

        /// <summary>
        /// Those are the delimiters that 'cut off' arithmetic, logical or
        /// programming-related expressions. Some examples:
        ///     - "420 + (6 * 7)" -> mathematical expression. delimiter is '('
        ///     - "!($i == 0)" -> logical expression. delimiter is '('
        ///     - "$l[50 - 6]" -> prog.-related expression. delimiter is '['
        /// The keys are the opening-delimiters (e.g. '[') and the values are
        /// the closing delimiters (e.g. ']').
        /// </summary>
        public static readonly Dictionary<char, char> base_delimiters = new Dictionary<char, char>()
        {
            {'[', ']'},
            {'(', ')'}
        };

        /// <summary>
        /// "AL_OPERATIONS" stands for "Arithmetical and Logical Operations".
        /// This array is sorted by operation priority (reversed). For example, the '*'
        /// operation has a higher priority than the '+' operation.
        /// The same goes for '&amp;' (logic) and '%' (arithmetic).
        /// <para/> Here are unusual characters representations:
        /// <para/>* ':' is "!="
        /// <para/>* '~' is "=="
        /// <para/>* '&amp;' is "&amp;&amp;"
        /// <para/>* '|' is "||"
        /// <para/>* '}' is "&gt;="
        /// <para/>* '{' is "&lt;="
        /// </summary>
        public static readonly char[] al_operations = { '^', '&', '|', ':', '~', '}', '{', '>', '<', '-', '+', '/', '*', '%'};//, '<', '>', '{', '}', '~', ':', '|', '&', '^' }; // '!' missing. special case
        // real priority order: { '^', '&', '|', ':', '~', '}', '{', '>', '<', '%', '*', '/', '+', '-' };

        /// <summary>
        /// If set to true, all the <see cref="Debugging.print"/> calls will output their content to the default stdout
        /// </summary>
        public static bool debug = true;

        /// <summary>
        /// If set to true, all the <see cref="Tracer.printTrace"/> calls will output their content to the default stdout
        /// </summary>
        public static bool trace_debug = true;

        /// <summary>
        /// Temporary function that should be used as follows:<para>throw Global.NIE();</para>
        /// <para/> This should be used while there is no pseudo-code related Exception.
        /// All the <see cref="aquilaError"/> calls should be later replaced by custom <see cref="Exception"/>s.
        /// </summary>
        /// <returns> new <see cref="NotImplementedException"/>("Error occurred. Custom Exceptions not implemented")</returns>
        public static Exception aquilaError() =>
            new Exception("Error occurred. Custom Exception not implemented for this use case");

        /// <summary>
        /// List of all the variable tracers
        /// </summary>
        public static List<VarTracer> var_tracers = new List<VarTracer>();

        /// <summary>
        /// List of all the functions tracers
        /// </summary>
        public static readonly List<FuncTracer> func_tracers = new List<FuncTracer>();
        
        /// <summary>
        /// null event generator
        /// </summary>
        /// <returns> null event</returns>
        public static Event nullEvent()
        {
            Event null_event = new Event(null) {status = -1, info = null}; //! Unity compatible ?
            return null_event;
        }

        /// <summary>
        /// Function called on every tracer change
        /// </summary>
        public static Func<Alteration, bool> graphical_function; // example: new Func<Alteration, bool>(graphicalFunction)
    }

    /// <summary>
    /// <see cref="Debugging"/> is used for assertions and logging (Enabled with the <see cref="Global.debug"/> variable).
    /// <para/>Note: <see cref="Global.debug"/> is defined in <see cref="Global"/>
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
                StackTrace stackTrace = new StackTrace();
                string call_name = stackTrace.GetFrame(1).GetMethod().Name;
                throw new Exception(call_name + " (" + Global.current_line_index.ToString() + "): Debugging.Assertion Error. CUSTOM ERROR");
            }
        }

        /// <summary>
        /// Outputs the args to the stdout stream if <see cref="Global.debug"/> is set to true
        /// </summary>
        /// <param name="args"> consecutive calls of the ".ToString()" method of these will be printed</param>
        public static void print(params dynamic[] args)
        {
            // if not in debugging mode, return
            if (!Global.debug) { return; }

            int max_call_name_length = 30;
            StackTrace stackTrace = new StackTrace();
            string call_name = stackTrace.GetFrame(1) == null ? "? stackTrace == null ?" : stackTrace.GetFrame(1).GetMethod().Name;
            int missing_spaces = max_call_name_length - call_name.Length - Global.current_line_index.ToString().Length - 2; // 2: parentheses
            
            // debugging mode is on
            Console.Write("DEBUG " + call_name + "(" + Global.current_line_index.ToString() + ")");
            for (int i = 0; i < missing_spaces; i++) { Console.Write(" "); }
            
            Console.Write(" : ");
            foreach (dynamic arg in args)
            {
                Console.Write(arg.ToString());
            }
            Console.WriteLine();
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
        public static List<string> readLines(string path)
        {
            List<string> lines = new List<string>();
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

            for (int i = 0; i < full.Length; i++)
            {
                remaining--;
                if (!in_comment)
                {
                    if (ml_open_flag.Length < remaining && full.Substring(i, ml_open_flag.Length) == ml_open_flag)
                    {
                        lines.Add(full.Substring(last_valid_index, i - last_valid_index));
                        in_comment = true;
                        Debugging.print("in multiple line comment");
                    }
                    else if (sl_flag.Length < remaining && full.Substring(i, sl_flag.Length) == sl_flag)
                    {
                        int next_line_index = full.IndexOf('\n', i);
                        lines.Add(full.Substring(last_valid_index, i - last_valid_index));
                        last_valid_index = next_line_index; // new last_valid_index (in future)
                        remaining -= next_line_index - i; // decrement remaining
                        i = next_line_index; // updating i
                        Debugging.print("jumped to ", i);
                    }
                    else if (full[i] == '\n')
                    {
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        lines.Add(line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                        Debugging.print("adding line of code ", lines.Count, " : ", line_of_valid_code);
                    }
                    else if (i == full.Length - 1) // in case the file just ends there (could check EOF ?)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index);
                        lines.Add(line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                        Debugging.print("adding last line of code ", lines.Count, " : ", line_of_valid_code);
                    }
                }
                else if (ml_close_flag.Length < remaining && full.Substring(i, ml_close_flag.Length) == ml_close_flag)
                {
                    in_comment = false;
                    last_valid_index = i + ml_close_flag.Length;
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
        /// Extract all the macro preprocessor values from an Acquila algorithm.
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

    static class Program
    {
        // ReSharper disable once InconsistentNaming
        static void Main(string[] args)
        {
            bool interactive = true;
            Global.debug = false;
            Global.trace_debug = false;

            Global.func_tracers.Add(new FuncTracer("list_at", new []{ 0 }, new []{ 0 }));
            Global.func_tracers.Add(new FuncTracer("swap", new []{ 0 }, new []{ 4 }));

            if (interactive || args.Length > 0 && args[0] == "interactive")
            {
                List<string> exec_lines = new List<string>()
                {
                    "declare float PI 4f",
                    "declare float n -1f",
                    "declare float d 3f",
                    "declare num_iterations 1000",
                    "for (declare i 0, $i < $num_iterations, $i = $i + 1)",
                    "$PI = $PI + $n * (4f / $d)",
                    "$d = $d + 2f",
                    "$n = $n * (-1f)",
                    "print($PI)",
                    "print_endl()",
                    "end-for"
                };
                Interpreter.interactiveMode(exec_lines);
                return;
            }
            
            Console.WriteLine(args.Length > 0 ? args[0] : "");

            string src_code = args.Length == 1 ? args[0] : "test.aq"; // "Leibniz-Gregory PI approximation.aq" // "test.aq" // "bubble sort.aq" // "rule 110.aq";

            Algorithm algo = Interpreter.algorithmFromSrcCode(src_code);

            /*DynamicList l_list = new DynamicList(new List<Variable>() {new Integer(0), new Integer(1), new Integer(2), new Integer(3)});
            Global.variables.Add("i2", new Integer(-4));
            Global.variables.Add("j2", new Integer(5));
            Global.variables.Add("l2", l_list);*/
            
            //Global.debug = true;
            
            /*const string INSTR1_STR = "declare l4 [1, 2, 3, 4, 0]";
            const string INSTR2_STR = "declare v []";
            Instruction instr1 = new RawInstruction(INSTR1_STR).toInstr();
            Instruction instr2 = new RawInstruction(INSTR2_STR).toInstr();
            //Console.WriteLine(Expression.parse(test).getValue());
            instr1.execute();
            instr2.execute();
            Console.WriteLine(Global.variables["v"]);*/
            
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            Variable return_value = Interpreter.runAlgorithm(algo);

            //stopwatch.Stop();
            Console.WriteLine(return_value);
            
            /*Console.WriteLine("Value Stack:");
            Global.var_tracers[0].printValueStack();
            Console.WriteLine("Event Stack:");
            Global.var_tracers[0].printEventStack();
            
            //Console.WriteLine(Global.func_tracers[0]._events.Count);
            
            Console.WriteLine("\nlist_at Value Stack:");
            Global.func_tracers[0].printValueStack();
            Console.WriteLine("list_at Event Stack:");
            Global.func_tracers[0].printEventStack();*/

            //Console.WriteLine("Time: {0} ms", stopwatch.Elapsed.Milliseconds);
        }
    }
}