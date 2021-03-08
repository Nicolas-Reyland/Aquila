using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

// ReSharper disable RedundantExplicitArrayCreation

namespace Parser
{
    public class Function
    {
        private readonly string _name;
        private Dictionary<string, Variable> _var_copy;
        private readonly Type _type;
        public readonly List<string> func_args;
        private readonly List<Instruction> _instructions;
        private bool _in_function_scope; // = false;
        public Function(string name, Type type, List<string> func_args, List<Instruction> instructions)
        {
            _name = name;
            _type = type;
            this.func_args = func_args;
            _instructions = instructions;
        }

        public string getName() => _name;

        public bool isVoid() => _type == typeof(NullVar);
        public bool isValue() => !isVoid();

        private void initialize(Dictionary<string, Variable> args)
        {
            if (_in_function_scope) throw new Exception("Already in function scope");
            _var_copy = Global.variables;
            Global.variables.Clear();
            
            foreach (KeyValuePair<string,Variable> pair in args)
            {
                // This is equivalent to a declaration & assignment
                Global.variables.Add(pair.Key, pair.Value);
            }

            if (Context.isFrozen())
            {
                Debugging.print("Context already frozen in function init: " + _name);
            }
            else
            {
                Context.setStatus(Context.StatusEnum.instruction_function_loop);
                Context.setInfo(this);
                Context.freeze();
            }
            
            _in_function_scope = true;
        }

        public Variable callFunction(Dictionary<string, Variable> args)
        {
            initialize(args);
            
            Debugging.assert(_in_function_scope);
            foreach (Instruction instruction in _instructions)
            {
                try
                {
                    instruction.execute(); // return here (not continue, nor break) 
                }
                catch (System.Reflection.TargetInvocationException out_exception)
                {
                    if (!(out_exception.InnerException is AquilaExceptions.ReturnValueException)) throw;
                    AquilaExceptions.ReturnValueException return_value_exception = out_exception.InnerException as AquilaExceptions.ReturnValueException;
                    Debugging.print("ReturnValueException was thrown");
                    string return_value_string = return_value_exception.getExprStr();
                    Expression return_value_expression = new Expression(return_value_string);
                    Variable return_value = return_value_expression.evaluate();

                    restore();
                    return return_value;
                }
            }
            
            Debugging.print("no ReturnValueException thrown. returning NullVar");
            
            restore();
            return new NullVar();
        }

        private void restore()
        {
            if (!_in_function_scope) throw new Exception("Not in function scope");

            Global.variables.Clear(); // clear function variables
            Global.variables = _var_copy; // restore the Global variables
            _var_copy.Clear(); // this will prevent from difficult-to-detect mistakes

            if (!Context.isFrozen())
            {
                Debugging.print("Context was not frozen in function restore: " + _name);
            }
            else
            {
                Context.unfreeze();
                Context.reset();
            }
            
            _in_function_scope = false;
        }
    }
    
    /// <summary>
    /// The <see cref="Functions"/> lets you define new value_functions and use
    /// them in your code. You can only add value_functions that return a value here.
    /// <para/>See <see cref="VoidFunctionCall"/> for value_functions that are cannot be
    /// thought of like an <see cref="Expression"/>
    /// </summary>
    public static class Functions
    {
        public static Function readFunction(string declaration_line, List<RawInstruction> function_declaration)
        {
            Debugging.assert(function_declaration.Count > 0); // >= 1
            
            // function type
            List<string> decl =
                StringUtils.splitStringKeepingStructureIntegrity(declaration_line, ' ', Global.base_delimiters);
            Debugging.assert(decl.Count == 3); // function type name(args)
            Debugging.assert(decl[0] == "function");
            // type
            string type_str = decl[1];
            Type type;
            switch (type_str)
            {
                case "int":
                    type = typeof(Integer);
                    break;
                case "float":
                    type = typeof(FloatVar);
                    break;
                case "bool":
                    type = typeof(BooleanVar);
                    break;
                case "list":
                    type = typeof(DynamicList);
                    break;
                case "void":
                    type = typeof(NullVar);
                    break;
                default:
                    throw Global.aquilaError(); // unknown type
            }
            // name
            Debugging.assert(decl[2].Contains("(") && decl[2].Contains(")"));
            int name_sep_index = decl[2].IndexOf('(');
            string function_name = decl[2].Substring(0, name_sep_index);
            // args
            string function_args_str = decl[2].Substring(name_sep_index + 1);
            function_args_str = function_args_str.Substring(0, function_args_str.Length - 1);
            Debugging.print("name: " + function_name);
            Debugging.print("args: " + function_args_str);
            List<string> function_args = function_args_str.Split(',').ToList();
            for (int i = 0; i < function_args.Count; i++)
            {
                function_args[i] = StringUtils.purgeLine(function_args[i]);
            }

            Debugging.print("args args: " + function_args);

            // Instructions
            List<Instruction> instr_list = function_declaration.Select(raw_instruction => raw_instruction.toInstr()).ToList();

            return new Function(function_name, type, function_args, instr_list);
        }
        
