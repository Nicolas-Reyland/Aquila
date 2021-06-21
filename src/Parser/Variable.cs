using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

// ReSharper disable RedundantExplicitArrayCreation

namespace Parser
{
    /// <summary>
    /// The <see cref="Variable"/> abstract class defines all the Variables types
    /// in Aquila. It can be used to store variables from the code, as well as to
    /// designate some value (e.g. "1" or "true"), which is calculated using
    /// the <see cref="Expression.parse"/> method
    /// </summary>
    public abstract class Variable
    {
        // attributes
        /// <summary>
        /// Variable name
        /// </summary>
        private string _name;
        /// <summary>
        /// Is the variable a constant ?
        /// </summary>
        protected bool is_const;
        /// <summary>
        /// Has the variable be assigned a value or has it only be declared ?
        /// </summary>
        public bool assigned;
        /// <summary>
        /// If the variable is traced by a <see cref="VarTracer"/>, this should be
        /// its <see cref="Tracer"/>. It only has a <see cref="Tracer"/> attached to
        /// it if the <see cref="startTracing"/> method has been called. You can know
        /// if a <see cref="Variable"/> is traced by using the <see cref="isTraced"/> method
        /// </summary>
        public VarTracer tracer;
        /// <summary>
        /// is the variable traced
        /// </summary>
        private bool _traced;
        /// <summary>
        /// The tracing mode (c.f. Alteration.mode)
        /// </summary>
        public string trace_mode = "variable";

        // getters
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> the name</returns>
        public string getName() => _name;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> is the variable a constant</returns>
        public bool isConst() => is_const;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> is the <see cref="Variable"/> traced ?</returns>
        public bool isTraced() => _traced;
        /// <summary>
        /// If the <see cref="Variable"/> is an <see cref="Integer"/>, returns "int".
        /// If it is a <see cref="DynamicList"/>, returns "list", etc.
        /// </summary>
        /// <returns> Variable type as string</returns>
        public abstract string getTypeString();
        /// <summary>
        /// Create a <see cref="Variable"/> of the same type as itself.
        /// <para/>For example:
        /// If this method is called on an instance of the <see cref="Integer"/> class,
        /// the value should be an int (Int32 in C#). It will return a new
        /// <see cref="Integer"/> having the value of the input value.
        /// </summary>
        /// <param name="value"> internal value of the new <see cref="Variable"/></param>
        /// <returns> new <see cref="Variable"/> sub-class</returns>
        public abstract Variable cloneTypeToVal(dynamic value);
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> real internal value of the class (int, float, List, etc.)</returns>
        public abstract dynamic getValue();
        /// <summary>
        /// Get the "raw" value. This differs with the <see cref="getValue"/> method for <see cref="DynamicList"/>s.
        /// In fact, instead of returning a List of <see cref="Variable"/>s, this method would return
        /// a list of Lists, ints, etc.
        /// </summary>
        /// <returns> real, "raw" value</returns>
        public virtual dynamic getRawValue() => getValue(); //! comments
        /// <summary>
        /// explicit naming
        /// </summary>
        public void assign() => assigned = true;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <param name="new_name"> new string Name</param>
        public void setName(string new_name) => _name = new_name;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <param name="other"> new Value for the class</param>
        public abstract void setValue(Variable other);
        /// <summary>
        /// Force the variable to get a new value. Disables tracing for this step
        /// </summary>
        /// <param name="value"> new internal value for the variable</param>
        public abstract void forceSetValue(dynamic value);
        /// <summary>
        /// Set the variable to a constant variable. Cannot be undone
        /// </summary>
        public void setConst() => is_const = true;

