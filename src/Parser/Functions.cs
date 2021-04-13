using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

// ReSharper disable RedundantExplicitArrayCreation

namespace Parser
{
    /// <summary>
    /// User-defined functions
    /// </summary>
    public class Function
    {
        /// <summary>
        /// Name by which it is called
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// Return type (as a string)
        /// </summary>
        private readonly string _type;
        /// <summary>
        /// List of variables which will be accessible inside the function. Only holds their names
        /// </summary>
        public readonly List<string> func_args;
        /// <summary>
        /// The instructions inside the function. Same usage as in the <see cref="Algorithm"/> class
        /// </summary>
        private readonly List<Instruction> _instructions;
        /// <summary>
        /// Are we in the function ?
        /// </summary>
        private bool _in_function_scope; // = false;
        /// <summary>
        /// Is the function recursive ?
        /// </summary>
        private readonly bool _rec_function;
        /// <summary>
        /// For recursive functions. Length of the call implicit stack
        /// </summary>
        private int _call_depth;

        /// <summary>
        /// Create a new <see cref="Function"/>
        /// </summary>
        /// <param name="name"> Name by which it will be called in the code</param>
        /// <param name="type"> Return type</param>
        /// <param name="func_args"> List of the variable argument names</param>
        /// <param name="instructions"> List of instructions defining the content of the function</param>
        /// <param name="rec_function"> Is the function recursive ?</param>
        public Function(string name, string type, List<string> func_args, List<Instruction> instructions, bool rec_function)
        {
            _name = name;
            _type = type;
            this.func_args = func_args;
            _instructions = instructions;
            _rec_function = rec_function;
            _call_depth = 0;
        }

        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> Name of the function</returns>
        public string getName() => _name;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> Type of the function</returns>
        public string getType() => _type;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> Is the function recursive ?</returns>
        public bool isRec() => _rec_function;

        /// <summary>
        /// Should be called BEFORE executing the <see cref="Function"/>.
        /// Initializes a new Main Context Stack, as well as a new Local Context Stack with the given variables
        /// </summary>
        /// <param name="args"> Given variables. Accessible in the Context Stack</param>
        /// <exception cref="Exception"> If not a recursive function, already executing this very function</exception>
        private void initialize(Dictionary<string, Variable> args)
        {
            _call_depth++;
            Debugging.print("calling function with depth: ", _call_depth);
            // Debugging.assert(Global.getSetting("flame mode") || Context.isFrozen());
            if (!_rec_function && _in_function_scope) throw Global.aquilaError("Already in function scope. Missing \"recursive\" keyword ?");// recursive ?
            
            // new local context scope using custom function args
            Global.newLocalContextScope(args);
            
            _in_function_scope = true;
        }

        /// <summary>
        /// Call the function with input parameters (args)
        /// </summary>
        /// <param name="args"> The variables defining the new Main Context Stack</param>
        /// <returns> The return value of the function</returns>
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
                    if (!(out_exception.InnerException is AquilaControlFlowExceptions.ReturnValueException)) throw;
                    AquilaControlFlowExceptions.ReturnValueException return_value_exception = out_exception.InnerException as AquilaControlFlowExceptions.ReturnValueException;
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

        /// <summary>
        /// Restore the Context at the end of the function
        /// </summary>
        /// <exception cref="Exception"> The Context (this method has already been called) has already been restored</exception>
        private void restore()
        {
            _call_depth--;
            if (!_rec_function && !_in_function_scope) throw new Exception("Not in function scope.");//" Missing \"recursive\" keyword ?");
            // Debugging.assert(Global.getSetting("flame mode") || Context.isFrozen());

            Global.resetLocalContextScope();

            _in_function_scope = false;
        }

        public dynamic[] translatorInfo() => new dynamic[] { _name, _type, func_args, _instructions, _rec_function };
    }
    