        public static void addUserFunction(Function func)
        {
            user_functions.Add(func.getName(), func);
        }

        private static Dictionary<string, Variable> args2Dict(string name, object[] args)
        {
            Dictionary<string, Variable> d = new Dictionary<string, Variable>();
            for (int i = 0; i < args.Length; i++)
            {
                Debugging.assert(args[i] is Expression);
                Variable v = (args[i] as Expression).evaluate();
                string func_name = user_functions[name].func_args[i];
                d.Add(func_name, v);
            }
            
            return d;
        }

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

        private static Variable listAtFunction(Expression list_expr, Expression index_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // extract index
            Variable index_var = index_expr.evaluate();
            Debugging.assert(index_var is Integer); // TypeError
            Integer index = index_var as Integer;
            // access at index
            if (list_var.isTraced())
            {
                Tracer.printTrace("list_at last event: " + list_var.tracer.peekEvent());
                handleValueFunctionTracing("list_at", list,
                    new dynamic[] {index.getValue()});
                Tracer.printTrace("list_at last event: " + list_var.tracer.peekEvent());
            }

            return list.atIndex(index);
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
            Variable var_ = list_expr.evaluate();
            Debugging.assert(var_ is DynamicList); // TypeError
            DynamicList list = var_ as DynamicList;
            // copy list
            List<Variable> copy = list.getValue();
            return new DynamicList(copy);
        }

        // random number function
        private static Variable randomNumber()
        {
            int rand = new Random().Next();
            return new Integer(rand);
        }





        /// <summary>
        /// Cannot be overwritten. Used to return a value in a function or the main algorithm.
        /// </summary>
        /// <param name="expr"> string value of the returned expression</param>
        /// <exception cref="AquilaExceptions.ReturnValueException"> Exception raised to stop the <see cref="Algorithm"/> from executing any further instructions</exception>
        private static NullVar returnFunction(Expression expr) // Variable return type only for the callFunctionByName compatibility
        {
            // this Exception will stop the function/algorithm from executing
            throw new AquilaExceptions.ReturnValueException(expr.expr);
        }