        // methods
        /// <summary>
        /// Attach a <see cref="VarTracer"/> to the variable
        /// </summary>
        public void startTracing()
        {
            Tracer.printTrace("enabling tracing for " + _name);
            tracer = new VarTracer(this);
            Global.var_tracers.Add(tracer);
            _traced = true;
        }
        /// <summary>
        /// Trace the variable value <see cref="Alteration"/>.
        /// This is done manually in <see cref="FunctionCall"/> to prevent from infinite recursive calls
        /// </summary>
        /// <param name="info_name"> name of the method</param>
        /// <param name="sub_values"> sub_values are e.g. function parameters</param>
        /// <param name="check"> force checking for different value in ?</param>
        protected void trace(string info_name, dynamic[] sub_values, bool check = false)
        {
            if (!_traced) return;
            if (check)
            {
                Debugging.print("checking if any values have changed");
                if (tracer.peekValue() == getValue())
                {
                    Debugging.print("values equal. no updating done");
                    return;
                }
            }
            tracer.update(new Event(new Alteration(info_name, this, getRawValue(), sub_values, trace_mode)));
        }
        /// <summary>
        /// Has the input variable the same type as the current variable.
        /// For example: the current variable could be an <see cref="Integer"/>.
        /// If the input <see cref="Variable"/>
        /// is a <see cref="FloatVar"/>, the return value would be false
        /// </summary>
        /// <param name="other"> input <see cref="Variable"/></param>
        /// <returns> is it an instance of the same sub class</returns>
        public abstract bool hasSameParent(Variable other);
        /// <summary>
        /// Before using the variable and its value, check if the variable has
        /// been assigned to a value (and not only declared)
        /// </summary>
        /// <exception cref="AquilaExceptions.InvalidUsageException"> Variable is not assigned</exception>
        internal void assertAssignment()
        {
            if (!assigned) throw new AquilaExceptions.InvalidUsageException($"The variable \"{_name}\" is not assigned but its value is accessed"); // AssignmentError
        }
        /// <summary>
        /// Compare two variables of the same type. If they
        /// are equal, returns 0. If they can be compared (floats, ints),
        /// returns 1 if greater, -1 if smaller. If they can't be compared
        /// in size or value in a logical way (lists, bools), returns -1
        /// if they differ.
        /// </summary>
        /// <param name="other"> other variable to compare to</param>
        /// <returns> -1 (less or different), 0 (equal), 1 (greater)</returns>
        public abstract int compare(Variable other);
        /// <summary>
        /// Returns a <see cref="Variable"/> instance, corresponding to the value of "o"
        /// </summary>
        /// <param name="o"> value to interpret as a <see cref="Variable"/></param>
        /// <returns> corresponding <see cref="Variable"/></returns>
        /// <exception cref="AquilaExceptions.RuntimeError"> Raw type not supported</exception>
        public static Variable fromRawValue(dynamic o)
        {
            if (o is int) return new Integer(o);
            if (o is float) return new FloatVar(o);
            if (o is bool) return new BooleanVar(o);
            if (o is List<dynamic>) return new DynamicList(DynamicList.valueFromRawList(o));
            throw new AquilaExceptions.RuntimeError($"Raw type \"{o.GetType()}\" not supported");
        }
    }

    /// <summary>
    /// The <see cref="NullVar"/> represents the equivalent of the C# "null".
    /// It is a valid function return-type
    /// </summary>
    public class NullVar : Variable
    {
        /// <summary>
        /// Should never be called on a <see cref="NullVar"/>
        /// </summary>
        /// <param name="value"> Value</param>
        /// <returns> Error thrown</returns>
        public override Variable cloneTypeToVal(dynamic value) =>
            throw new AquilaExceptions.InvalidUsageException("Cannot use the null var this way");

        /// <summary>
        /// Should never be called on a <see cref="NullVar"/>
        /// </summary>
        /// <returns> Error thrown</returns>
        public override dynamic getValue() => throw new AquilaExceptions.InvalidUsageException("Cannot use the null var this way"); // RuntimeError (never supposed to happen whatsoever)

        /// <summary>
        /// Should never be called on a <see cref="NullVar"/>
        /// </summary>
        /// <param name="other"> Other value</param>
        public override void setValue(Variable other) => throw new AquilaExceptions.InvalidUsageException("Cannot use the null var this way"); // RuntimeError (never supposed to happen whatsoever)

        /// <summary>
        /// Should never be called on a <see cref="NullVar"/>
        /// </summary>
        /// <param name="value"> Value</param>
        public override void forceSetValue(dynamic value) => throw new AquilaExceptions.InvalidUsageException("Cannot use the null var this way");

