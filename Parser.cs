using System;
using System.Collections.Generic;
using System.IO;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary> All the global variables are stored here. Global variables are the ones that should be accessible every, by everyone in the <see cref="Parser"/> program.
    /// <see cref="Global"/> is a static class. All it's attributes and methods are static too.
    /// 
    /// <para/>List of the attributes:
    /// <para/>* <see cref="Global.variables"/> : Dictionary(string, Variable)
    /// <para/>* <see cref="single_line_comment_string"/> : string
    /// <para/>* <see cref="Global.multiple_lines_comment_string"/>
    /// <para/>* <see cref="current_line_index"/> : int
    /// <para/>* <see cref="nested_instruction_flags"/> : Dictionary(string, string)
    /// <para/>* <see cref="default_values_by_var_type"/> : Dictionary(string, Variable)
    /// <para/>* <see cref="base_delimiters"/> : Dictionary(char, char)
    /// <para/>* <see cref="al_operations"/> : char[]
    /// <para/>* <see cref="debug"/> : bool
    /// </summary>
    static class Global
    {
        /// <summary>
        /// Those are all the variables that the analysed algorithm uses
        /// </summary>
        public static readonly Dictionary<string, Variable> variables = new Dictionary<string, Variable>();

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
#pragma warning disable 414
        public static readonly int current_line_index = 0; // disable compiler warning
#pragma warning restore 414

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
            {"int", new Integer(0)},
            {"list", new DynamicList()}
        };

        /// <summary>
        /// Those are the delimiters that 'cut off' arithmetic, logical or
        /// programming-related expressions. Some examples:
        ///     - "420 + (6 * 7)" -> mathematical expression. delimiter is '('
        ///     - "!(var i == 0)" -> logical expression. delimiter is '('
        ///     - "var l[50 - 6]" -> prog.-related expression. delimiter is '['
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
        /// This array is sorted by operation priority. For example, the '*'
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
        public static readonly char[] al_operations = { '^', '&', '|', ':', '~', '}', '{', '>', '<', '%', '*', '/', '+', '-' }; // '!' missing. special case

        /// <summary>
        /// If set to true, all the <see cref="Debugging.print"/> calls will output their content to the stdout
        /// </summary>
        public static bool debug = true;

        /// <summary>
        /// Temporary function that should be used as follows:<para>throw Global.NIE();</para>
        /// <para/> This should be used while there is no pseudo-code related Exception.
        /// All the <see cref="aquilaError"/> calls should be later replaced by custom <see cref="Exception"/>s.
        /// </summary>
        /// <returns> new <see cref="NotImplementedException"/>("Error occurred. Custom Exceptions not implemented")</returns>
        public static NotImplementedException aquilaError() =>
            new NotImplementedException("Error occurred. Custom Exception not implemented for this use case");
    }

    /// <summary>
    /// <see cref="Debugging"/> is used for assertions and logging (Enabled with the <see cref="Global.debug"/> variable).
    /// <para/>Note: <see cref="Global.debug"/> is defined in <see cref="Global"/>
    /// </summary>
    static class Debugging
    {
        /// <summary>
        /// Same as <see cref="System.Diagnostics.Debug"/>.Assert, but can be called anywhere.
        /// <param name="condition"> if condition is false, raises and exception</param>
        /// </summary>
        public static void assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Debugging.Assertion Error. CUSTOM ERROR");
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
            
            // debugging mode is on
            Console.Write("DEBUG: ");
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
                    else if (full[i] == '\n'){
                        lines.Add(full.Substring(last_valid_index, i - last_valid_index));
                        last_valid_index = i + 1; // + 1 : don't take the '\n' char
                        Debugging.print("adding line of code");
                    }
                }
                else if (ml_close_flag.Length < remaining && full.Substring(i, ml_close_flag.Length) == ml_close_flag)
                {
                    in_comment = false;
                    last_valid_index = i + ml_close_flag.Length;
                    Debugging.print("out of multiple line comment");
                }
            }

            if (in_comment) throw Global.aquilaError();

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
                throw Global.aquilaError();
            }

            s = s.Replace(" ", "");
            s = s.Substring(1, s.Length - 2);

            List<string> splitted = StringUtils.splitStringKeepingStructureIntegrity(s, ',', Global.base_delimiters);
            DynamicList list = new DynamicList();

            foreach (string split in splitted)
            {
                string var_type;
                if (split[0] == 91)
                {
                    var_type = "list";
                    if (split[split.Length - 1] != 93)
                    {
                        throw Global.aquilaError();
                    }
                }
                else
                {
                    var_type = "int";
                    if (split[0] < 48 || split[0] > 57)
                    {
                        throw Global.aquilaError();
                    }
                }
                list.addValue(value2Variable(var_type, split));
            }

            return list;
        }

        /// <summary>
        /// Takes an integer or list variable string and returns the corresponding <see cref="Variable"/>.
        /// </summary>
        /// <param name="var_type"> variable-type string "int", "list", etc.</param>
        /// <param name="var_value"> variable-value integer or list (for lists: <see cref="string2DynamicList"/></param>
        /// <returns> The corresponding <see cref="Variable"/></returns>
        /// <exception cref="Global.aquilaError"></exception>
        public static Variable value2Variable(string var_type, string var_value)
        {
            switch (var_type)
            {
                case "int":
                    return Expression.parse(var_value) as Integer;
                case "list":
                    return string2DynamicList(var_value);
                default:
                    throw Global.aquilaError();
            }
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

        /// <summary>
        /// Extract the return value <see cref="Expression"/> from the main algorithm/a function
        /// </summary>
        /// <param name="lines"> list of lines</param>
        /// <returns> return value <see cref="Expression"/></returns>
        /// <exception cref="Global.aquilaError"></exception>
        public static Expression getReturnValue(List<string> lines)
        {
            foreach (string line in lines)
            {
                if (line.StartsWith("#return "))
                {
                    string return_expr_string = line.Substring(8);
                    return new Expression(return_expr_string);
                }
            }

            throw Global.aquilaError();
        }
    }

    static class Program
    {
        // ReSharper disable once InconsistentNaming
        static void Main()
        {
            Global.debug = true;
            
            string path = "rule 110.aq"; // there are 3 "simple.txt" files !
            List<string> lines = Parser.readLines(path);
            Debugging.print("lines read");
            lines = StringUtils.purgeLines(lines); // same as "lines = StringUtils.purgeLines(lines);"
            Debugging.print("lines purged");

            StringUtils.printStringList(lines, true);

            // extract macros
            Dictionary<string, string> macros = Parser.getMacroPreprocessorValues(lines);

            // Parse code into RawInstructions
            List<RawInstruction> raw_instructions = RawInstruction.code2RawInstructions(lines);
            Debugging.print("raw_instructions done");
            
            // Pretty-print code
            foreach (RawInstruction instruction in raw_instructions)
            {
                if (Global.debug) instruction.prettyPrint();
            }

            // Build instructions from the RawInstructions
            List<Instruction> instructions = new List<Instruction>();
            foreach (RawInstruction raw_instruction in raw_instructions)
            {
                Instruction instruction = raw_instruction.toInstr();
                instructions.Add(instruction);
            }

            string algo_name = macros.ContainsKey("name") ? macros["name"] : "no-name-given";
            Algorithm algo = new Algorithm(algo_name, instructions);

            DynamicList l_list = new DynamicList(new List<Variable>() {new Integer(0), new Integer(1), new Integer(2), new Integer(3)});
            Global.variables.Add("i2", new Integer(-4));
            Global.variables.Add("j2", new Integer(5));
            Global.variables.Add("l2", l_list);
            
            string test = "return(var l)";
            //Console.WriteLine(Expression.parse(test).getValue());

            Variable return_value = algo.run();
            Console.WriteLine( return_value );
        }
    }
}