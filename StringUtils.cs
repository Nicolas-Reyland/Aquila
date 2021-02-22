using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
// ReSharper disable SuggestVarOrType_SimpleTypes

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
        public static void printStringList(List<string> list, bool print_index = false)
        {
            short index = 0;
            foreach (string line in list)
            {
                if (print_index) Console.Write("{0} ", index++);
                Console.WriteLine(line);
            }
        }
        
        /// <summary>
        /// DEPRECATED. ONLY KEPT FOR BACKUP PURPOSES.
        /// <para/>If you want to use it, /** please be aware **/ that multiple-line comment tags don't work
        /// if used on the same line. You cannot have multiple multiple-line comment tags on the same line
        /// </summary>
        /// <param name="code"> lines of code</param>
        /// <returns> list of lines without comment</returns>
        /// <exception cref="Global.aquilaError"/>
        public static List<string> removeComments(List<string> code)
        {
            Console.Error.WriteLine("DEPRECATION WARNING. the StringUtils.removeComments method is deprecated. use at your own risk");
            
            List<string> new_code = new List<string>();
            bool in_comment = false;

            char[] char_array= Global.multiple_lines_comment_string.ToCharArray();
            Array.Reverse(char_array);
            string multiple_lines_comment_string_closing = new string(char_array);
            Debugging.print("multiple comment closing is " + multiple_lines_comment_string_closing);

            int index = 0; // lines start at 1

            foreach (string line in code)
            {
                index++;
                Debugging.print("comments - line ", index);
                // in code
                if (!in_comment)
                {
                    if (line.Contains(Global.single_line_comment_string))
                    {
                        Debugging.print("single comment on line");
                        // no multiple line comment on same line
                        if (!line.Contains(Global.multiple_lines_comment_string))
                        {
                            new_code.Add(line.Substring(0,
                                line.IndexOf(Global.single_line_comment_string, StringComparison.Ordinal)));
                            Debugging.print("adding as only simple comment on line ", index);
                            continue;
                        }
                        
                        Debugging.print("multiple comment on line too!");
                        int simple_index = line.IndexOf(Global.single_line_comment_string, StringComparison.Ordinal);
                        int multiple_index =
                            line.IndexOf(Global.multiple_lines_comment_string, StringComparison.Ordinal);
                        // simple comment
                        if (simple_index > multiple_index)
                        {
                            Debugging.print("simple comment first on line ", index);
                            new_code.Add(line.Substring(0, simple_index));
                            continue;
                        }
                        // multiple comment
                        Debugging.print("multiple comment first on line ", index);
                        new_code.Add(line.Substring(0, multiple_index));
                        in_comment = true;
                        continue;
                    }
                    // only multiple line comment
                    if (line.Contains(Global.multiple_lines_comment_string))
                    {
                        Debugging.print("multiple comment on line only");
                        new_code.Add(line.Substring(0,
                            line.IndexOf(Global.multiple_lines_comment_string, StringComparison.Ordinal)));
                        in_comment = true;
                        continue;
                    }
                    // nothing there
                    Debugging.print("added line ", index, " as is");
                    new_code.Add(line);
                }
                // in comment
                else
                {
                    Debugging.print("line ", index, " is in comment");
                    if (line.Contains(multiple_lines_comment_string_closing))
                    {
                        new_code.Add(line.Substring(
                            line.IndexOf(multiple_lines_comment_string_closing, StringComparison.Ordinal) +
                            multiple_lines_comment_string_closing.Length));
                        in_comment = false;
                    }
                }
            }

            if (in_comment) throw Global.aquilaError(); // no closing comment tag

            return new_code;
        }
        
        /// <summary>
        /// Remove all the tabs (replaced by one space if needed) and unnecessary spaces from a pseudo-code line.
        /// Also removes comments from lines (comments start with <see cref="Global.single_line_comment_string"/>).
        /// <para/>The input should not have comments in it (<seealso cref="removeComments"/> 
        /// </summary>
        /// <param name="line"> line of code, can be any string tho</param>
        /// <returns> purged line</returns>
        public static string purgeLine(string line)
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
                if (new_line[new_line.Length - 1] == 32)
                {
                    new_line = new_line.Remove(new_line.Length - 1);
                }
            }

            return new_line;
        }

        /// <summary>
        /// Same as purgeLine, but one a List of lines. If the input lines contain a line only
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
                string new_line = purgeLine(line);

                // if the line is not empty, add it
                if (new_line != "")
                {
                    new_lines.Add(new_line);
                }
            }

            return new_lines;
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
        /// <exception cref="Global.aquilaError"></exception>
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

            throw Global.aquilaError(); // no corresponding closing char
        }
        
        /// <summary>
        /// Find the corresponding char to an opening and closing string.
        /// It works exactly as the <see cref="findCorrespondingElementIndex(string,int,char,char)"/>, but we are looking for
        /// strings starting with the opening and ending with closing.
        /// <para/>Example:
        /// <para/>* lines = ["var a = 0", "for (var i, 0, 5, 1)", "if (var i == var j)", "a++", "end-if", "end-for"]
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
        /// <exception cref="Global.aquilaError"></exception>
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
            
            throw Global.aquilaError(); // no corresponding closing line
        }

        /// <summary>
        /// Split a string, which should be an expression, and keeping the expression-structure
        /// integrity.The integrity is kept by using the section_delimiters delimiters ( '(': ')', '[': ']' ).
        /// Split the string by a character: delimiter
        /// <para/>Example:
        /// <para/>* line = "(4 + 5) + var l[1 + 2]"
        /// <para/>* delimiter = '+'
        /// <para/>* section_delimiter = <see cref="Global.base_delimiters"/>
        /// <para/>* returns: ["(4 + 5)", "var l[1 + 2]"]
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
                foreach (KeyValuePair<char, char> section_delimiter in section_delimiters)
                {
                    if (line[i] == section_delimiter.Key)
                    {
                        section_depth++;
                    }
                    else if (line[i] == section_delimiter.Value)
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
        /// In the pseudo-code, the variable "x" is accessed with this syntax: "var x".
        /// Returns the corresponding <see cref="Variable"/> in the <see cref="Global.variables"/> dictionary
        /// </summary>
        /// <param name="s"> pseudo-code variable access</param>
        /// <returns> corresponding <see cref="Variable"/></returns>
        public static Variable variableFromString(string s)
        {
            Debugging.assert(s.Length > 4);
            Debugging.assert(s.StartsWith("var "));
            s = s.Substring(4);
            s = s.Replace(" ", "");
            Debugging.assert(Global.variables.ContainsKey(s));
            return Global.variables[s];
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
        /// Example:
        /// input => "var l[5 + var i] - ((6 + var j) * (1 / 2)) + 4", '+'
        /// output => "EXPR + EXPR"
        /// Note that the expressions that are not either simple or "simplified" to the "EXPR" string
        /// This should only be used for expression analysis only, due to data integrity loss.
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
                foreach (KeyValuePair<char,char> delimiter in Global.base_delimiters)
                {
                    if (expr[i] == delimiter.Key)
                    {
                        depth++;
                        look = false;
                        break;
                    }

                    if (expr[i] == delimiter.Value)
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
    }
}