        /// <summary>
        /// We are considering that every <see cref="Variable"/> is also a <see cref="NullVar"/>.
        /// Therefore this method is always returning true.
        /// </summary>
        /// <param name="other"> The other <see cref="Variable"/></param>
        /// <returns> true</returns>
        public override bool hasSameParent(Variable other) => true;

        /// <summary>
        /// Returns "null"
        /// </summary>
        /// <returns> "null"</returns>
        public override string getTypeString() => StringConstants.Types.NULL_TYPE; // RuntimeError (never supposed to happen whatsoever)

        /// <summary>
        /// Returns "none"
        /// </summary>
        /// <returns> "none"</returns>
        public override string ToString() => "none";

        /// <summary>
        /// Should never be called on a <see cref="NullVar"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override int compare(Variable other) => throw new AquilaExceptions.InvalidUsageException("Cannot use the null var this way");
    }

    public sealed class BooleanVar : Variable
    {
        private bool _bool_value;

        public BooleanVar(bool bool_value, bool is_const = false)
        {
            assigned = true;
            _bool_value = bool_value;
            this.is_const = is_const;
        }

        public BooleanVar not()
        {
            assertAssignment();
            return new BooleanVar(!_bool_value, is_const);
        }

        public BooleanVar or(BooleanVar other)
        {
            assertAssignment();
            // lazy or
            return _bool_value ? new BooleanVar(true) : new BooleanVar(_bool_value || other.getValue(), is_const && other.is_const);
        }

        public BooleanVar xor(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(_bool_value ^ other.getValue(), is_const && other.is_const);
        }

        public BooleanVar and(BooleanVar other)
        {
            assertAssignment();
            // lazy and
            return _bool_value
                ? new BooleanVar(_bool_value && other.getValue(), is_const && other.is_const)
                : new BooleanVar(false, is_const && other.is_const);
        }

        public override Variable cloneTypeToVal(dynamic value) => new BooleanVar(value);

        public override dynamic getValue() => assigned ? _bool_value : throw new AquilaExceptions.InvalidUsageException("Unassigned variable");

        public override void setValue(Variable other)
        {
            if (!assigned) assign();
            _bool_value = other.getValue();
            trace("setValue", new dynamic[] { other.getValue() });
        }

        public override void forceSetValue(dynamic value) => _bool_value = (bool) value;

        public override string ToString()
        {
            if (Global.getSetting("debug") || Global.getSetting("trace debug")) return "Boolean (" + (_bool_value ? "true" : "false") + ")";
            return _bool_value ? "true" : "false"; // _bool_value.ToString() capitalizes "true" and "false"
        }

        public override bool hasSameParent(Variable other_value) => other_value is BooleanVar || other_value is NullVar;

        public override int compare(Variable other) => other.getValue() == _bool_value ? 0 : -1;

        public override string getTypeString() => StringConstants.Types.BOOl_TYPE;
    }

    public abstract class NumericalValue : Variable
    {
        protected dynamic numeric_value;
        private readonly Type _val_type;
        /// <summary>
        /// For test mode only. Tracks the variables that were arithmetically implicated
        /// to create this variable.
        /// </summary>
        public Dictionary<string, NumericalValue> source_vars;

        protected NumericalValue(dynamic numeric_value, Dictionary<string, NumericalValue> source_vars)
        {
            this.numeric_value = numeric_value;
            this.source_vars = source_vars ?? new Dictionary<string, NumericalValue>();
            //Debugging.print($"source_vars count: {this.source_vars.Count}");
            // ReSharper disable once ConvertIfStatementToSwitchExpression
            if (this.numeric_value is int) _val_type = typeof(Integer);
            if (this.numeric_value is float) _val_type = typeof(FloatVar);
        }
        
        public override dynamic getValue() => assigned ? numeric_value : throw new AquilaExceptions.InvalidUsageException("Unassigned variable");
        
        public override void setValue(Variable other)
        {
            if (!assigned) assign();
            dynamic other_value = other.getValue();
            numeric_value = other_value;
            trace("setValue", new dynamic[] { other_value });
        }