        /// <summary>
        /// Prints the value of an <see cref="Expression"/> to the stdout. Doesn't add a return '\n' symbol
        /// </summary>
        /// <param name="value"> Expression you want to printTrace (its evaluated value)</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printFunction(Expression value)
        {
            Debugging.print("begin printing to console: (" + value.expr + ")");
            Console.Write(value.evaluate().ToString());
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Prints a line, new-line-char to the stdout
        /// </summary>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printStrEndlFunction(Expression expr)
        {
            Debugging.print("begin printing to console");
            Console.Write(expr.expr);
            Console.Write('\n');
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
            Console.Write(expr.expr);
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


        private static NullVar deleteValueAt(Expression list_expr, Expression index_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // extract index
            Variable index_var = index_expr.evaluate();
            Debugging.assert(index_var is Integer); // TypeError
            Integer index = index_var as Integer;
            // delete
            list.removeValue(index);
            return new NullVar();
        }


        private static NullVar insertValueAt(Expression list_expr, Expression index_expr, Expression var_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // extract index
            Variable index_var = index_expr.evaluate();
            Debugging.assert(index_var is Integer);
            Integer index = index_var as Integer;
            // extract var
            Variable var_ = var_expr.evaluate();
            // insert
            list.insertValue(var_, index);
            return new NullVar();
        }

        private static NullVar appendValue(Expression list_expr, Expression var_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // index
            Integer index = list.length();
            Expression index_expr = new Expression(index.ToString());
            // insert
            return insertValueAt(list_expr, index_expr, var_expr);
        }

        /// <summary>
        /// Default function
        /// </summary>
        /// <para>
        /// Swaps the elements at index a and b in a list
        /// </para>
        /// <param name="list_expr"> the target <see cref="DynamicList"/> (as an <see cref="Expression"/>)</param>
        /// <param name="a_expr"> index of the first element</param>
        /// <param name="b_expr"> index of the second element</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar swapFunction(Expression list_expr, Expression a_expr, Expression b_expr)
        {
            // evaluate every expression
            DynamicList list = list_expr.evaluate() as DynamicList;
            Integer a = a_expr.evaluate() as Integer;
            Integer b = b_expr.evaluate() as Integer;
            // check indexes
            list.validateIndex(a);
            list.validateIndex(b);
            // extract both values
            Variable var_a = list.getValueAt(a);
            Variable var_b = list.getValueAt(b);
            // freeze the context
            Context.freeze();
            // change a
            list.removeValue(a);
            list.insertValue(var_b, a);
            // change b
            list.removeValue(b);
            list.insertValue(var_a, b);
            // unfreeze the context
            Context.unfreeze();
            // update manually (void)
            if (list.isTraced())
                list.tracer.update(new Event(
                    new Alteration("swap", list, list.getValue(), new dynamic[] {a.getValue(), b.getValue()})));

            return new NullVar();
        }

        // float2int




        // int2float

        /*
        value function:
          $x = value_function(arg1, arg2, ...)
        void function:
          void_function(arg1, arg2, ...)
        */

        /* $l = copy_list($l2)
         * $l = delete_value_at($l, 1)
         * $l = insert_value_at($l, 4, 3.1415)
         */



        /// <summary>
        /// Holds all the non-void value_functions. There are some default value_functions, but you can add your
        /// own through the <see cref="Functions.addValueFunction"/> static method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> value_functions = new Dictionary<string, Delegate>
        {
            {"length", new Func<Expression, Variable>(lengthFunction)},
            {"list_at", new Func<Expression, Expression, Variable>(listAtFunction)},
            {"copy_list", new Func<Expression, Variable>(copyListFunction)},
            {"random", new Func<Variable>(randomNumber)},
        };

        /// <summary>
        /// Holds all the non-value void_functions. There are some default void_functions, but you can add your
        /// own through the <see cref="Functions.addVoidFunction"/> static method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> void_functions = new Dictionary<string, Delegate>
        {
            {"return", new Func<Expression, NullVar>(returnFunction)}, // NullVar is equivalent of void, null or none
            {"print", new Func<Expression, NullVar>(printFunction)},
            {"print_str", new Func<Expression, NullVar>(printStrFunction)},
            {"print_str_endl", new Func<Expression, NullVar>(printStrEndlFunction)},
            {"print_endl", new Func<NullVar>(printEndlFunction)},
            {"delete_var", new Func<Expression, NullVar>(deleteVarFunction)},
            {"delete_value_at", new Func<Expression, Expression, NullVar>(deleteValueAt)},
            {"insert_value_at", new Func<Expression, Expression, Expression, NullVar>(insertValueAt)},
            {"append_value", new Func<Expression, Expression, NullVar>(appendValue)},
            {"swap", new Func<Expression, Expression, Expression, NullVar>(swapFunction)},
        };

        private static Dictionary<string, Function> user_functions = new Dictionary<string, Function>();

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
                Context.assertStatus(Context.StatusEnum.function_value_call);
                Debugging.print("invoking value function ", name, " dynamically with ", args.Length, " argument(s)");
                //handleValueFunctionTracing(name, args); // cannot use expressions, have to use variables ...
                return value_functions[name].DynamicInvoke(args) as Variable;
            }
            if (void_functions.ContainsKey(name))
            {
                Context.assertStatus(Context.StatusEnum.function_void_call);
                Debugging.print("invoking void function ", name, " dynamically with ", args.Length, " argument(s)");
                //handleValueFunctionTracing(name, args); // cannot use expressions, have to use variables ...
                return void_functions[name].DynamicInvoke(args) as Variable;
            }
            if (user_functions.ContainsKey(name))
            {
                Debugging.print("calling user function: " + name);
                Dictionary<string, Variable> arg_dict = args2Dict(name, args);
                return user_functions[name].callFunction(arg_dict);
            }

            throw Global.aquilaError(); // UnknownFunctionNameException
        }

        /// <summary>
        /// Use this everytime you want to access a function in the <see cref="value_functions"/> or <see cref="void_functions"/> dictionaries by its name (key)
        /// </summary>
        /// <param name="function_name"></param>
        public static void assertFunctionExists(string function_name) =>
            Debugging.assert(value_functions.ContainsKey(function_name) ^
                             void_functions.ContainsKey(function_name) ^
                             user_functions.ContainsKey(function_name)); // UnknownFunctionNameException

        private static void handleValueFunctionTracing(string name, Variable affected, dynamic[] minor_values)
        {
            Tracer.printTrace("checking all the " + Global.func_tracers.Count + " total function tracers for " + name);
            foreach (FuncTracer tracer in Global.func_tracers)
            {
                if (name == tracer.traced_func)
                {
                    Tracer.printTrace("found traced function " + name);
                    tracer.awaitTrace(new Event(new Alteration(name, affected, null, minor_values)));
                    return;
                }
            }
        }
    }
}
