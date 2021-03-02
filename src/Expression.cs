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
        /// The literal expresion, as a string (e.g. "$x + 5")
        /// </summary>
        internal readonly string expr;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="expr"> expression string</param>
        public Expression(string expr)
        {
            this.expr = StringUtils.purgeLine(expr);
        }

        /// <summary>
        /// Call <see cref="parse"/> on <see cref="expr"/>
        /// </summary>
        /// <returns> Arithmetic or logic value of <see cref="expr"/></returns>
        public Variable evaluate(bool force_stop_tracer_update = false) => parse(expr, force_stop_tracer_update);

        /// <summary>
        /// Takes an arithmetical or logical expression and returns the corresponding variable
        /// <para/>Examples:
        /// <para/>* "5 + 6" : returns Integer (11)
        /// <para/>* "$l[5 * (1 - $i)]" : returns the elements at index 5*(1-i) in the list/array l
        /// <para/>* "$l" : returns the list variable l
        /// </summary>
        /// <param name="expr_string"> expression to parse</param>
        /// <returns> Variable object containing the value of the evaluated expression value (at time t)</returns>
        public static Variable parse(string expr_string, bool force_stop_tracer_update = false)
        {
            /* Order of operations:
             * checking expression string integrity
             * raw dynamic list
             * clean redundant symbols
             * raw integer value
             * raw boolean value (not done yet)
             * raw float value (not done yet)
             * mathematical or logical operation
             * function call
             * variable access (e.g. $name or in list by index)
             */
            
            // clean expression
            expr_string = StringUtils.purgeLine(expr_string);

            Debugging.print("input expression: " + expr_string);

            // matching parentheses & brackets
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '(', ')'));
            Debugging.assert(StringUtils.checkMatchingDelimiters(expr_string, '[', ']'));
            expr_string = StringUtils.removeRedundantMatchingDelimiters(expr_string, '(', ')');
            
            // update Tracers before expression execution
            if (!force_stop_tracer_update) Tracer.updateTracers("update Tracers in Expression.parse");

            // dynamic list
            try
            {
                return Parser.string2DynamicList(expr_string);
            }
            catch
            {
                //
            }
            
            // now that lists are over, check for redundant brackets
            expr_string = StringUtils.removeRedundantMatchingDelimiters(expr_string, '[', ']');

            if (expr_string == null)
            {
                throw Global.aquilaError();
            }

            Debugging.assert(expr_string != ""); //! NullValue here, instead of Exception

            // try evaluating expression as an integer
            if (Int32.TryParse(expr_string, out int int_value))
            {
                return new Integer(int_value);
            }
            
            // try evaluating expression as a boolean
            if (expr_string == "true")
            {
                return new BooleanVar(true);
            }
            if (expr_string == "false")
            {
                return new BooleanVar(false);
            }

            // try evaluating expression as float
            if (float.TryParse(expr_string, out float float_value))
            {
                return new FloatVar(float_value);
            }
            if (float.TryParse(expr_string.Replace('.', ','), out float_value))
            {
                return new FloatVar(float_value);
            }
            if (expr_string.EndsWith("f") &&
                float.TryParse(expr_string.Substring(0, expr_string.Length - 1), out float_value))
            {
                return new FloatVar(float_value);
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
                        Debugging.print("operation ", expr_string, " and op: ", op);
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
                Debugging.assert(expr_string[1] == '(');
                Debugging.assert(expr_string[expr_string.Length - 1] == ')');
                Variable expr = parse(expr_string.Substring(2, expr_string.Length - 3));
                Debugging.assert(expr is BooleanVar);
                Debugging.print("base val b4 not operator is ", expr.getValue());
                return ((BooleanVar) expr).not();
            }
            
            // value function call
            if (expr_string.Contains("("))
            {
                string function_name = expr_string.Split('(')[0]; // extract function name
                Debugging.print("function name: ", function_name);
                Functions.assertFunctionExists(function_name);
                expr_string = expr_string.Substring(function_name.Length); // remove function name
                expr_string = expr_string.Substring(1, expr_string.Length - 2); // remove parenthesis
                Debugging.print("expr_string for function call ", expr_string);

                List<Expression> arg_list = new List<Expression>();
                foreach (string arg_string in StringUtils.splitStringKeepingStructureIntegrity(expr_string, ',', Global.base_delimiters))
                {
                    string purged_arg_string = StringUtils.purgeLine(arg_string);
                    Expression arg_expr = new Expression(purged_arg_string);
                    arg_list.Add(arg_expr);
                }

                if (arg_list.Count == 1 && arg_list[0].expr == "")
                {
                    arg_list = new List<Expression>();
                }
                
                Debugging.print("creating value function call with ", arg_list.Count, " parameters");

                FunctionCall func_call = new FunctionCall(function_name, arg_list);
                return func_call.call_function();
            }


            // variable access
            
            // since it is the last possibility for the parse call to return something, assert it is a variable
            Debugging.print("variable by name: ", expr_string);
            Debugging.assert(expr_string.StartsWith("$")); // SyntaxError
            // ReSharper disable once PossibleNullReferenceException
            if (expr_string.Contains("["))
            {
                Debugging.print("list access");
                throw new NotImplementedException("list bracket access disabled due to tracing issues");
                // brackets
/*
                Debugging.assert(expr_string.EndsWith("]")); // cannot be "$l[0] + 5" bc AL_operations have already been processed
                int bracket_start_index = expr_string.IndexOf('[');
                Debugging.assert(bracket_start_index > 1); // "$[$i - 4]" is not valid
                // variable
                string var_name = expr_string.Substring(1, bracket_start_index - 1);
                Variable temp_list = variableFromName(var_name);
                Debugging.assert(temp_list is DynamicList);
                DynamicList list_var = temp_list as DynamicList;
                Debugging.printTrace("dynamic-list $", var_name, " exists");
                // index
                string index_string = expr_string.Substring(bracket_start_index + 1,
                    expr_string.Length - bracket_start_index - 2); // "$l[5]" => "5"
                Integer index = parse(index_string) as Integer;
                list_var.validateIndex(index);
                // return value
                return list_var.atIndex(index);
*/
            }
            else // only variable name, no brackets
            {
                Debugging.print("$simple var access");
                string var_name = expr_string.Substring(1);
                Debugging.assert(Global.variables.ContainsKey(var_name));
                Debugging.print("var $", var_name, " exists");
                return Global.variables[var_name];
            }
        }

        /// <summary>
        /// Get the <see cref="Variable"/> from the <see cref="Global.variables"/> Dictionary.
        /// You an give the variable name with or without the "$" prefix
        /// </summary>
        /// <param name="var_name"> The variable name (with or without the "$" as a prefix)</param>
        /// <returns> the corresponding <see cref="Variable"/></returns>
        public static Variable variableFromName(string var_name)
        {
            if (var_name.StartsWith("$")) var_name = var_name.Substring(1);
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
            Debugging.print("applyOperator: ", v1.ToString(), " ", op, " ", v2.ToString(), " (", v1.getTypeString(), " ", op, " ", v2.getTypeString(), ")");
            Debugging.assert(v1.hasSameParent(v2)); // operations between same classes/subclasses
            switch (op)
            {
                // arithmetic
                case '+':
                    if (v1 is Integer integer)
                    {
                        return integer.addition(v2 as Integer);
                    }
                    else if (v1 is FloatVar)
                    {
                        return ((FloatVar) v1).addition(v2 as FloatVar);
                    }
                    else
                    {
                        throw Global.aquilaError(); // TypeError
                    }   
                case '-':
                    if (v1 is Integer)
                    {
                        return ((Integer) v1).subtraction(v2 as Integer);
                    }
                    else if (v1 is FloatVar)
                    {
                        return ((FloatVar) v1).subtraction(v2 as FloatVar);
                    }
                    else
                    {
                        throw Global.aquilaError(); // TypeError
                    }
                case '/':
                    if (v1 is Integer)
                    {
                        return ((Integer) v1).division(v2 as Integer);
                    }
                    else if (v1 is FloatVar)
                    {
                        return ((FloatVar) v1).division(v2 as FloatVar);
                    }
                    else
                    {
                        throw Global.aquilaError(); // TypeError
                    }
                case '*':
                    if (v1 is Integer)
                    {
                        return ((Integer) v1).mult(v2 as Integer);
                    }
                    else if (v1 is FloatVar)
                    {
                        return ((FloatVar) v1).mult(v2 as FloatVar);
                    }
                    else
                    {
                        throw Global.aquilaError(); // TypeError
                    }
                case '%':
                    if (v1 is Integer)
                    {
                        return ((Integer) v1).modulo(v2 as Integer);
                    }
                    else
                    {
                        throw Global.aquilaError(); // TypeError
                    }
                // logic
                case '<':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison == -1);
                case '>':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison == 1);
                case '{':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison != 1);
                case '}':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison != -1);
                case '~':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison == 0);
                case ':':
                    Debugging.assert(v1 is Integer || v1 is FloatVar);
                    if (v1 is Integer)
                    {
                        comparison = ((Integer) v1).compare(v2 as Integer);
                    }
                    else
                    {
                        comparison = ((FloatVar) v1).compare(v2 as FloatVar);
                    }
                    return new BooleanVar(comparison != 0);
                case '|':
                    Debugging.assert(v1 is BooleanVar);
                    return ((BooleanVar) v1).or((BooleanVar) v2);
                case '&':
                    Debugging.assert(v1 is BooleanVar);
                    return ((BooleanVar) v1).and((BooleanVar) v2);
                case '^':
                    Debugging.assert(v1 is BooleanVar);
                    return ((BooleanVar) v1).xor((BooleanVar) v2);
                default:
                    throw Global.aquilaError(); // Operation is not implemented ?
            }
        }
    }
}