        public override void forceSetValue(dynamic value) => numeric_value = value;

        public NumericalValue getTracedSource() =>
            source_vars.Where(pair => pair.Value?.isTraced() ?? false).Select(pair => pair.Value).FirstOrDefault();

        protected static Dictionary<string, NumericalValue> mergeDictionaries(Dictionary<string, NumericalValue> dict1,
            Dictionary<string, NumericalValue> dict2)
        {
            var dict3 = new Dictionary<string, NumericalValue>();
            // this.source_vars
            foreach (var pair in dict1.Where(pair => !string.IsNullOrEmpty(pair.Key) && !dict3.ContainsKey(pair.Key)))
            {
                dict3.Add(pair.Key, pair.Value);
            }
            // other.source_vars
            foreach (var pair in dict2.Where(pair => !string.IsNullOrEmpty(pair.Key) && !dict3.ContainsKey(pair.Key)))
            {
                dict3.Add(pair.Key, pair.Value);
            }

            return dict3;
        }
        
        public NumericalValue addition(NumericalValue other)
        {
            assertAssignment();
            //
            dynamic result_raw_value = numeric_value + other.getValue();
            bool result_is_const = is_const && other.isConst();
            // source variables
            var source_variables = new Dictionary<string, NumericalValue>();
            if (!string.IsNullOrEmpty(getName())) source_variables.Add(getName(), this);
            if (!string.IsNullOrEmpty(other.getName()) && other.getName() != getName()) source_variables.Add(other.getName(), other);
            source_variables = mergeDictionaries(source_variables, other.source_vars);
            //
            NumericalValue result = Activator.CreateInstance(_val_type, result_raw_value, result_is_const, source_variables);// as NumericalValue;
            return result;
        }

        public NumericalValue subtraction(NumericalValue other)
        {
            assertAssignment();
            //
            dynamic result_raw_value = numeric_value - other.getValue();
            bool result_is_const = is_const && other.isConst();
            // source variables
            var source_variables = new Dictionary<string, NumericalValue>();
            if (!string.IsNullOrEmpty(getName())) source_variables.Add(getName(), this);
            if (!string.IsNullOrEmpty(other.getName()) && other.getName() != getName()) source_variables.Add(other.getName(), other);
            source_variables = mergeDictionaries(source_variables, other.source_vars);
            //
            NumericalValue result = Activator.CreateInstance(_val_type, result_raw_value, result_is_const, source_variables) as NumericalValue;
            return result;
        }

        public NumericalValue mult(NumericalValue other)
        {
            assertAssignment();
            //
            dynamic result_raw_value = numeric_value * other.getValue();
            bool result_is_const = is_const && other.isConst();
            // source variables
            var source_variables = new Dictionary<string, NumericalValue>();
            if (!string.IsNullOrEmpty(getName())) source_variables.Add(getName(), this);
            if (!string.IsNullOrEmpty(other.getName()) && other.getName() != getName()) source_variables.Add(other.getName(), other);
            source_variables = mergeDictionaries(source_variables, other.source_vars);
            //
            NumericalValue result = Activator.CreateInstance(_val_type, result_raw_value, result_is_const, source_variables) as NumericalValue;
            return result;
        }

        public NumericalValue division(NumericalValue other)
        {
            assertAssignment();
            //
            dynamic other_value = other.getValue();
            Debugging.assert(other_value != 0, new AquilaExceptions.ZeroDivisionException($"{numeric_value} / {other_value}"));
            dynamic result_raw_value = numeric_value / other_value;
            bool result_is_const = is_const && other.isConst();
            // source variables
            var source_variables = new Dictionary<string, NumericalValue>();
            if (!string.IsNullOrEmpty(getName())) source_variables.Add(getName(), this);
            if (!string.IsNullOrEmpty(other.getName()) && other.getName() != getName()) source_variables.Add(other.getName(), other);
            source_variables = mergeDictionaries(source_variables, other.source_vars);
            //
            NumericalValue result = Activator.CreateInstance(_val_type, result_raw_value, result_is_const, source_variables) as NumericalValue;
            return result;
        }
        
