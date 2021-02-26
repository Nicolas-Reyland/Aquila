using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// The <see cref="Functions"/> lets you define new value_functions and use
    /// them in your code. You can only add value_functions that return a value here.
    /// <para/>See <see cref="VoidFunctionCall"/> for value_functions that are cannot be
    /// thought of like an <see cref="Expression"/>
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// Default function
        /// <para>
        /// Calculates the length of a <see cref="DynamicList"/> and returns it as an <see cref="Integer"/>
        /// </para>
        /// </summary>
        /// <param name="list_expr"> the target <see cref="DynamicList"/></param>
        /// <returns> <see cref="Integer"/> which has the list's length as value</returns>
        private static Variable lengthFunction(Expression list_expr)
        {
            // evaluate every expression
            DynamicList list = list_expr.evaluate() as DynamicList;
            // length
            return list.length();
        }

        /// <summary>
        /// Default function
        /// </summary>
        /// <para>
        /// Creates a new list which has the elements at index a and b swapped
        /// </para>
        /// <param name="list_expr"> the target <see cref="DynamicList"/> (as an <see cref="Expression"/>)</param>
        /// <param name="a_expr"> index of the first element</param>
        /// <param name="b_expr"> index of the second element</param>
        /// <returns> New list which has the values at index a and b swapped</returns>
        private static Variable swapFunction(Expression list_expr, Expression a_expr, Expression b_expr)
        {
            // evaluate every expression
            DynamicList list = list_expr.evaluate() as DynamicList;
            Integer a = a_expr.evaluate() as Integer;
            Integer b = b_expr.evaluate() as Integer;
            // swap
            list.validateIndex(a);
            list.validateIndex(b);
            Integer temp = list.atIndex(a) as Integer;
            List < Variable > copy = list.getValue();
            copy[a.getValue()] = copy[b.getValue()];
            copy[b.getValue()] = temp;
            return new DynamicList(copy);
        }

        /// <summary>
        /// Default function
        /// </summary>
        /// <para>
        /// Creates a copy of a <see cref="DynamicList"/>
        /// </para>
        /// <param name="list_expr"> the target <see cref="DynamicList"/></param>
        /// <returns> A new <see cref="DynamicList"/>. It's values are the same as the target list</returns>
        private static Variable copyListFunction(Expression list_expr)
        {
            // evaluate every expression
            DynamicList list = list_expr.evaluate() as DynamicList;
            // copy list
            List<Variable> copy = list.getValue();
            return new DynamicList(copy);
        }

        /// <summary>
        /// Cannot be overwritten. Used to return a value in a function or the main algorithm.
        /// </summary>
        /// <param name="expr"> string value of the returned expression</param>
        /// <exception cref="AquilaExceptions.ReturnValueException"> Exception raised to stop the <see cref="Algorithm"/> from executing any further instructions</exception>
        private static Variable returnFunction(Expression expr) // Variable return type only for the callFunctionByName compatibility
        {
            // this Exception will stop the function/algorithm from executing
            throw new AquilaExceptions.ReturnValueException(expr._expr);
        }

        /// <summary>
        /// Prints the value of an <see cref="Expression"/> to the stdout. Doesn't add a return '\n' symbol
        /// </summary>
        /// <param name="value"> Expression you want to print (its evaluated value)</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printFunction(Expression value)
        {
            Debugging.print("begin printing to console");
            Console.Write(value.evaluate().ToString());
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Prints a new-line-char to the stdout
        /// </summary>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printEndlFunction()
        {
            Debugging.print("begin printing to console");
            Console.Write('\n');
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Prints the string of the Expression
        /// </summary>
        /// <param name="expr"> <see cref="NullVar"/> (equivalent of null/void)</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printStrFunction(Expression expr)
        {
            Debugging.print("begin printing to console");
            Console.Write(expr._expr);
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Remove a <see cref="Variable"/> from the <see cref="Global.variables"/> dictionary
        /// </summary>
        /// <param name="expr"> <see cref="Expression"/> resulting in a <see cref="Variable"/> from <see cref="Global.variables"/></param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar deleteVarFunction(Expression expr)
        {
            // evaluate every expression
            Variable variable = expr.evaluate();
            // delete var
            Debugging.assert(Global.variables.ContainsValue(variable)); // UnknownVariableNameException
            // search the first element which matches the variable
            KeyValuePair<string, Variable> item = Global.variables.First(kvp => kvp.Value == variable);
            // remove the variable by key (cannot remove by value)
            Global.variables.Remove(item.Key);

            return new NullVar();
        }

        // float2int




        // int2float




        /// <summary>
        /// Holds all the non-void value_functions. There are some default value_functions, but you can add your
        /// own through the <see cref="Functions.addValueFunction"/> static method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> value_functions = new Dictionary<string, Delegate>
        {
            {"length", new Func<Expression, Variable>(lengthFunction)},
            {"swap", new Func<Expression, Expression, Expression, Variable>(swapFunction)},
            {"copy_list", new Func<Expression, Variable>(copyListFunction)},
        };

        /// <summary>
        /// Holds all the non-value void_functions. There are some default void_functions, but you can add your
        /// own through the <see cref="Functions.addVoidFunction"/> static method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> void_functions = new Dictionary<string, Delegate>
        {
            {"return", new Func<Expression, Variable>(returnFunction)}, // NullVar is equivalent of void, null or none
            {"print", new Func<Expression, NullVar>(printFunction)},
            {"print_endl", new Func<NullVar>(printEndlFunction)},
            {"print_str", new Func<Expression, NullVar>(printStrFunction)},
            {"delete_var", new Func<Expression, NullVar>(deleteVarFunction)},
        };

        /// <summary>
        /// Add your own value_functions to the <see cref="value_functions"/> dictionary. If the function
        /// already exists in it, please use the <see cref="Functions.addFunctionOverwrite"/> method instead
        /// </summary>
        /// <param name="function_name"> function name to add (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addValueFunction(string function_name, Delegate function)
        {
            Debugging.assert(!value_functions.ContainsKey(function_name)); // UnknownFunctionNameException
            value_functions.Add(function_name, function);
        }

        /// <summary>
        /// Add your own value_functions to the <see cref="void_functions"/> dictionary. If the function
        /// already exists in it, please use the <see cref="Functions.addFunctionOverwrite"/> method instead
        /// </summary>
        /// <param name="function_name"> function name to add (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addVoidFunction(string function_name, Delegate function)
        {
            Debugging.assert(!void_functions.ContainsKey(function_name)); // DefinedExistingFunctionException
            void_functions.Add(function_name, function);
        }

        /// <summary>
        /// Overwrite default value_functions or your own value_functions using this method. The function has to exist
        /// in the <see cref="value_functions"/> or <see cref="void_functions"/> dictionary.
        /// <para/>You cannot overwrite the <see cref="Functions.returnFunction"/> function
        /// </summary>
        /// <param name="function_name"> function name to overwrite (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addFunctionOverwrite(string function_name, Delegate function)
        {
            assertFunctionExists(function_name); // OverwriteFunctionDoesNotExistException
            value_functions[function_name] = function;
        }

        /// <summary>
        /// Call a function from the <see cref="value_functions"/> or <see cref="void_functions"/> dictionary. This can be a default
        /// function or a custom function, which you added with <see cref="addValueFunction"/>, <see cref="addVoidFunction"/> or
        /// overwrote using <see cref="addFunctionOverwrite"/>
        /// </summary>
        /// <param name="name"> The function name (key)</param>
        /// <param name="args"> The arguments for you function</param>
        /// <returns></returns>
        public static Variable callFunctionByName(string name, params object[] args)
        {
            if (value_functions.ContainsKey(name))
            {
                Debugging.print("invoking value function ", name, " dynamically with ", args.Length, " argument(s)");
                return value_functions[name].DynamicInvoke(args) as Variable;
            }
            if (void_functions.ContainsKey(name))
            {
                Debugging.print("invoking void function ", name, " dynamically with ", args.Length, " argument(s)");
                return void_functions[name].DynamicInvoke(args) as Variable;
            }

            throw Global.aquilaError(); // UnknownFunctionNameException
        }

        /// <summary>
        /// Use this everytime you want to access a function in the <see cref="value_functions"/> or <see cref="void_functions"/> dictionaries by its name (key)
        /// </summary>
        /// <param name="function_name"></param>
        public static void assertFunctionExists(string function_name) =>
            Debugging.assert(value_functions.ContainsKey(function_name) ^ void_functions.ContainsKey(function_name)); // UnknownFunctionNameException
    }
}
