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
        private readonly string _type;
        public readonly List<string> func_args;
        private readonly List<Instruction> _instructions;
        private bool _in_function_scope; // = false;
        private readonly bool _rec_function;
        private int _call_depth;

        public Function(string name, string type, List<string> func_args, List<Instruction> instructions, bool recFunction)
        {
            _name = name;
            _type = type;
            this.func_args = func_args;
            _instructions = instructions;
            _rec_function = recFunction;
            _call_depth = 0;
        }

        public string getName() => _name;

        public string getType() => _type;

        public bool isRec() => _rec_function;

        private void initialize(Dictionary<string, Variable> args)
        {
            _call_depth++;
            Debugging.print("calling function with depth: ", _call_depth);
            Debugging.assert(Global.getSetting("flame mode") || Context.isFrozen());
            if (!_rec_function && _in_function_scope) throw Global.aquilaError("Already in function scope. Missing \"recursive\" keyword ?");// recursive ?
            
            // new local context scope using custom function args
            Global.newLocalContextScope(args);
            
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

                    if (_type != "auto" && _type != "null") Debugging.assert(return_value.getTypeString() == _type);
                    restore();
                    return return_value;
                }
            }
            Debugging.print("no ReturnValueException thrown. returning NullVar");

            if (_type != "auto") Debugging.assert(_type == "null");
            restore();
            return new NullVar();
        }

        private void restore()
        {
            _call_depth--;
            if (!_rec_function && !_in_function_scope) throw new Exception("Not in function scope.");//" Missing \"recursive\" keyword ?");
            Debugging.assert(Global.getSetting("flame mode") || Context.isFrozen());

            Global.resetLocalContextScope();

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
            bool resp = false;
            
            // function decl
            List<string> decl =
                StringUtils.splitStringKeepingStructureIntegrity(declaration_line, ' ', Global.base_delimiters);
            if (decl.Count == 4)
            {
                // function KEYWORD type name(args)
                Debugging.print("special function. definition with count 4: " + decl[1]);
                Debugging.assert(decl[1] == "recursive"); // hardcoded.
                decl.RemoveAt(1);
                resp = true; // REPLACE WITH "rec-function", "end-rec-function"
            }
            Debugging.assert(decl.Count == 3); // function type name(args)
            Debugging.assert(decl[0] == "function");
            // type
            string type_str = decl[1];
            // name
            Debugging.assert(decl[2].Contains("(") && decl[2].Contains(")"));
            int name_sep_index = decl[2].IndexOf('(');
            string function_name = decl[2].Substring(0, name_sep_index);
            Debugging.assert(StringUtils.validObjectName(function_name)); // InvalidNamingException
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

            if (function_args.Count == 1 && function_args[0] == "") function_args.Clear();

            Debugging.print("args args: " + function_args);

            // Instructions
            List<Instruction> instr_list = function_declaration.Select(raw_instruction => raw_instruction.toInstr()).ToList();

            return new Function(function_name, type_str, function_args, instr_list, resp);
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
            var raw = new List<dynamic>(list.getRawValue());
            return Variable.fromRawValue(raw);
        }

        // random number function
        private static Variable randomNumber()
        {
            int rand = new Random().Next();
            return new Integer(rand);
        }

        private static Variable sqrtFunction(Expression expr)
        {
            Variable v = expr.evaluate();
            if (v is Integer)
            {
                int raw_int = v.getValue();
                // ReSharper disable once RedundantCast
                float raw_float = (float) raw_int;
                v = new FloatVar(raw_float);
            }
            Debugging.assert(v is FloatVar);
            double real = (double) v.getValue();
            float real_sqrt = (float) Math.Sqrt(real);
            
            return new FloatVar(real_sqrt);
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

	private static NullVar interactiveCallFunction(Expression expr)
	{
		Interpreter.processInterpreterInput(expr.expr);
		return new NullVar();
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
        /// Remove a <see cref="Variable"/> from the current variable dictionary
        /// </summary>
        /// <param name="expr"> <see cref="Expression"/> resulting in a <see cref="Variable"/> from the current variable dict</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar deleteVarFunction(Expression expr)
        {
            // evaluate every expression
            Variable variable = expr.evaluate();
            // delete var
            Debugging.assert(Global.getCurrentDict().ContainsKey(variable.getName())); // UnknownVariableNameException
            // remove the variable by key
            Global.getCurrentDict().Remove(variable.getName());

            return new NullVar();
        }

        /// <summary>
        /// Delete the nth value of a list
        /// </summary>
        /// <param name="list_expr"> expression resulting in a <see cref="DynamicList"/> variable</param>
        /// <param name="index_expr"> expression resulting in a <see cref="Integer"/> variable</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
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
            Variable var_a = list.atIndex(a);
            Variable var_b = list.atIndex(b);
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
        private static Variable float2intFunction(Expression expr)
        {
            throw new NotImplementedException();
        }

        // int2float
        private static Variable int2floatFunction(Expression expr)
        {
            throw new NotImplementedException();
        }

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
        /// own through the <see cref="addFunction"/> method.
        /// </summary>
        private static readonly Dictionary<string, Delegate> functions_dict = new Dictionary<string, Delegate>
        {
            {"length", new Func<Expression, Variable>(lengthFunction)}, // functions which return a value
            {"list_at", new Func<Expression, Expression, Variable>(listAtFunction)},
            {"copy_list", new Func<Expression, Variable>(copyListFunction)},
            //{"float2int", new Func<Expression, Variable>(float2intFunction)},
            //{"int2float", new Func<Expression, Variable>(int2floatFunction)},
            {"random", new Func<Variable>(randomNumber)},
            {"sqrt", new Func<Expression, Variable>(sqrtFunction)},

            {"return", new Func<Expression, NullVar>(returnFunction)}, // NullVar is equivalent of void, null or none
            {"interactive_call", new Func<Expression, NullVar>(interactiveCallFunction)},
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

        public static readonly Dictionary<string, Function> user_functions = new Dictionary<string, Function>();

        /// <summary>
        /// Add your own value_functions to the <see cref="functions_dict"/> dictionary. If the function
        /// already exists in it, please use the <see cref="Functions.addFunctionOverwrite"/> method instead
        /// </summary>
        /// <param name="function_name"> function name to add (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addFunction(string function_name, Delegate function)
        {
            Debugging.assert(!functions_dict.ContainsKey(function_name)); // UnknownFunctionNameException
            functions_dict.Add(function_name, function);
        }

        /// <summary>
        /// Overwrite default value_functions or your own value_functions using this method. The function has to exist
        /// in the <see cref="functions_dict"/> or <see cref="user_functions"/> dictionary.
        /// <para/>You cannot overwrite the <see cref="returnFunction"/> function
        /// </summary>
        /// <param name="function_name"> function name to overwrite (key)</param>
        /// <param name="function"> Delegate function (value)</param>
        public static void addFunctionOverwrite(string function_name, Delegate function)
        {
            assertFunctionExists(function_name); // OverwriteFunctionDoesNotExistException
            functions_dict[function_name] = function;
        }

        /// <summary>
        /// Call a function from the <see cref="functions_dict"/> or <see cref="functions_dict"/> dictionary. This can be a default
        /// function or a custom function, which you added with <see cref="addFunction"/> or
        /// overwrote using <see cref="addFunctionOverwrite"/>
        /// </summary>
        /// <param name="name"> The function name (key)</param>
        /// <param name="args"> The arguments for you function</param>
        /// <returns></returns>
        public static Variable callFunctionByName(string name, params object[] args)
        {
            if (functions_dict.ContainsKey(name))
            {
                // no new context scope needed, because no function defined here would benefit from it. plus, wouldn't it break some functionalities ?
                
                Context.assertStatus(Context.StatusEnum.predefined_function_call);
                Debugging.print("invoking value function ", name, " dynamically with ", args.Length, " argument(s)");
                //handleValueFunctionTracing(name, args); // cannot use expressions, have to use variables ...
                return functions_dict[name].DynamicInvoke(args) as Variable;
                
                //return result;
            }
            if (user_functions.ContainsKey(name))
            {
                Debugging.print("calling user function: " + name);
                Dictionary<string, Variable> arg_dict = args2Dict(name, args);
                
                bool unfreeze = Context.tryFreeze();
                Global.newMainContextScope();
                Variable result = user_functions[name].callFunction(arg_dict);
                Global.resetMainContextScope();
                if (unfreeze) Context.unfreeze();
                
                return result;
            }

            throw Global.aquilaError(); // UnknownFunctionNameException
        }

        /// <summary>
        /// Use this everytime you want to access a function in the <see cref="functions_dict"/> dictionary by its name (key)
        /// </summary>
        /// <param name="function_name"></param>
        public static void assertFunctionExists(string function_name) =>
            Debugging.assert(functions_dict.ContainsKey(function_name) ^
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
