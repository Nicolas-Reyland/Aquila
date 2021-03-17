using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        /// You can disable the Context. If you do so, it will still update itself, but all
        /// checks concerning the context (<see cref="assertStatus"/>) will not execute
        /// </summary>
        public static bool enabled = true;
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

        /// <summary>
        /// The last local 'main' scope. The main scope would be defined at the beginning of your program
        /// or at every function call. This is updated every time the 'accessible variables' list changes
        /// </summary>
        public static int last_main_scope_depth;

        // status
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> current status</returns>
        public static int getStatus() => _status;
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
            Debugging.assert(!_frozen);
            _frozen = true;
        }
        /// <summary>
        /// unfreeze the status
        /// <para/>Exception will be raised if the context is not frozen
        /// </summary>
        public static void unfreeze()
        {
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
        /// <para/>* the Context is not <see cref="enabled"/>
        /// <para/>* the Context is <see cref="_frozen"/>
        /// </summary>
        /// <param name="supposed"> wanted status</param>
        /// <exception cref="Exception"> the status is not the input status</exception>
        public static void assertStatus(StatusEnum supposed)
        {
            if (enabled && !_frozen && (int) supposed != _status) // not sure about not being blocked ?
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

    /// <summary> All the global variables are stored here. Global variables are the ones that should be accessible every, by everyone in the <see cref="Parser"/> program.
    /// <see cref="Global"/> is a static class. All it's attributes and methods are static too.
    ///
    /// <para/>List of the attributes:
    /// <para/>* <see cref="reserved_keywords"/> : string[]
    /// <para/>* <see cref="_variable_stack"/> : Stack( (int, Dictionary(string, Variable)) )
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
    /// <para/>* <see cref="var_tracers"/> : List(VarTracer)
    /// <para/>* <see cref="func_tracers"/> : List(FuncTracer)
    /// <para/>* <see cref="aquilaError"/> : () -> Exception
    /// <para/>* <see cref="resetEnv"/> : () -> void
    /// </summary>
    static class Global
    {
        /// <summary>
        /// All the reserved keywords
        /// </summary>
        public static readonly string[] reserved_keywords = new string[] {
            "if", "else", "end-if",
            "for","end-for",
            "while", "end-while",
            "function", "end-function", "recursive",
            "decl", "safe", "overwrite",
            "overwrite", // not yet
            "safe", // not yet
            "trace",
            "null", "auto", "int", "float", "bool", "list",
        };

        /// <summary>
        /// All default available variables in Aquila
        /// </summary>
        private static readonly Dictionary<string, Variable> default_variables = new Dictionary<string, Variable>
            {{"null", new NullVar()}};

        /// <summary>
        /// The current variable stack.
        /// </summary>
        private static Stack<List<Dictionary<string, Variable>>> _variable_stack;

        /// <summary>
        /// Initialize the variable stack.
        /// </summary>
        public static void initVariables()
        {
            _variable_stack = new Stack<List<Dictionary<string, Variable>>>();
            addVariableStackLayer();
        }

        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> main context scope depth</returns>
        public static int getMainScopeDepth() => _variable_stack.Count;

        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> local context scope depth</returns>
        public static int getLocalScopeDepth() => _variable_stack.Peek().Count;
        
        /// <summary>
        /// Add a new layer to the variable stack
        /// </summary>
        private static void addVariableStackLayer() => _variable_stack.Push(new List<Dictionary<string, Variable>>
            {new Dictionary<string, Variable>(default_variables)});

        /// <summary>
        /// Remove the last variable stack layer
        /// </summary>
        private static void removeVariableStackLayer() => _variable_stack.Pop();

        /// <summary>
        /// Enter a new variable stack layer.
        /// This is called when entering, for example, a function call
        /// </summary>
        public static void newMainContextScope()
        {
            Debugging.print("new main scope from (current): ", getMainScopeDepth());
            addVariableStackLayer();
            addVariableStackElement(default_variables); // initialize default variables
            Debugging.print("new main scope: " + getMainScopeDepth());
        }

        /// <summary>
        /// Removes the last added variable stack layer.
        /// This is called when exiting, for example, a function call
        /// </summary>
        public static void resetMainContextScope()
        {
            Debugging.print("exiting main scope (current): " + getMainScopeDepth());
            Debugging.assert(getMainScopeDepth() > 0); // cannot exit a function call or something in the main instruction loop
            removeVariableStackLayer();
            Debugging.print("new main scope: " + getMainScopeDepth());
        }

        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> list in the stack (head)</returns>
        public static List<Dictionary<string, Variable>> getCurrentDictList() => _variable_stack.Peek();
        
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> last element of the list in the stack (head)</returns>
        public static Dictionary<string, Variable> getCurrentDict() =>
            _variable_stack.Peek()[_variable_stack.Peek().Count - 1];

        /// <summary>
        /// Add a new local scope variable dict
        /// </summary>
        /// <param name="vars"> variable to use as new dict (empty dict if null)</param>
        private static void addVariableStackElement(Dictionary<string, Variable> vars) =>
            _variable_stack.Peek().Add(vars ?? new Dictionary<string, Variable>());

        /// <summary>
        /// Remove the last local scope variable dict
        /// </summary>
        private static void removeVariableStackElement() =>
            _variable_stack.Peek().Remove(getCurrentDict()); // last element index
        
        /// <summary>
        /// Add a new local context scope variable dict.
        /// This is called when entering a nested instruction
        /// </summary>
        public static void newLocalContextScope(Dictionary<string, Variable> vars = null)
        {
            Debugging.print("new local scope from (current): ", getLocalScopeDepth());
            addVariableStackElement(vars);
            Debugging.print("new local scope: " + getLocalScopeDepth());
        }

        /// <summary>
        /// Remove the last local context scope variable dict.
        /// This is called when exiting a nested instruction
        /// </summary>
        public static void resetLocalContextScope()
        {
            Debugging.print("exiting local scope (current): ", getLocalScopeDepth());
            Debugging.assert(getLocalScopeDepth() > 0);
            removeVariableStackElement();
            Debugging.print("new local scope: " + getLocalScopeDepth());
        }

        public static Variable variableFromName(string name)
        {
            int local_scope_depth = getLocalScopeDepth();
            // reverse search (from last local scope to the oldest)
            for (int i = local_scope_depth - 1; i >= 0; i--)
            {
                Dictionary<string, Variable> local_var_dict = _variable_stack.Peek()[i];
                if (local_var_dict.ContainsKey(name))
                {
                    return local_var_dict[name];
                }
            }

            throw aquilaError("Variable name \"" + name + "\" does not exist"); // variable does not exist
        }

        /// <summary>
        /// Does the variable exist in the current variable scope ?
        /// </summary>
        /// <param name="name"> variable name</param>
        /// <returns> var name in any of the dictionaries ?</returns>
        public static bool variableExistsInCurrentScope(string name) =>
            _variable_stack.Peek().Any(variables => variables.ContainsKey(name));

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
            {"if", "end-if"},
            {"function", "end-function"},
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
        public static readonly char[] al_operations = { '&', '^', '|', ':', '~', '}', '{', '>', '<', '-', '+', '/', '*', '%'};//, '<', '>', '{', '}', '~', ':', '|', '&', '^' }; // '!' missing. special case
        // real priority order: { '&', '^', '|', ':', '~', '}', '{', '>', '<', '%', '*', '/', '+', '-' };

        /// <summary>
        /// If set to true, all the <see cref="Debugging.print"/> calls will output their content to the default stdout
        /// </summary>
        public static bool debug = true;

        /// <summary>
        /// If set to true, all the <see cref="Tracer.printTrace"/> calls will output their content to the default stdout
        /// </summary>
        public static bool trace_debug = true;

        /// <summary>
        /// Should function names be checked before or during runtime ?
        /// </summary>
        public static bool check_function_names_before_runtime = false;

        /// <summary>
        /// List of all the variable tracers
        /// </summary>
        public static readonly List<VarTracer> var_tracers = new List<VarTracer>();

        /// <summary>
        /// List of all the functions tracers
        /// </summary>
        public static readonly List<FuncTracer> func_tracers = new List<FuncTracer>();

        /// <summary>
        /// Function called on every tracer change
        /// </summary>
#pragma warning disable 649
        public static Func<Alteration, bool> graphical_function; // example: new Func<Alteration, bool>(graphicalFunction)
#pragma warning restore 649

        /// <summary>
        /// Temporary function that should be used as follows:<para>throw Global.NIE();</para>
        /// <para/> This should be used while there is no pseudo-code related Exception.
        /// All the <see cref="aquilaError"/> calls should be later replaced by custom <see cref="Exception"/>s.
        /// </summary>
        /// <returns> new <see cref="NotImplementedException"/>("Error occurred. Custom Exceptions not implemented")</returns>
        public static Exception aquilaError(string message = "") =>
            new Exception("Error occurred. " + (message == "" ? "Custom Exception not implemented for this use case" : message));

        /// <summary>
        /// Reset the current Environment to zero
        /// </summary>
        public static void resetEnv()
        {
            initVariables();
            Functions.user_functions.Clear();
            var_tracers.Clear();
            func_tracers.Clear();
            usable_variables.Clear();
            Context.resetContext();
        }
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
                StackTrace stack_trace = new StackTrace();
                string call_name = stack_trace.GetFrame(1).GetMethod().Name;
                throw new Exception(call_name + " (" + Global.current_line_index + "): Debugging.Assertion Error. CUSTOM ERROR");
            }
        }

        /// <summary>
        /// Outputs the args to the stdout stream if <see cref="Global.debug"/> is set to true
        /// </summary>
        /// <param name="args"> consecutive calls of the ".ToString()" method of these will be printed</param>
        public static void print(params object[] args)
        {
            // if not in debugging mode, return
            if (!Global.debug) return;

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

            int line_index = 0; // start at 0, bc will be incremented before the line is appended (debut of for loop)

            for (int i = 0; i < full.Length; i++)
            {
                remaining--;
                if (full[i] == '\n') line_index++; // next line starts

                if (!in_comment)
                {
                    // multiple-line comment start
                    if (ml_open_flag.Length < remaining && full.Substring(i, ml_open_flag.Length) == ml_open_flag)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        Debugging.print("adding line of code ", line_index, " : ", line_of_valid_code);
                        lines.Add(line_index, line_of_valid_code);
                        in_comment = true;
                        Debugging.print("in multiple line comment");
                    }
                    // single line comment start
                    else if (sl_flag.Length < remaining && full.Substring(i, sl_flag.Length) == sl_flag)
                    {
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
                    else if (full[i] == '\n')
                    {
                        string line_of_valid_code = full.Substring(last_valid_index, i - last_valid_index);
                        lines.Add(line_index, line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                        Debugging.print("adding line of code ", line_index, " : ", line_of_valid_code);
                    }
                    // end-of-file (EOF)
                    else if (i == full.Length - 1) // in case the file just ends there (could check EOF ?)
                    {
                        string line_of_valid_code = full.Substring(last_valid_index);
                        lines.Add(line_index, line_of_valid_code);
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                        Debugging.print("adding last line of code ", line_index, " : ", line_of_valid_code);
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
            
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            bool interactive = true;
            Global.debug = true;
            Global.trace_debug = false;
            Context.enabled = true;

            Global.func_tracers.Add(new FuncTracer("list_at"));
            Global.func_tracers.Add(new FuncTracer("swap"));

            if (args.Length > 0) interactive = false; // for Atom running!

            if (interactive || args.Length > 0 && args[0] == "interactive")
            {
                List<string> exec_lines = new List<string>()
                {
                    "function recursive auto test(n)",
                    "if ($n { 0)",
                    "print_str_endl(yes!)",
                    "return(0)",
                    "end-if",
                    "print($n)",
                    "print_endl()",
                    "return(test($n - 1) + test($n - 2))",
                    "end-function",
                    "eval test(2)"
                };
                Interpreter.interactiveMode(exec_lines);
                return;
            }

            Console.WriteLine(args.Length > 0 ? args[0] : "");

            string src_code = args.Length == 1 ? args[0] : "merge sort.aq"; // "Leibniz-Gregory PI approximation.aq" // "test.aq" // "bubble sort.aq" // "rule 110.aq";
            bool interpreter_after = false;

            Algorithm algo = Interpreter.algorithmFromSrcCode(src_code);
            Variable return_value = Interpreter.runAlgorithm(algo);

            Console.WriteLine("returned value: " + return_value);

            if (interpreter_after) Interpreter.interactiveMode();

            // ReSharper restore HeuristicUnreachableCode
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore RedundantAssignment
        }
    }
}