    /// <summary>
    /// The <see cref="Functions"/> lets you define new value_functions and use
    /// them in your code. You can only add value_functions that return a value here.
    /// <para/>See <see cref="VoidFunctionCall"/> for value_functions that are cannot be
    /// thought of like an <see cref="Expression"/>
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// Convert a list of <see cref="RawInstruction"/>s into a <see cref="Function"/>
        /// </summary>
        /// <param name="declaration_line"> First line, recursion, name, arguments</param>
        /// <param name="function_declaration"> The lines defining the function content</param>
        /// <returns> the corresponding <see cref="Function"/> object</returns>
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
                function_args[i] = StringUtils.normalizeWhiteSpaces(function_args[i]);
            }

            if (function_args.Count == 1 && function_args[0] == "") function_args.Clear();

            Debugging.print("args args: " + function_args);

            // Instructions
            List<Instruction> instr_list = function_declaration.Select(raw_instruction => raw_instruction.toInstr()).ToList();

            return new Function(function_name, type_str, function_args, instr_list, resp);
        }
        
        /// <summary>
        /// Add a <see cref="Function"/> to the <see cref="user_functions"/> dict
        /// </summary>
        /// <param name="func"> <see cref="Function"/> object</param>
        public static void addUserFunction(Function func)
        {
            user_functions.Add(func.getName(), func);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
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
        /// <param name="list_expr"> The target <see cref="DynamicList"/></param>
        /// <returns> <see cref="Integer"/> which has the list's length as value</returns>
        private static Variable lengthFunction(Expression list_expr)
        {
            // evaluate every expression
            DynamicList list = list_expr.evaluate() as DynamicList;
            // length
            return list.length();
        }

        /// <summary>
        /// Get the value at the index in a list
        /// </summary>
        /// <param name="list_expr"> The list</param>
        /// <param name="index_list_expr"> The index</param>
        /// <returns></returns>
        private static Variable listAtFunction(Expression list_expr, Expression index_list_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // extract index
            Variable index_var = index_list_expr.evaluate();
            Debugging.assert(index_var is DynamicList); // TypeError
            DynamicList index_list = index_var as DynamicList;
            // access at index
            if (list_var.isTraced())
            {
                Tracer.printTrace("list_at last event: " + list_var.tracer.peekEvent());
                handleValueFunctionTracing("list_at", list,
                    new dynamic[] {index_list.getValue()});
                Tracer.printTrace("list_at last event: " + list_var.tracer.peekEvent());
            }

            return list.atIndexList(index_list);
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

        /// <summary>
        /// Generate a random int
        /// </summary>
        /// <returns> A random int</returns>
        private static Variable randomNumber()
        {
            int rand = new Random().Next();
            return new Integer(rand);
        }

        /// <summary>
        /// Calculate the square root of a float
        /// </summary>
        /// <param name="expr"> The float</param>
        /// <returns> The sqrt of the input float</returns>
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
        /// <exception cref="AquilaControlFlowExceptions.ReturnValueException"> Exception raised to stop the <see cref="Algorithm"/> from executing any further instructions</exception>
        private static NullVar returnFunction(Expression expr) // Variable return type only for the callFunctionByName compatibility
        {
            // this Exception will stop the algorithm/function from executing
            throw new AquilaControlFlowExceptions.ReturnValueException(expr.expr);
        }

        /// <summary>
        /// Break the program flow
        /// </summary>
        /// <returns> An Error is thrown in this method</returns>
        /// <exception cref="AquilaControlFlowExceptions.BreakException"> Break the program flow</exception>
        private static NullVar breakFunction()
        {
            // this function will stop the loop from executing
            throw new AquilaControlFlowExceptions.BreakException();
        }

        /// <summary>
        /// Continue in the program flow
        /// </summary>
        /// <returns> An Error is thrown in this method</returns>
        /// <exception cref="AquilaControlFlowExceptions.ContinueException"> Continue in the program flow</exception>
        private static NullVar continueFunction()
        {
            // this function will stop the current loop iteration and pass to the next, if there is any
            throw new AquilaControlFlowExceptions.ContinueException();
        }

        /// <summary>
        /// Call an interactive-mode function
        /// </summary>
        /// <param name="expr"> Input expression holding the call</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
	    private static NullVar interactiveCallFunction(Expression expr)
	    {
		    Interpreter.processInterpreterInput(expr.expr);
		    return new NullVar();
	    }

        /// <summary>
        /// Prints the value of an <see cref="Expression"/> to the stdout. Doesn't add a return '\n' symbol
        /// </summary>
        /// <param name="value"> Expression you want to print (the evaluated value)</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printValFunction(Expression value)
        {
            Debugging.print("begin printing to console: (" + value.expr + ")");
            Global.stdoutWrite(value.evaluate().ToString());
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Prints the value of an <see cref="Expression"/> to the stdout. Does add a return '\n' symbol
        /// </summary>
        /// <param name="value"> Expression you want to print (the evaluated value)</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar printValEndlFunction(Expression value)
        {
            Debugging.print("begin printing to console: (" + value.expr + ")");
            Global.stdoutWriteLine(value.evaluate().ToString());
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
            Global.stdoutWrite(expr.expr);
            Global.stdoutWrite('\n');
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
            Global.stdoutWrite('\n');
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
            Global.stdoutWrite(expr.expr);
            Debugging.print("end printing to console");

            return new NullVar();
        }

        /// <summary>
        /// Remove a <see cref="Variable"/> and its references
        /// </summary>
        /// <param name="expr"> <see cref="Expression"/> resulting in a <see cref="Variable"/></param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar deleteVarFunction(Expression expr)
        {
            // evaluate every expression
            Variable variable = expr.evaluate();
            string var_name = variable.getName();
            // delete var
            Debugging.assert(Global.variableExistsInCurrentScope(var_name),
                new AquilaExceptions.NameError($"Variable name \"{var_name}\" does not exist in the current Context")); // NameError
            // remove the Tracer if is traced
            if (variable.isTraced())
            {
                // remove from the usable variables
                Global.usable_variables.Remove(var_name);
                // Deletion Alteration
                var alter = new Alteration("delete_var", variable, null, new dynamic[] {});
                // Update the tracer with the death event
                variable.tracer.update(new Event(alter));
                // Remove the tracer
                Global.var_tracers.Remove(variable.tracer);
            }
            // remove from the dict
            Global.getCurrentDict().Remove(var_name);

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

        /// <summary>
        /// Insert the given value in the list at the given index
        /// </summary>
        /// <param name="list_expr"> The list</param>
        /// <param name="index_expr"> The index</param>
        /// <param name="value_expr"> The value</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar insertValueAt(Expression list_expr, Expression index_expr, Expression value_expr)
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
            Variable var_ = value_expr.evaluate();
            // insert
            list.insertValue(var_, index);
            return new NullVar();
        }

        /// <summary>
        /// Append a value to the end of a list
        /// </summary>
        /// <param name="list_expr"> The list</param>
        /// <param name="value_expr"> The value</param>
        /// <returns> <see cref="NullVar"/> (equivalent of null/void)</returns>
        private static NullVar appendValue(Expression list_expr, Expression value_expr)
        {
            // extract list
            Variable list_var = list_expr.evaluate();
            Debugging.assert(list_var is DynamicList); // TypeError
            DynamicList list = list_var as DynamicList;
            // index
            Integer index = list.length();
            Expression index_expr = new Expression(index.getValue().ToString()); // this should definitely be ok
            // insert
            return insertValueAt(list_expr, index_expr, value_expr);
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
                    new Alteration("swap", list, list.getRawValue(), new dynamic[] {a.getRawValue(), b.getRawValue()})));

            return new NullVar();
        }
        
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Convert a <see cref="FloatVar"/> into and <see cref="Integer"/>
        /// </summary>
        /// <param name="expr"> <see cref="Expression"/> resulting in a <see cref="float"/> value</param>
        /// <returns> The new <see cref="Integer"/></returns>
        /// <exception cref="Exception"> The value is not a <see cref="float"/></exception>
        private static Variable float2intFunction(Expression expr)
        {
            dynamic value = expr.evaluate().getValue();
            if (!(value is float)) throw Global.aquilaError($"Type should be float but is {value.GetType()}");
            try
            {
                // ReSharper disable once PossibleInvalidCastException
                return new Integer((int) value);
            }
            catch (InvalidCastException)
            {
                throw Global.aquilaError("The cast did not succeed (float to int)");
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Convert an <see cref="Integer"/> into and <see cref="FloatVar"/>
        /// </summary>
        /// <param name="expr"> <see cref="Expression"/> resulting in an <see cref="int"/></param>
        /// <returns> The new <see cref="FloatVar"/></returns>
        /// <exception cref="Exception"> The value is not an <see cref="int"/></exception>
        private static Variable int2floatFunction(Expression expr)
        {
            dynamic value = expr.evaluate().getValue();
            if (!(value is int)) throw Global.aquilaError($"Type should be int but is {value.GetType()}");
            // ReSharper disable once PossibleInvalidCastException
            return new FloatVar((float) value);
        }

        /// <summary>
        /// Holds all the pre-defined (default) functions. This is basically the standard library of Aquila
        /// </summary>
        public static readonly Dictionary<string, Delegate> functions_dict = new Dictionary<string, Delegate>
        {
            // Value functions
            {"length", new Func<Expression, Variable>(lengthFunction)},
            {"list_at", new Func<Expression, Expression, Variable>(listAtFunction)},
            {"copy_list", new Func<Expression, Variable>(copyListFunction)},
            {"float2int", new Func<Expression, Variable>(float2intFunction)},
            {"int2float", new Func<Expression, Variable>(int2floatFunction)},
            {"random", new Func<Variable>(randomNumber)},
            {"sqrt", new Func<Expression, Variable>(sqrtFunction)},
            // Void functions
            {"return", new Func<Expression, NullVar>(returnFunction)}, // NullVar is equivalent to void, null or none
            {"break", new Func<NullVar>(breakFunction)},
            {"continue", new Func<NullVar>(continueFunction)},
            {"interactive_call", new Func<Expression, NullVar>(interactiveCallFunction)},
            {"print_value", new Func<Expression, NullVar>(printValFunction)},
            {"print_value_endl", new Func<Expression, NullVar>(printValEndlFunction)},
            {"print_str", new Func<Expression, NullVar>(printStrFunction)},
            {"print_str_endl", new Func<Expression, NullVar>(printStrEndlFunction)},
            {"print_endl", new Func<NullVar>(printEndlFunction)},
            {"delete_var", new Func<Expression, NullVar>(deleteVarFunction)},
            {"delete_value_at", new Func<Expression, Expression, NullVar>(deleteValueAt)},
            {"insert_value_at", new Func<Expression, Expression, Expression, NullVar>(insertValueAt)},
            {"append_value", new Func<Expression, Expression, NullVar>(appendValue)},
            {"swap", new Func<Expression, Expression, Expression, NullVar>(swapFunction)},
        };

        /// <summary>
        /// The dictionary holding all the user-defined functions
        /// </summary>
        public static readonly Dictionary<string, Function> user_functions = new Dictionary<string, Function>();

        /// <summary>
        /// Call a function from the <see cref="functions_dict"/> or <see cref="functions_dict"/> dictionary.
        /// This can be a default function or a custom function
        /// </summary>
        /// <param name="name"> The function name (key)</param>
        /// <param name="args"> The arguments for you function</param>
        /// <returns></returns>
        public static Variable callFunctionByName(string name, params object[] args)
        {
            if (functions_dict.ContainsKey(name))
            {
                // no new context scope needed, because no function defined here would benefit from it. plus, wouldn't it break some functionalities ? idk
                
                Context.assertStatus(Context.StatusEnum.predefined_function_call);
                Debugging.print("invoking value function ", name, " dynamically with ", args.Length, " argument(s)");
                return functions_dict[name].DynamicInvoke(args) as Variable;
            }
            if (user_functions.ContainsKey(name))
            {
                Debugging.print("calling user function: " + name);
                Dictionary<string, Variable> arg_dict = args2Dict(name, args);
                
                // user-functions: should not be frozen
                // bool unfreeze = Context.tryFreeze();
                Global.newMainContextScope();
                Variable result = user_functions[name].callFunction(arg_dict);
                Global.resetMainContextScope();
                // if (unfreeze) Context.unfreeze();
                
                return result;
            }

            throw Global.aquilaError(); // UnknownFunctionNameException
        }

        /// <summary>
        /// Does the function exist ? (pre-defined or user-defined)
        /// </summary>
        /// <param name="function_name"> Function name</param>
        /// <returns> Does the function exist ?</returns>
        public static bool functionExists(string function_name) => functions_dict.ContainsKey(function_name) ||
                user_functions.ContainsKey(function_name);

        /// <summary>
        /// Does the function exists in the <see cref="functions_dict"/> or in the <see cref="user_functions"/> dict ?
        /// </summary>
        /// <param name="function_name"> Boolean value describing the function's existence</param>
        public static void assertFunctionExists(string function_name) =>
            Debugging.assert(functionExists(function_name),
                new AquilaExceptions.FunctionNameError($"The function \"{function_name}\" does not exist"));

        /// <summary>
        /// If a function is traced, we first want to wait for its execution to finish (so it can change the
        /// input variables). That is why we want to leave the call <see cref="Event"/> pending, until the next instruction loop update
        /// (which is done through <see cref="Tracer.updateTracers"/>)
        /// </summary>
        /// <param name="name"> Name of the called function</param>
        /// <param name="affected"> The affected <see cref="Variable"/> object</param>
        /// <param name="minor_values"> The other function arguments (apart from the variable). The order must be known</param>
        private static void handleValueFunctionTracing(string name, Variable affected, dynamic[] minor_values)
        {
            Tracer.printTrace($"checking all the {Global.func_tracers.Count} total function tracers for {name}");
            foreach (FuncTracer tracer in Global.func_tracers.Where(tracer => name == tracer.traced_func))
            {
                Tracer.printTrace("found traced function " + name);
                tracer.awaitTrace(new Event(new Alteration(name, affected, null, minor_values)));
                return;
            }
        }
    }
}
