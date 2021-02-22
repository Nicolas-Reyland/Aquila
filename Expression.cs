using System;
using System.Collections.Generic;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// An <see cref="Expression"/> is basically a value. It can be an <see cref="Integer"/>,
    /// a <see cref="DynamicList"/> or a <see cref="BooleanVar"/>.
    /// <para>The only exception is the function call, which is a value, but it can modify values if
    /// they are passed through by reference</para>
    /// <see cref="Expression"/>s are often represented as a literal arithmetical or logical expressions
    /// </summary>
    public class Expression
    {
        /// <summary>
        /// The literal expresion, as a string (e.g. "var x + 5")
        /// </summary>
        private readonly string _expr;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="expr"> expression string</param>
        public Expression(string expr)
        {
            this._expr = StringUtils.purgeLine(expr);
        }

        /// <summary>
        /// Call <see cref="parse"/> on <see cref="_expr"/>
        /// </summary>
        /// <returns> Arithmetic or logic value of <see cref="_expr"/></returns>
        public Variable evaluate() => parse(_expr);

        /// <summary>
        /// Takes an arithmetical or logical expression and returns the corresponding variable
        /// <para/>Examples:
        /// <para/>* "5 + 6" : returns Integer (11)
        /// <para/>* "var l[5 * (1 - var i)]" : returns the elements at index 5*(1-i) in the list/array l
        /// <para/>* "var l" : returns the list variable l
        /// </summary>
        /// <param name="expr_string"> expression to parse</param>
        /// <returns> Variable object containing the value of the evaluated expression value (at time t)</returns>
        public static Variable parse(string expr_string)
        {
            /* Order of operations:
             * checking expression string integrity
             * raw dynamic list
             * clean redundant symbols
             * raw integer value
             * mathematical or logical operation
             * function call
             * variable access (e.g. var name or in list by index)
             */
            
            // clean expression
            expr_string = StringUtils.purgeLine(expr_string);

            Debugging.print("input expression: " + expr_string);
            
            // dynamic list
            try
            {
                return Parser.string2DynamicList(expr_string);
            }
            catch
            {
                //
            }

            // matching parentheses & brackets
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '(', ')'));
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '[', ']'));
            expr_string = StringUtils.removeRedundantMatchingDelimiters(expr_string, '(', ')');
            expr_string = StringUtils.removeRedundantMatchingDelimiters(expr_string, '[', ']');

            if (expr_string == null)
            {
                throw Global.aquilaError();
            }
            
            Debugging.assert(expr_string != "");

            // try evaluating expression as an integer
            if (Int32.TryParse(expr_string, out int potential_value))
            {
                return new Integer(potential_value);
            }
            
            // mathematical operations (and logical operations ?)
            foreach (char op in Global.al_operations)
            {
                // ReSharper disable once PossibleNullReferenceException
                if (expr_string.Contains(op.ToString()))
                {
                    string simplified = StringUtils.simplifyExpr(expr_string, new []{ op }); // only look for specific delimiter
                    // more than one simplified expression ?
                    if (simplified.Split(op).Length > 1)
                    {
                        Debugging.print("operation with", expr_string, " and op: ", op);
                        List<string> splitted_str =
                            StringUtils.splitStringKeepingStructureIntegrity(expr_string, op, Global.base_delimiters);
                        List<Variable> splitted_var = new List<Variable>();
                        foreach (string split_expr_str in splitted_str)
                        {
                            splitted_var.Add(parse(split_expr_str));
                        }
                        // reduce the list to a list of one element
                        // e.g. expr1 + expr2 + expr3 => final_expr
                        while (splitted_var.Count > 1)
                        {
                            // merge the two first expressions
                            Variable expr1_var = splitted_var[0];
                            Variable expr2_var = splitted_var[1];
                            Variable result = applyOperator(expr1_var, expr2_var, op);
                            // merge result of 0 and 1
                            splitted_var[0] = result;
                            // remove 1 (part of it found in 0 now)
                            splitted_var.RemoveAt(1);
                        }

                        return splitted_var[0];
                    }
                }
            }
            // '!' operator (only one to take one variable)
            if (expr_string.StartsWith("!"))
            {
                Debugging.assert(expr_string[expr_string.Length - 1] == '(');
                Debugging.assert(expr_string[expr_string.Length - 1] == ')');
                Variable expr = parse(expr_string.Substring(2, expr_string.Length - 3));
                Debugging.assert(expr is BooleanVar);
                Debugging.print("base val b4 not operator is ", expr.getValue());
                return ((BooleanVar) expr).not();
            }
            
            // function call
            if (expr_string.Contains("("))
            {
                string function_name = expr_string.Split('(')[0]; // extract function name
                Debugging.print("function name: ", function_name);
                Functions.assertFunctionExists(function_name);
                expr_string = expr_string.Substring(function_name.Length); // remove function name
                expr_string = expr_string.Substring(1, expr_string.Length - 2); // remove parenthesis
                Debugging.print("expr_string for function call ", expr_string);
                
                // check for return function
                if (function_name == "return")
                {
                    expr_string = StringUtils.purgeLine(expr_string);
                    Functions.callFunctionByName(function_name, expr_string);
                    throw Global.aquilaError(); // should not come to this, normally
                }

                List<Expression> arg_list = new List<Expression>();
                foreach (string arg_string in StringUtils.splitStringKeepingStructureIntegrity(expr_string, ',', Global.base_delimiters))
                {
                    string purged_arg_string = StringUtils.purgeLine(arg_string);
                    Expression arg_expr = new Expression(purged_arg_string);
                    arg_list.Add(arg_expr);
                }

                Debugging.print("creating function call");

                return new FunctionCall(function_name, arg_list);
            }


            // variable access
            
            // since it is the last possibility for the parse call to return something, assert it is a variable
            Debugging.assert(expr_string.StartsWith("var "));
            // ReSharper disable once PossibleNullReferenceException
            if (expr_string.Contains("["))
            {
                // brackets
                Debugging.assert(expr_string.EndsWith("]")); // cannot be "var l[0] + 5" bc AL_operations have already been processed
                int bracket_start_index = expr_string.IndexOf('[');
                Debugging.assert(bracket_start_index > 4); // "var [var i - 4]" is not valid
                // variable
                string var_name = expr_string.Substring(4, bracket_start_index - 4);
                Variable temp_list = variableFromName(var_name);
                Debugging.assert(temp_list is DynamicList);
                DynamicList list_var = temp_list as DynamicList;
                // index
                string index_string = expr_string.Substring(bracket_start_index + 1,
                    expr_string.Length - bracket_start_index - 2); // "var l[5]" => "5"
                Integer index = parse(index_string) as Integer;
                list_var.validateIndex(index);
                // return value
                return list_var.atIndex(index);
            }
            else // only variable name, no brackets
            {
                string var_name = expr_string.Substring(4);
                Debugging.assert(Global.variables.ContainsKey(var_name));
                return Global.variables[var_name];
            }
        }

        /// <summary>
        /// Get the <see cref="Variable"/> from the <see cref="Global.variables"/> Dictionary.
        /// You an give the variable name with or without the "var " prefix
        /// </summary>
        /// <param name="var_name"> The variable name (with or without the "var " as a prefix)</param>
        /// <returns> the corresponding <see cref="Variable"/></returns>
        public static Variable variableFromName(string var_name)
        {
            if (var_name.StartsWith("var ")) var_name = var_name.Substring(4);
            Debugging.assert(Global.variables.ContainsKey(var_name));
            Variable variable = Global.variables[var_name];
            return variable;
        }

        /// <summary>
        /// Applies an arithmetical or logical operation on two <see cref="Variable"/>s
        /// <para/>result = (variable1) op (variable2)
        /// </summary>
        /// <param name="v1"> <see cref="Variable"/> 1</param>
        /// <param name="v2"> <see cref="Variable"/> 2</param>
        /// <param name="op"> operator char</param>
        /// <returns> result <see cref="Variable"/></returns>
        /// <exception cref="Global.aquilaError"></exception>
        private static Variable applyOperator(Variable v1, Variable v2, char op)
        {
            int comparison;
            switch (op)
            {
                // arithmetic
                case '+':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    return ((Integer) v1).addition((v2 as Integer));
                case '-':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    return ((Integer) v1).subtraction((v2 as Integer));
                case '/':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    return ((Integer) v1).division((v2 as Integer));
                case '*':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    return ((Integer) v1).mult((v2 as Integer));
                case '%':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    return ((Integer) v1).modulo((v2 as Integer));
                // logic
                case '<':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison);
                    return new BooleanVar(comparison == -1);
                case '>':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison);
                    return new BooleanVar(comparison == 1);
                case '{':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison);
                    return new BooleanVar(comparison != 1);
                case '}':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison);
                    return new BooleanVar(comparison != -1);
                case '~':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison);
                    return new BooleanVar(comparison == 0);
                case ':':
                    Debugging.assert(v1 is Integer);
                    Debugging.assert(v2 is Integer);
                    comparison = ((Integer) v1).compare((v2 as Integer));
                    Debugging.print(comparison); //!
                    return new BooleanVar(comparison != 0);
                case '|':
                    Debugging.assert(v1 is BooleanVar);
                    Debugging.assert(v2 is BooleanVar);
                    return ((BooleanVar) v1).or((BooleanVar) v2);
                case '&':
                    Debugging.assert(v1 is BooleanVar);
                    Debugging.assert(v2 is BooleanVar);
                    return ((BooleanVar) v1).and((BooleanVar) v2);
                case '^':
                    Debugging.assert(v1 is BooleanVar);
                    Debugging.assert(v2 is BooleanVar);
                    return ((BooleanVar) v1).xor((BooleanVar) v2);
                default:
                    throw Global.aquilaError(); // just not implemented
            }
        }
    }
}