        public override int compare(Variable other)
        {
            Debugging.assert(hasSameParent(other));
            dynamic other_value = other.getValue();
            trace("compare", new []{other_value});
            if (numeric_value == other_value) return 0;
            if (numeric_value > other_value) return 1;
            return -1;
        }
        
        public override Variable cloneTypeToVal(dynamic value) => Activator.CreateInstance(_val_type, value);
        
        public override bool hasSameParent(Variable other) => other.GetType() == _val_type || other is NullVar;
    }

    public sealed class Integer : NumericalValue
    {
        public Integer(int val, bool is_const = false, Dictionary<string, NumericalValue> src_vars = null) : base(val, src_vars)
        {
            assigned = true;
            this.is_const = is_const;
        }

        public Integer modulo(Integer other)
        {
            assertAssignment();
            int int_result = numeric_value % other.getValue();
            // source variables
            var source_variables = new Dictionary<string, NumericalValue>();
            if (!string.IsNullOrEmpty(getName())) source_variables.Add(getName(), this);
            if (!string.IsNullOrEmpty(other.getName()) && other.getName() != getName()) source_variables.Add(other.getName(), other);
            source_variables = mergeDictionaries(source_variables, other.source_vars);
            return new Integer(int_result, is_const && other.is_const, source_variables);
        }

        public override string ToString()
        {
            if (Global.getSetting("debug") || Global.getSetting("trace debug")) return "Integer (" + numeric_value + ")";
            return numeric_value.ToString();
        }

        public override string getTypeString() => StringConstants.Types.INT_TYPE;
    }

    public sealed class FloatVar : NumericalValue
    {
        public FloatVar(float val, bool is_const = false, Dictionary<string, NumericalValue> src_vars = null) : base(val, src_vars)
        {
            assigned = true;
            this.is_const = is_const;
        }

        public override string getTypeString() => StringConstants.Types.FLOAT_TYPE;

