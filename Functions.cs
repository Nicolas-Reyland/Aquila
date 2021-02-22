using System;
using System.Collections.Generic;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// The <see cref="Functions"/> lets you define new functions and use
    /// them in your code. You can only add functions that return a value here.
    /// <para/>See <see cref="VoidFunctionCall"/> for functions that are cannot be
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
        /// <param name="list"> the target <see cref="DynamicList"/></param>
        /// <returns> <see cref="Integer"/> which has the list's length as value</returns>
        private static Variable lengthFunction(DynamicList list)
        {
            return list.length();
        }

        /// <summary>
        /// Default function
        /// </summary>
        /// <para>
        /// Creates a new list which has the elements at index a and b swapped
        /// </para>
        /// <param name="list"> the target <see cref="DynamicList"/></param>
        /// <param name="a"> index of the first element</param>
        /// <param name="b"> index of the second element</param>
        /// <returns> New list which has the values at index a and b swapped</returns>
        private static Variable swapFunction(DynamicList list, Integer a, Integer b)
        {
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
        /// <param name="list"> the target <see cref="DynamicList"/></param>
        /// <returns> A new <see cref="DynamicList"/>. It's values are the same as the target list</returns>
        private static Variable copyListFunction(DynamicList list)
        {
            List<Variable> copy = list.getValue();
            return new DynamicList(copy);
        }

        /// <summary>
        /// Cannot be overwritten. Used to return a value in a function or the main algorithm.
        /// </summary>
        /// <param name="expr"> string value of the returned expression</param>
        /// <exception cref="AquilaExceptions.ReturnValueException"> Exception raised to stop the <see cref="Algorithm"/> from executing any further instructions</exception>
        private static Variable returnFunction(string expr) // Variable return type only for the callFunctionByName compatibility
        {
            // this Exception will stop the function/algorithm from executing
            throw new AquilaExceptions.ReturnValueException(expr);
        }

        /// <summary>
        /// Holds all the non-void functions. There are some default functions, but you can add your
        /// own through the <see cref="Functions.addFunction"/> static method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> functions = new Dictionary<string, Delegate>
        {
            {"length", new Func<DynamicList, Variable>(lengthFunction)},
            {"swap", new Func<DynamicList, Integer, Integer, Variable>(swapFunction)},
            {"copy_list", new Func<DynamicList, Variable>(copyListFunction)},
            {"return", new Func<string, Variable>(returnFunction)}
        };

        /// <summary>
        /// Add your own functions to the <see cref="Functions.functions"/> dictionary. If the function
        /// already exists in it, please use the <see cref="Functions.addFunctionOverwrite"/> method instead
        /// </summary>
        /// <param name="function_name"> function name to add (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addFunction(string function_name, Delegate function)
        {
            Debugging.assert(!functions.ContainsKey(function_name));
            functions.Add(function_name, function);
        }

        /// <summary>
        /// Overwrite default functions or your own functions using this method. The function has to exist
        /// in the <see cref="Functions.functions"/> dictionary.
        /// <para/>You cannot overwrite the <see cref="Functions.returnFunction"/> function
        /// </summary>
        /// <param name="function_name"> function name to overwrite (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addFunctionOverwrite(string function_name, Delegate function)
        {
            assertFunctionExists(function_name);
            functions[function_name] = function;
        }

        /// <summary>
        /// Call a function from the <see cref="Functions.functions"/> dictionary. This can be a default
        /// function or a custom function, which you added with <see cref="addFunction"/> or overwrote using
        /// <see cref="addFunctionOverwrite"/>
        /// </summary>
        /// <param name="name"> The function name (key)</param>
        /// <param name="args"> The arguments for you function</param>
        /// <returns></returns>
        public static Variable callFunctionByName(string name, params object[] args)
        {
            Debugging.assert(functions.ContainsKey(name));
            return (Variable) functions[name].DynamicInvoke(args);
        }

        /// <summary>
        /// Use this everytime you want to access a function in the <see cref="Functions.functions"/> dictionary by its name (key)
        /// </summary>
        /// <param name="function_name"></param>
        public static void assertFunctionExists(string function_name) => Debugging.assert(functions.ContainsKey(function_name));
    }
}