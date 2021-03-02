using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
// ReSharper disable SuggestVarOrType_SimpleTypes

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
        private bool _traced = false;
        /// <summary>
        /// For debugging and testing purposes
        /// </summary>
        public string test_data = "some random data";

        // getters
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> is the <see cref="Variable"/> traced ?</returns>
        public bool isTraced() => _traced;
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> the name</returns>
        public string getName() => _name;
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
        /// <param name="other_value"> new Value for the class</param>
        public abstract void setValue(Variable other_value);
        /// <summary>
        /// Force the variable to get a new value. Disables tracing for this step
        /// </summary>
        /// <param name="value"> new internal value for the variable</param>
        public abstract void forceSetValue(dynamic value);

        // methods
        /// <summary>
        /// Attach a <see cref="VarTracer"/> to the variable
        /// </summary>
        public void startTracing()
        {
            Debugging.print("enabling tracing for " + this._name);
            tracer = new VarTracer(this);
            Global.var_tracers.Add(tracer);
            _traced = true;
        }
        /// <summary>
        /// Trace the variable value <see cref="Alteration"/>
        /// </summary>
        /// <param name="info_name"> name of the method</param>
        /// <param name="value"> new internal value</param>
        /// <param name="sub_values"> sub_values are e.g. function parameters</param>
        /// <param name="check"> force checking for different value in ?</param>
        protected void trace(string info_name, dynamic value, dynamic[] sub_values, bool check = false)
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
            tracer.update(new Event(new Alteration(info_name, value, sub_values)));
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
        /// <exception cref="Exception"></exception>
        internal void assertAssignment()
        {
            if (!assigned) throw Global.aquilaError(); // AssignmentError
        }
    }

    public class NullVar : Variable
    {
        public override Variable cloneTypeToVal(dynamic value) => throw new NotImplementedException();

        public override dynamic getValue() => throw Global.aquilaError(); // RuntimeError (never supposed to happen whatsoever)

        public override void setValue(Variable other_value) => throw Global.aquilaError(); // RuntimeError (never supposed to happen whatsoever)

        public override void forceSetValue(dynamic value) => throw Global.aquilaError();

        public override bool hasSameParent(Variable other) => true;

        public override string getTypeString() => "null"; // RuntimeError (never supposed to happen whatsoever)

        public override string ToString() => "none";
    }

    public class BooleanVar : Variable
    {
        private bool _bool_value;

        public BooleanVar(bool bool_value)
        {
            assigned = true;
            _bool_value = bool_value;
        }

        public BooleanVar not()
        {
            assertAssignment();
            return new BooleanVar(!_bool_value);
        }

        public BooleanVar or(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(_bool_value || other.getValue());
        }

        public BooleanVar and(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(_bool_value && other.getValue());
        }

        public BooleanVar xor(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(_bool_value ^ other.getValue());
        }

        public override Variable cloneTypeToVal(dynamic value) => new BooleanVar(value);

        public override dynamic getValue() => assigned ? _bool_value : throw Global.aquilaError(); // AssignmentError

        public override void setValue(Variable other_value)
        {
            if (!assigned) assign();
            trace("setValue", getValue(), new dynamic[] { other_value.getValue() });
            _bool_value = other_value.getValue();
        }

        public override void forceSetValue(dynamic value) => _bool_value = (bool) value;

        public override string ToString()
        {
            return _bool_value ? "true" : "false"; // _bool_value.ToString() capitalizes "true" and "false"
        }

        public override bool hasSameParent(Variable other_value) => other_value is BooleanVar || other_value is NullVar;

        public override string getTypeString() => "bool";

        public virtual bool Equals(BooleanVar other) => _bool_value == other.getValue();
    }

    public class Integer : Variable
    {
        private int _int_value;

        public Integer(int val)
        {
            assigned = true;
            _int_value = val;
        }

        public override Variable cloneTypeToVal(dynamic value)=>  new Integer(value);

        public override dynamic getValue() => assigned ? _int_value : throw Global.aquilaError(); // AssignmentError

        public Integer modulo(Integer other)
        {
            assertAssignment();
            int int_result = _int_value % other.getValue();
            return new Integer(int_result);
        }

        public Integer addition(Integer other)
        {
            assertAssignment();
            return new Integer(this._int_value + other.getValue());
        }

        public Integer subtraction(Integer other)
        {
            assertAssignment();
            return new Integer(this._int_value - other.getValue());
        }

        public Integer division(Integer other)
        {
            assertAssignment();
            return new Integer(this._int_value / other.getValue());
        }

        public Integer mult(Integer other)
        {
            assertAssignment();
            return new Integer(this._int_value * other.getValue());
        }

        public int compare(Integer n1)
        {
            assertAssignment();
            if (_int_value > n1.getValue())
            {
                return 1;
            }

            if (n1.getValue() == _int_value)
            {
                return 0;
            }

            return -1;
        }

        public override void setValue(Variable other_value)
        {
            if (!assigned) assign();
            trace("setValue", getValue(), new dynamic[] { other_value.getValue() });
            _int_value = other_value.getValue();
        }

        public override void forceSetValue(dynamic value) => _int_value = (int) value;

        public override string ToString()
        {
            return _int_value.ToString();
        }

        public override bool hasSameParent(Variable other_value) => other_value is Integer || other_value is NullVar;

        public override string getTypeString() => "int";

        public virtual bool Equals(Integer other) => _int_value == other.getValue();
    }

    public class FloatVar : Variable
    {
        private float _float_value;
        public FloatVar(float val) // le "params object[] args" n'est que là pour faire tair le compilateur. A enlever
        {
            _float_value = val;
            assigned = true;
        }

        public override Variable cloneTypeToVal(dynamic value) => new FloatVar(value);

        public override dynamic getValue() => assigned ? _float_value : throw Global.aquilaError();

        public override void setValue(Variable other_value)
        {
            trace("setValue", getValue(), new dynamic[] { other_value.getValue() });
            _float_value = other_value.getValue();
        }

        public override void forceSetValue(dynamic value) => throw new NotImplementedException();

        public override bool hasSameParent(Variable other_value) => other_value is FloatVar;

        public override string getTypeString() => "float";

        public virtual bool Equals(FloatVar other) => other.getValue() == _float_value;


        public int compare(Variable other)
        {
            if (_float_value > other.getValue()) return 1;
            if (_float_value == other.getValue()) return 0;
            return -1;
        }
        
        public Variable addition(FloatVar other)
        {
            return new FloatVar(_float_value + other.getValue());
        }
        
        public Variable subtraction(FloatVar other)
        {
            return new FloatVar(_float_value  - other.getValue());
        }
        
        public Variable mult(FloatVar other)
        {
            return new FloatVar(_float_value * other.getValue());
        }
        
        public Variable division(FloatVar other)
        {
            return new FloatVar(_float_value / other.getValue());
        }

        public override string ToString()
        {
            return _float_value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class DynamicList : Variable
    {
        private List<Variable> _list;

        public DynamicList(List<Variable> values = null)
        {
            if (values == null)
            {
                assigned = false;
                _list = new List<Variable>();
            }
            else
            {
                assigned = true;
                _list = values;
            }
        }

        public override Variable cloneTypeToVal(dynamic value) => new DynamicList(new List<Variable>(_list));

        public override dynamic getValue() => new List<Variable>(_list);
        public Integer length() => assigned ? new Integer(_list.Count) : throw Global.aquilaError(); // AssignmentError

        public void validateIndex(Integer index)
        {
            assertAssignment();
            Debugging.assert(index.getValue() < _list.Count); // InvalidIndexException
        }

        public Variable atIndex(Integer index)
        {
            assertAssignment();
            return Enumerable.ElementAt(_list, index.getValue());
        }

        public void addValue(Variable x)
        {
            if (!assigned) assign();
            trace("addValue", getValue(), new dynamic[] { x.getValue() });
            _list.Add(x);
        }

        public void insertValue(Variable x, Integer index)
        {
            assertAssignment();
            trace("insertValue", getValue(), new dynamic[] { x.getValue(), index.getValue() });
            _list.Insert(index.getValue(), x);
        }

        public Variable getValueAt(Integer index)
        {
            assertAssignment();
            int i = index.getValue();
            if (i < 0) i += _list.Count;
            if (i < 0) throw Global.aquilaError();

            return _list[i];
        }

        public void removeValue(Integer index)
        {
            assertAssignment();
            trace("removeValue", getValue(), new dynamic[] { index.getValue() });
            int i = index.getValue();
            if (i < 0) i += _list.Count; // if "-1" -> last element of the list
            if (i < _list.Count)
            {
                _list.RemoveAt(i);
            }
            else
            {
                throw Global.aquilaError(); // InvalidIndexException
            }
        }

        public override void setValue(Variable other_value)
        {
            if (!assigned) assign();
            trace("setValue", getValue(), new dynamic[] { other_value.getValue() });
            _list = other_value.getValue();
        }

        public override void forceSetValue(dynamic value)
        {
            trace("setValue", getValue(), new dynamic[] { value });
            _list = new List<Variable>(value);
        }

        public override string ToString()
        {
            if (_list.Count == 0) return "[]";
            string s = "";
            foreach (Variable variable in _list)
            {
                s += variable.ToString() + ", "; // ToString() forced by Unity ?
            }

            s = "[" + s.Substring(0, s.Length - 2) + "]";
            return s;
        }

        public override bool hasSameParent(Variable other_value) => other_value is DynamicList || other_value is NullVar;

        public override string getTypeString() => "list";

        public virtual bool Equals(DynamicList other)
        {
            if (length() != other.length()) return false;
            
            List<Variable> other_list = other.getValue();

            for (int i = 0; i < _list.Count; i++)
            {
                if (other_list[i] != _list[i]) return false;
            }
            
            return true;
        }
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
        }

        public Variable call_function()
        {
            Context.setStatus(6);
            Context.setInfo(this);
            _called = true;
            //List<Variable> arg_list = _arg_expr_list.Select(x => x.evaluate()).ToList();
            object[] args = _arg_expr_list.Select(x => (object) x).ToArray();
            trace("call_function", null, args);
            Variable result = Functions.callFunctionByName(_function_name, args);
            Context.reset();
            return result;
        }

        public override Variable cloneTypeToVal(dynamic value) => throw Global.aquilaError();

        public override dynamic getValue() => call_function().getValue();

        public override void setValue(Variable other_value)
        {
            throw Global.aquilaError(); // should never set a value to a function call
        }

        public override void forceSetValue(dynamic value) => throw Global.aquilaError();

        public bool hasBeenCalled() => _called;

        public override bool hasSameParent(Variable other_value) => other_value is FunctionCall || other_value is NullVar;

        public override string getTypeString() => "func";
    }

    // Utils for Graphics & Animations
    public static class VariableUtils {
        public static DynamicList createDynamicList(List<int> list)
        {
            List<Variable> var_list = list.Select(i => new Integer(i)).Cast<Variable>().ToList();
            return new DynamicList(var_list);
        }

        public static DynamicList createDynamicList(int[] array)
        {
            List<int> list = array.ToList();
            List<Variable> var_list = list.Select(i => new Integer(i)).Cast<Variable>().ToList();
            return new DynamicList(var_list);
        }
    }
}
