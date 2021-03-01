using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    /// <summary>
    /// This is a summary
    /// <para>this is a new paragraph (new line, etc)</para>
    /// This links to the class Expression : <see cref="Expression"/>
    /// </summary>
    public abstract class Variable
    {
        // attributes
        private string _name;
        public bool assigned;
        public VarTracer tracer;
        private bool _traced = false;
        
        public string test_data = "none"; // for testing purposes only

        // getters
        public bool isTraced() => _traced;
        public string getName() => _name;
        public abstract string getTypeString();
        public abstract Variable cloneTypeToVal(dynamic value);
        public abstract dynamic getValue();

        // setters
        public void setName(string new_name) => _name = new_name;

        // methods
        public void startTracing()
        {
            Debugging.print("enabling tracing for " + this._name);
            tracer = new VarTracer(this);
            Global.var_tracers.Add(tracer);
            _traced = true;
        }

        public void trace(string info_name, dynamic value, dynamic[] sub_values, bool check = false)
        {
            if (!_traced) return;
            if (check)
            {
                Debugging.print("checking if any values have changed");
                if (tracer.peekValue() == getValue())
                {
                    Debugging.print("values equal. no updating");
                    return;
                }
            }
            tracer.update(new Event(new Alteration(info_name, value, sub_values)));
        }

        public abstract void setValue(Variable other_value);

        public abstract void forceSetValue(dynamic value);

        public abstract bool hasSameParent(Variable other);

        public void assign() => assigned = true;

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
            return _bool_value.ToString();
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