        public override string ToString()
        {
            if (Global.getSetting("debug") || Global.getSetting("trace debug")) return "Float (" + numeric_value + ")";
            return numeric_value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class DynamicList : Variable
    {
        private List<Variable> _list;

        public DynamicList(List<Variable> values = null, bool is_const = false)
        {
            _list = values ?? new List<Variable>();
            assigned = true;
            this.is_const = is_const;
        }

        public override Variable cloneTypeToVal(dynamic value) => new DynamicList(new List<Variable>(_list));

        public override dynamic getValue() => new List<Variable>(_list);

        public override dynamic getRawValue()
        {
            List<dynamic> raw_value = new List<dynamic>();
            foreach (Variable variable in _list)
            {
                raw_value.Add(variable.getRawValue());
            }

            return raw_value;
        }

        public Integer length() => assigned ? new Integer(_list.Count) : throw new AquilaExceptions.InvalidUsageException("Unassigned variable"); // AssignmentError

        public void validateIndex(Integer index)
        {
            assertAssignment();
            Debugging.assert(index.getValue() < _list.Count); // InvalidIndexException
        }

        public Variable atIndex(Integer index)
        {
            assertAssignment();
            int i = index.getValue();
            if (i < 0) i += _list.Count;
            if (i < 0) throw new AquilaExceptions.InvalidIndexException($"Index {index} out of bounds");
            return _list.ElementAt(i);
        }

        public Variable atIndexList(DynamicList index_list)
        {
            // copy itself
            Variable sub_element = new DynamicList(getValue());
            foreach (Integer index in index_list.getValue())
            {
                // get next element
                sub_element = (sub_element as DynamicList).atIndex(index);
            }

            // return final result
            return sub_element;
        }

        public void addValue(Variable x)
        {
            if (!assigned) assign();
            _list.Add(x);
            trace("addValue", new dynamic[] { x.getRawValue() });
        }

        public void insertValue(Variable x, Integer index)
        {
            assertAssignment();
            _list.Insert(index.getValue(), x);
            trace("insertValue", new dynamic[] { index.getRawValue(), x.getRawValue() });
        }

        public void removeValue(Integer index)
        {
            assertAssignment();
            int i = index.getValue();
            if (i < 0) i += _list.Count; // if "-1" -> last element of the list
            if (i < _list.Count)
            {
                _list.RemoveAt(i);
            }
            else
            {
                throw new AquilaExceptions.InvalidIndexException($"Index {index} out of bounds");
            }
            trace("removeValue", new dynamic[] { index.getRawValue() });
        }

        public override void setValue(Variable other)
        {
            if (!assigned) assign();
            _list = other.getValue();
            trace("setValue", new dynamic[] { other.getRawValue() });
        }

        public override void forceSetValue(dynamic value)
        {
            if (value is List<Variable>)
            {
                _list = new List<Variable>(value);
            }
            else if (value is List<dynamic>)
            {
                _list = valueFromRawList(value);
            }
            else
            {
                throw new AquilaExceptions.RuntimeError($"Cannot convert type \"{value.GetType()}\" into DynamicList");
            }
        }

        public static List<Variable> valueFromRawList(IEnumerable<dynamic> dyn_list)
        {
            List<Variable> new_value = new List<Variable>();
            foreach (dynamic o in dyn_list)
            {
                new_value.Add(fromRawValue(o));
            }

            return new_value;
        }

        public override string ToString()
        {
            if (_list.Count == 0) return "[]";
            string s = _list.Aggregate("", (current, variable) => current + (variable + ", "));

            s = "[" + s.Substring(0, s.Length - 2) + "]";
            if (Global.getSetting("debug") || Global.getSetting("trace debug")) s = "List (" + s + ")";
            return s;
        }

        public override bool hasSameParent(Variable other_value) => other_value is DynamicList || other_value is NullVar;

        public override int compare(Variable other)
        {
            Debugging.assert(hasSameParent(other));
            DynamicList other_list = (DynamicList) other;
            if (length().compare(other_list.length()) != 0) return length().compare(other_list.length());

            List<Variable> other_list_value = other.getValue();

            if (_list.Where((t, i) => other_list_value[i] != t).Any())
            {
                return -1;
            }

            return 0;
        }

        public override string getTypeString() => StringConstants.Types.LIST_TYPE;
    }

    public class FunctionCall : Variable
    {
        private readonly string _function_name;
        private readonly List<Expression> _arg_expr_list;
        private bool _called;

        public FunctionCall(string function_name, List<Expression> arg_expr_list)
        {
            Functions.assertFunctionExists(function_name);

            _function_name = function_name;
            _arg_expr_list = arg_expr_list;
            assigned = true;
            is_const = false;
        }

        public Variable callFunction()
        {
            Debugging.print("calling \"" + _function_name + "\" as value");
            // manually set context
            Context.setStatus(Context.StatusEnum.predefined_function_call);
            Context.setInfo(this);
            // for rec functions:
            bool frozen_at_start = Context.isFrozen();

            _called = true;
            // from list to array of objects
            // ReSharper disable once SuggestVarOrType_Elsewhere
            object[] args = _arg_expr_list.Select(x => (object) x).ToArray();
            
            // call by name
            Variable result = Functions.callFunctionByName(_function_name, args);

            // have to trace manually here
            if (isTraced())
            {
                Tracer.printTrace("Tracing manually value function");
                tracer.update(new Event(new Alteration(_function_name, this, result, args)));
            }

            // reset Context
            if (!frozen_at_start) Context.reset();

            return result;
        }

        public override Variable cloneTypeToVal(dynamic value) => throw new AquilaExceptions.InvalidUsageException("Cannot use a function this way");

        public override dynamic getValue() => callFunction().getValue();

        public override void setValue(Variable other) =>
            throw new AquilaExceptions.InvalidUsageException("Cannot use a function this way");

        public override void forceSetValue(dynamic value) => throw new AquilaExceptions.InvalidUsageException("Cannot use a function this way");

        public bool hasBeenCalled() => _called;

        public override bool hasSameParent(Variable other_value) => other_value is FunctionCall || other_value is NullVar;

        public override int compare(Variable other) => throw new AquilaExceptions.InvalidUsageException("Cannot use a function this way");

        public override string getTypeString() => "val_func";
    }
}
