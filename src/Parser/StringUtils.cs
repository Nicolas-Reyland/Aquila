using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// All the string parsing, comparisons, splitting, reunification, etc. needed for a project like this.
    /// The function names are relatively self-explanatory, but you are welcome to read the documentation anyways.
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Print a List&lt;string&gt;. One line per string
        /// </summary>
        /// <param name="list"> list of strings</param>
        /// <param name="print_index"> false by default. if true, index of line (starting 0) will be printed as prefix</param>
        public static void printStringList(IEnumerable<string> list, bool print_index = false)
        {
            short index = 1;
            foreach (string line in list)
            {
                if (print_index) Global.stdoutWrite($"{index++} ");
                Global.stdoutWriteLine(line);
            }
        }

        /// <summary>
        /// Remove all the unnecessary whitespaces from the input. The only whitespace in the output are spaces.
        /// For an input that looks like this: " word_1  word_2\t ... word_n \t ", the output will look something like this: "word_1 word_2 ... word_n"
        /// </summary>
        /// <param name="line"> a string containing words and whitespaces</param>
        /// <returns> normalized string</returns>
        public static string normalizeWhiteSpaces(string line)
        {
            // replace all tabs with spaces
            string new_line = line.Replace("\t", " ");
            // replace all duplicates of spaces with single spaces
            new_line = Regex.Replace(new_line, @"\s+", " ");
            // remove spaces at the beginning or closing of the line
            if (new_line == " ") return "";
            if (new_line.Length != 0)
            {
                if (new_line[0] == 32)
                {
                    new_line = new_line.Remove(0, 1);
                }
                // ReSharper disable once UseIndexFromEndExpression
                if (new_line[new_line.Length - 1] == 32)
                {
                    new_line = new_line.Remove(new_line.Length - 1);
                }
            }

            return new_line;
        }

        /// <summary>
        /// Same as <see cref="normalizeWhiteSpaces"/>, but one a List of lines. If the input lines contain a line only
        /// containing spaces/tabs or a line that is simply empty, it will not be included in the
        /// returned list.
        /// </summary>
        /// <param name="lines"> list of lines</param>
        /// <returns> List of purged lines</returns>
        public static List<string> purgeLines(List<string> lines)
        {
            List<string> new_lines = new List<string>();
            foreach (string line in lines)
            {
                string new_line = normalizeWhiteSpaces(line);

                // if the line has content, add it
                if (new_line != "")
                {
                    new_lines.Add(new_line);
                }
            }

            return new_lines;
        }

        /// <summary>
        /// Same as <see cref="purgeLines(System.Collections.Generic.List{string})"/>, and keeps the keys.
        /// </summary>
        /// <param name="lines"> dict, values must be the lines to normalize</param>
        /// <returns> normalized dict</returns>
        public static Dictionary<int, string> purgeLines(Dictionary<int, string> lines)
        {
            Dictionary<int, string> new_lines = new Dictionary<int, string>();
            foreach (var pair in lines)
            {
                string new_line = normalizeWhiteSpaces(pair.Value);

                // if the line has content, add it
                if (new_line != "")
                {
                    new_lines.Add(pair.Key, new_line);
                }
            }

            return new_lines;
        }

        /// <summary>
        /// Remove the comments from a string
        /// </summary>
        /// <param name="source_code"> String from which the comments will be filtered out</param>
        /// <returns> Filtered string</returns>
        /// <exception cref="Exception"> A comment tag doesn't end</exception>
        public static string removeComments(string source_code)
        {
            //! instead of using SubString everywhere, work with a (one or more) string(s) and modify the chars at each iteration (or an array of chars)
            
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

            for (int i = 0; i < source_code.Length; i++)
            {
                char c = source_code[i];
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
                    if (i >= sl_flag.Length && source_code.Substring(i - sl_flag.Length, sl_flag.Length) == sl_flag)
                    {
                        filtered = filtered.Substring(0, filtered.Length - sl_flag.Length);
                        in_comment = 1;
                    }
                    // multiple lines comment
                    else if (i >= ml_open_flag.Length && source_code.Substring(i - ml_open_flag.Length, ml_open_flag.Length) == ml_open_flag)
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
                    if (i >= ml_close_flag.Length && source_code.Substring(i - ml_close_flag.Length, ml_close_flag.Length) == ml_close_flag)
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

            // "**/" is the last substring of the whole source code
            if (source_code.Length >= ml_close_flag.Length &&
                source_code.Substring(source_code.Length - ml_close_flag.Length, ml_close_flag.Length) == ml_close_flag)
            {
                in_comment = 0;
            }
            // the single line comment is allowed, because everything on the same line after the original tag should get filtered out
            if (in_comment > 1) throw new AquilaExceptions.SyntaxExceptions.UnclosedTagError("Unclosed multiple lines comment tag");

            return filtered;
        }

        /// <summary>
        /// Find the corresponding char to an opening and closing char.
        /// <para/>Example:
        /// <para/>* s = "4 + (5 - 1) * 2"
        /// <para/>* start_index = 0
        /// <para/>* opening = '('
        /// <para/>* closing = ')'
        /// <para/>* returns: 11 ( index of first ')' )
        /// </summary>
        /// <param name="s"> string that is searched through</param>
        /// <param name="start_index"> will be looking starting with this index in the string</param>
        /// <param name="opening"> opening char</param>
        /// <param name="closing"> closing char</param>
        /// <returns> index of corresponding (closing) char</returns>
        /// <exception cref="AquilaExceptions.SyntaxExceptions.UnclosedTagError"> Unclosed tag in expression</exception>
        private static int findCorrespondingElementIndex(string s, int start_index, char opening, char closing)
        {
            int counter = 0;
            for (int index = start_index; index < s.Length; index++)
            {
                if (s[index] == closing)
                {
                    counter--;

                    if (counter == 0)
                    {
                        return index + 1;
                    }
                }

                if (s[index] == opening)
                {
                    counter++;
                }
            }

            Debugging.print("entry: \"" + s + "\" & counter: {0} & start_index: {1} & s.Length: {2}", counter, start_index, s.Length);

            throw new AquilaExceptions.SyntaxExceptions.UnclosedTagError($"Unclosed tag in \"{s.Substring(start_index)}\""); // no corresponding closing char: syntax
        }

        /// <summary>
        /// Find the corresponding char to an opening and closing string.
        /// It works exactly as the <see cref="findCorrespondingElementIndex(string,int,char,char)"/>, but we are looking for
        /// strings starting with the opening and ending with closing.
        /// <para/>Example:
        /// <para/>* lines = ["$a = 0", "for ($i, 0, 5, 1)", "if ($i == $j)", "a++", "end-if", "end-for"]
        /// <para/>* start_index = 1
        /// <para/>* opening = "if "
        /// <para/>* closing = "end-if"
        /// <para/>* returns: 4
        /// </summary>
        /// <param name="lines"> List of strings</param>
        /// <param name="start_index"> will be looking starting with this index in the list</param>
        /// <param name="opening"> opening string</param>
        /// <param name="closing"> closing string</param>
        /// <returns> index of the corresponding (closing) string</returns>
        /// <exception cref="AquilaExceptions.SyntaxExceptions.UnclosedTagError"> Tag was not closed</exception>
        public static int findCorrespondingElementIndex(List<string> lines, int start_index, string opening, string closing)
        {
            int counter = 0;
            for (int index = start_index; index < lines.Count; index++)
            {
                if (lines.ElementAt(index).StartsWith(closing))
                {
                    if (counter == 0)
                    {
                        return index;
                    }

                    counter--;
                }

                if (lines.ElementAt(index).StartsWith(opening))
                {
                    counter++;
                }
            }

            throw new AquilaExceptions.SyntaxExceptions.UnclosedTagError("Unclosed tag in lines");
        }

        /// <summary>
        /// Split a string, which should be an expression, and keeping the expression-structure
        /// integrity.The integrity is kept by using the section_delimiters delimiters ( '(': ')', '[': ']' ).
        /// Split the string by a character: delimiter
        /// <para/>Example:
        /// <para/>* line = "(4 + 5) + $l[1 + 2]"
        /// <para/>* delimiter = '+'
        /// <para/>* section_delimiter = <see cref="Global.base_delimiters"/>
        /// <para/>* returns: ["(4 + 5)", "$l[1 + 2]"]
        /// </summary>
        /// <param name="line"> line of pseudo-code</param>
        /// <param name="delimiter"> </param>
        /// <param name="section_delimiters"> delimiter dictionary(opening: closing)</param>
        /// <returns> List of string, representing the sections that were cut off by the delimiter</returns>
        public static List<string> splitStringKeepingStructureIntegrity(string line, char delimiter, Dictionary<char, char> section_delimiters)
        {
            int section_depth = 0;
            int counter = 0;
            List<string> splitted = new List<string>();

            for (int i = 0; i < line.Length; i++)
            {
                foreach (var pair in section_delimiters)
                {
                    if (line[i] == pair.Key)
                    {
                        section_depth++;
                    }
                    else if (line[i] == pair.Value)
                    {
                        section_depth--;
                    }
                }

                if (section_depth == 0 && line[i] == delimiter)
                {
                    splitted.Add(line.Substring(i - counter, counter));
                    counter = -1;
                }

                counter++;
            }

            splitted.Add(line.Substring(line.Length - counter));
            return splitted;
        }

        /// <summary>
        /// Check if the expression ( 's' ) structure integrity has been kept.
        /// This is known by checking if every opening char has a corresponding closing char.
        /// </summary>
        /// <param name="s"> string expresion</param>
        /// <param name="opening"> opening char</param>
        /// <param name="closing"> closing char</param>
        /// <returns> true if the integrity has been kept. false if corrupted expression</returns>
        public static bool checkMatchingDelimiters(string s, char opening, char closing)
        {
            int depth = 0;
            foreach (char c in s)
            {
                if (c == opening)
                {
                    depth++;
                }
                else if (c == closing)
                {
                    depth--;
                }
            }

            return depth == 0;
        }

        /// <summary>
        /// If an expression is surrounded by corresponding opening and closing chars,
        /// those will be removed (e.g. "(((1 + 2) + 3)) => "(1 + 2) + 3")
        /// </summary>
        /// <param name="s"> expression</param>
        /// <param name="opening"> opening char</param>
        /// <param name="closing"> closing char</param>
        /// <returns></returns>
        public static string removeRedundantMatchingDelimiters(string s, char opening, char closing)
        {
            while (s.StartsWith(opening.ToString()))
            {
                int close_index = findCorrespondingElementIndex(s, 0, opening, closing);
                if (close_index != s.Length)
                {
                    return s;
                }

                s = s.Remove(s.Length - 1);
                s = s.Remove(0, 1);
            }

            return s;
        }

        /// <summary>
        /// In the pseudo-code, the variable "x" is accessed with this syntax: "$x".
        /// Returns the corresponding <see cref="Variable"/> in the current scope variable dictionary
        /// </summary>
        /// <param name="s"> pseudo-code variable access</param>
        /// <returns> corresponding <see cref="Variable"/></returns>
        public static Variable variableFromString(string s)
        {
            Debugging.assert(s.Length > 4);
            Debugging.assert(s.StartsWith("$"));
            s = s.Substring(1);
            s = s.Replace(" ", "");
            return Global.variableFromName(s);
        }

        /// <summary>
        /// Concatenates a string list into a single string, by using the string 'symbol' as a connection between the various list strings.
        /// </summary>
        /// <param name="splitted"> string list to concatenate</param>
        /// <param name="symbol"> symbol to stick between each string of 'splitted'</param>
        /// <returns></returns>
        public static string reuniteBySymbol(List<string> splitted, string symbol = " ")
        {
            string reunited = "";

            foreach (string split in splitted)
            {
                reunited += split + symbol;
            }

            reunited = reunited.Remove(reunited.Length - symbol.Length);
            return reunited;
        }

        /// <summary>
        /// Takes an expression and removes the parts with lower priority.
        /// this can be described as "simplifying an expresion"
        /// <para/>Examples:
        /// <para/>* input => "$l[5 + $i] - ((6 + $j) * (1 / 2)) + 4", '+'
        /// <para/>* output => "EXPR + EXPR"
        /// <para/>* input => "$l[5 + $i] - ((6 + $j) * (1 / 2)) + 4", '*'
        /// <para/>* output => EXPR
        /// <para>Note that the expressions that are not either simple or "simplified" to the "EXPR" string are lost.
        /// Therefore, this method should only be used for expression analysis only, due to data loss.</para>
        /// </summary>
        /// <param name="expr"> Expression to simplify. Should be purged</param>
        /// <param name="delimiters"> delimiters used to separate expression (look at Global.AL_OPERATIONS)</param>
        /// <returns> the given expression, but the parts with lower priority were removed</returns>
        public static string simplifyExpr(string expr, char[] delimiters)
        {
            int depth = 0;
            string simplified = "";
            int last_index = 0;

            for (int i = 0; i < expr.Length; i++)
            {
                var look = true;
                foreach (var pair in Global.base_delimiters)
                {
                    if (expr[i] == pair.Key)
                    {
                        depth++;
                        look = false;
                        break;
                    }

                    if (expr[i] == pair.Value)
                    {
                        depth--;
                        look = false;
                        break;
                    }
                }

                if (look && depth == 0)
                {
                    foreach (char op in delimiters)
                    {
                        if (expr[i] == op)
                        {
                            simplified += (last_index == 0 ? "" : " ") + "EXPR " + op;
                            last_index = i;
                        }
                    }
                }
            }

            simplified += (last_index == 0 ? "" : " ") + "EXPR";
            return simplified;
        }

        /// <summary>
        /// Get the content of a bracket-expression in a list
        /// <para/>Examples:
        /// <para/>* "[5][1][4]"
        /// <para/> -> List{"5", "1", "4"}
        /// <para/>* " [ t - 6 ] [ 1 ] "
        /// <para/> -> List{"t - 6", "1"}
        /// </summary>
        /// <param name="bracket_access_string"> The bracket expression string</param>
        /// <returns> List of the separated bracket expressions</returns>
        public static IEnumerable<string> getBracketsContent(string bracket_access_string)
        {
            // assume the string starts with a '[' and ends with a ']'
            
            List<string> bracket_access_list = new List<string>();

            int current_index = 0;
            
            // go fetch the indexes !
            while (current_index < bracket_access_string.Length)
            {
                if (bracket_access_string[current_index] != '[')
                {
                    current_index++;
                    continue;
                }
                
                int next_index = findCorrespondingElementIndex(bracket_access_string, current_index, '[', ']');
                bracket_access_list.Add(normalizeWhiteSpaces(bracket_access_string.Substring(current_index + 1, next_index - current_index - 2)));
                current_index = next_index; // go to the next char
            }

            return bracket_access_list;
        }

        /// <summary>
        /// Generate a string from a list of bools
        /// </summary>
        /// <param name="args"> Boolean values</param>
        /// <returns> String</returns>
        public static string boolString(params bool[] args) =>
            args.Aggregate("", (current, t) => current + (t ? "1" : "0"));

        /// <summary>
        /// Checks if the variable name is valid
        /// </summary>
        /// <param name="var_name"> variable name </param>
        /// <returns> valid variable ?</returns>
        public static bool validObjectName(string var_name)
        {
            const string PATTERN = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            Regex rg = new Regex(PATTERN);
            return rg.Match(var_name).Success && !Global.reserved_keywords.Contains(var_name);
        }

        /// <summary>
        /// Equivalent to ToString() method of List(<see cref="Variable"/>)
        /// </summary>
        /// <param name="var_list"> list of <see cref="Variable"/></param>
        /// <returns> string describing the entry list</returns>
        private static string varList2String(List<Variable> var_list)
        {
            // Concatenate every element to this
            string s = var_list.Aggregate("{ ", (current, variable) => current + (variable + ", "));

            // Remove the last ", " if there are any elements in the list
            if (s.Length > 2) s = s.Substring(0, s.Length - 2);

            return s + " }";
        }

        /// <summary>
        /// Takes a dynamic value and calls ToString() on it. If it is a List(<see cref="Variable"/>), calls
        /// <see cref="varList2String"/> on it. If it is null, returns "null"
        /// </summary>
        /// <param name="value"> value to convert into a string</param>
        /// <returns> corresponding string</returns>
        public static string dynamic2Str(dynamic value)
        {
            if (value is dynamic[])
            {
                string s = "// {";
                value = value as Array;
                for (int i = 0; i < value.Length; i++)
                {
                    s += dynamic2Str(value[i]) + (i == value.Length - 1 ? "" : ", ");
                }
                return s + "} //";
            }
            if (value == null) return "null";
            if (!value.ToString().Contains("System.")) return value.ToString();
            if (value is List<Variable>) return varList2String(value);
            if (value is Variable) return value.ToString();
            try
            {
                return "// " + Variable.fromRawValue(value as List<dynamic>) + " //";
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Convert a string value into a Boolean value.
        /// <para/>This function is the mapping of the following:
        /// <para/>* "0" -> false
        /// <para/>* "false" -> false
        /// <para/>* "False" -> false
        /// <para/>* "FALSE" -> false
        /// <para/>* "1" -> true
        /// <para/>* "true" -> true
        /// <para/>* "True" -> true
        /// <para/>* "TRUE" -> true
        /// Any other case will throw a <see cref="NotImplementedException"/>
        /// </summary>
        /// <param name="val"> string value to convert to a boolean one</param>
        /// <returns> corresponding boolean value</returns>
        /// <exception cref="NotImplementedException"> The string is not recognized</exception>
        public static bool string2BoolValue(string val)
        {
            switch (val)
            {
                case "true": case "1": case "True": case "TRUE":
                    return true;
                case "false": case "0": case "False": case "FALSE":
                    return false;
                default:
                    throw new ArgumentException("boolean value \"" + (val ?? "(.net null)") + "\" is invalid");
            }
        }

        /// <summary>
        /// explicit naming (see <see cref="nicePrintFunction"/>
        /// </summary>
        private static int _last_native_offset;
        /// <summary>
        /// explicit naming (see <see cref="nicePrintFunction"/>
        /// </summary>
        private static MethodBase _last_method;

        /// <summary>
        /// Used in <see cref="Debugging.print"/> &amp; <see cref="Tracer.printTrace"/>.
        /// Nicely outputs debugging information to the default stdout
        /// </summary>
        /// <param name="max_call_name_length"> number of chars before printing the args (function name + call stack depth + spaces)</param>
        /// <param name="num_new_method_separators"> number of '=' symbols when detecting a new function call</param>
        /// <param name="enable_function_depth"> shift the output using function call stack depth (everything but the prefix)</param>
        /// <param name="num_function_depth_chars"> shift magnitude (1 : 1 ' ' char per function call stack depth)</param>
        /// <param name="prefix"> the prefix is printed before each line</param>
        /// <param name="args"> the args you want to print, consecutively (ToString() called on each one)</param>
        public static void nicePrintFunction(int max_call_name_length,
            int num_new_method_separators,
            bool enable_function_depth,
            int num_function_depth_chars,
            string prefix,
            params object[] args)
        {
            // correct parameters
            num_function_depth_chars = enable_function_depth ? num_function_depth_chars : 0;
            prefix += enable_function_depth ? "" : " ";
            
            // extract current method
            StackTrace stack_trace = new StackTrace();
            StackFrame current_stack_frame = stack_trace.GetFrame(2); // skip one frame bc this method is already called from Debugging.print or Tracer.printTrace (normally: GetFrame(1))
            MethodBase current_method = current_stack_frame.GetMethod();
            int current_native_offset = current_stack_frame.GetNativeOffset();
            string call_name = current_method.Name;
            int num_frames = stack_trace.GetFrames().Length - 2; // remove two frames : this one and debugging function
            string space_separator = new String(' ', num_frames * num_function_depth_chars);
            
            // new method call ?
            if (current_native_offset < _last_native_offset || current_method != _last_method)
            { // (current_native_offset < _last_native_offset) means that 1. new function call or 2. same function called again (recursive)
                string delim = new String('=', num_new_method_separators);
                Global.stdoutWrite(prefix + space_separator + delim);
                Global.stdoutWrite(" " + call_name + " (" + num_frames + ") ");
                Global.stdoutWriteLine(delim);
            }
            _last_native_offset = current_native_offset;
            _last_method = current_method;
            
            // every line config
            int missing_spaces = max_call_name_length - call_name.Length - Context.getStatus().ToString().Length - 2; // 2: parentheses

            // debugging mode is on
            Global.stdoutWrite(prefix  + " [" + Global.current_line_index + "]" + space_separator + call_name + "(" + Context.getStatus() + ")");
            Global.stdoutWrite(new String(' ', missing_spaces));

            Global.stdoutWrite(" : ");
            foreach (dynamic arg in args)
            {
                Global.stdoutWrite(arg == null ? "(.net null)" : arg.ToString());
            }
            Global.stdoutWriteLine();
        }
    }
}
