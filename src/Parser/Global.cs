using System;
using System.Collections.Generic;
using System.Linq;

namespace Parser
{
    

    /// <summary> All the global variables are stored here. Global variables are the ones that should be accessible every, by everyone in the <see cref="Parser"/> program.
    /// <see cref="Global"/> is a static class. All it's attributes and methods are static too.
    ///
    /// <para/>List of the attributes:
    /// <para/>* <see cref="reserved_keywords"/> : string[]
    /// <para/>* <see cref="_variable_stack"/> : Stack( (int, Dictionary(string, Variable)) )
    ///
    ///
    ///
    ///
    ///
    ///
    /// 
    /// <para/>* <see cref="usable_variables"/> : List(string)
    /// <para/>* <see cref="single_line_comment_string"/> : string
    /// <para/>* <see cref="multiple_lines_comment_string"/>
    /// <para/>* <see cref="current_line_index"/> : int
    /// <para/>* <see cref="nested_instruction_flags"/> : Dictionary(string, string)
    /// <para/>* <see cref="default_values_by_var_type"/> : Dictionary(string, Variable)
    /// <para/>* <see cref="base_delimiters"/> : Dictionary(char, char)
    /// <para/>* <see cref="al_operations"/> : char[]
    ///
    /// 
    /// <para/>* <see cref="var_tracers"/> : List(VarTracer)
    /// <para/>* <see cref="func_tracers"/> : List(FuncTracer)
    /// <para/>* <see cref="aquilaError"/> : () -> Exception
    /// <para/>* <see cref="resetEnv"/> : () -> void
    /// </summary>
    public static class Global
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
        /// Initialize the variable stack
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
            Debugging.print("new main scope depth from (current): ", getMainScopeDepth());
            addVariableStackLayer(); // defaults are added automatically
            Debugging.print("new main scope depth: " + getMainScopeDepth());
        }

        /// <summary>
        /// Removes the last added variable stack layer.
        /// This is called when exiting, for example, a function call
        /// </summary>
        public static void resetMainContextScope()
        {
            Debugging.print("exiting main scope depth (current): " + getMainScopeDepth());
            Debugging.assert(getMainScopeDepth() > 0); // cannot exit a function call or something in the main instruction loop
            removeVariableStackLayer();
            Debugging.print("new main scope depth: " + getMainScopeDepth());
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
            Debugging.print("new local scope depth from (current): ", getLocalScopeDepth());
            addVariableStackElement(vars);
            Debugging.print("new local scope depth: " + getLocalScopeDepth());
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

        /// <summary>
        /// Get a <see cref="Variable"/> from its name
        /// </summary>
        /// <param name="name"> Name of the <see cref="Variable"/></param>
        /// <returns> The actual <see cref="Variable"/></returns>
        /// <exception cref="AquilaExceptions.NameError"> The variable name does not exist in the current Context</exception>
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

            throw new AquilaExceptions.NameError($"Variable name \"{name}\" does not exist in the current Context"); // variable does not exist
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
        // ReSharper disable once UnassignedField.Global
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
        /// <summary>
        /// List of parameters that exist & that could be tweaked. Some combinations will break everything.
        /// Please read their description in the source code
        /// </summary>
	    private static readonly Dictionary<string, bool> settings = new Dictionary<string, bool>
	    { // These parameters default values should be set to work with the Graphics & Animation part
		    {"interactive", false},                             // get interactive interpreter on run
		    {"debug", false},                                   // enable debugging
		    {"trace debug", false},                             // enable tracing debugging
	        {"translator debug", false},			            // enable translator debugging
		    {"fail on context assertions", false},              // throw an Exception on Context.assertStatus fails
		    {"check function existence before runtime", false}, // Trust the user for defining used functions later in the code /!\ disables recursive functions if set to true /!\
            {"lazy logic", true},                               // enable lazy logic operations
            {"allow tracing in frozen context", false},         // trace even if the Context is frozen
            {"permafrost", false},                              // freeze Context permanently
            {"flame mode", false},                              // disables Context freezing completely
            {"implicit declaration in assignment", false},      // enable implicit declaration in assignment
	    };

        /// <summary>
        /// Set a <see cref="settings"/> key to a certain value
        /// </summary>
        /// <param name="key"> key</param>
        /// <param name="value"> value</param>
        /// <exception cref="KeyNotFoundException"> The key does not exist in the <see cref="settings"/> dict. You cannot invent settings !</exception>
        public static void setSetting(string key, bool value)
        {
            if (!settings.ContainsKey(key)) throw new KeyNotFoundException($"Setting: {key} not found.");
            settings[key] = value;
        }

        /// <summary>
        /// Get the value of a setting by its key
        /// </summary>
        /// <param name="key"> key</param>
        /// <returns> Return the value of the setting</returns>
        public static bool getSetting(string key) => settings[key];
    }
}