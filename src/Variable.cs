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

        // getters
        public string getName() => _name;
        
        // setters
        public void setName(string new_name) => _name = new_name;

        // methods
        public abstract dynamic getValue();

        public abstract void setValue(Variable other_value);

        public abstract bool hasSameParent(Variable other);

        public abstract string getTypeString();

        public void assign() => assigned = true;

        internal void assertAssignment()
        {
            if (!assigned) throw Global.aquilaError(); // AssignmentError
        }
    }

    public class NullVar : Variable
    {
        public override dynamic getValue() => throw Global.aquilaError(); // RuntimeError (never supposed to happen whatsoever)

        public override void setValue(Variable other_value) => throw Global.aquilaError(); // RuntimeError (never supposed to happen whatsoever)

        public override bool hasSameParent(Variable other) => true;

        public override string getTypeString() => throw Global.aquilaError(); // RuntimeError (never supposed to happen whatsoever)

        public override string ToString() => "none";
    }

    public class BooleanVar : Variable
    {
        private bool _bool_value;
        
        public BooleanVar(bool bool_value)
        {
            assigned = true;
            this._bool_value = bool_value;
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
        
        public override dynamic getValue() => assigned ? _bool_value : throw Global.aquilaError(); // AssignmentError

        public override void setValue(Variable other_value)
        {
            _bool_value = other_value.getValue();
            if (!assigned) assign();
        }

        public override string ToString()
        {
            return _bool_value.ToString();
        }

        public override bool hasSameParent(Variable other_value) => other_value is BooleanVar || other_value is NullVar;

        public override string getTypeString() => "bool";
    }

    public class Integer : Variable
    {
        private int _int_value;
        
        public Integer(int val)
        {
            assigned = true;
            _int_value = val;
        }

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
            _int_value = other_value.getValue();
            if (!assigned) assign();
        }

        public override string ToString()
        {
            return _int_value.ToString();
        }

        public override bool hasSameParent(Variable other_value) => other_value is Integer || other_value is NullVar;
        
        public override string getTypeString() => "int";
    }

    public class FloatVar : Variable
    {
        public FloatVar()
        {
            throw new NotImplementedException();
        }

        public override dynamic getValue()
        {
            throw new NotImplementedException();
        }

        public override void setValue(Variable other_value)
        {
            throw new NotImplementedException();
        }

        public override bool hasSameParent(Variable other_value)
        {
            throw new NotImplementedException();
        }

        public override string getTypeString()
        {
            throw new NotImplementedException();
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
        
        public override dynamic getValue() => _list;
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
            _list.Add(x);
            if (!assigned) assign();
        }
        
        public void insertValue(Variable x, Integer index)
        {
            assertAssignment();
            _list.Insert(index.getValue(), x);
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
                throw Global.aquilaError(); // InvalidIndexException
            }
        }
        
        /* insertion
         * get item -> renvoie l'élément à l'indice
         * set item -> list[indice] = nouvelle-valeur
         * del item -> list.pop(indice)
         */
        
        public override void setValue(Variable other_value)
        {
            _list = other_value.getValue();
            if (!assigned) assign();
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
    }

    public class FunctionCall : Variable
    {
        private readonly string _function_name;
        private readonly List<Expression> _arg_expr_list;
        private bool _called;
        
        public FunctionCall(string function_name, List<Expression> arg_expr_list)
        {
            _function_name = function_name;
            _arg_expr_list = arg_expr_list;

            assign();
            Functions.assertFunctionExists(function_name);
        }

        public Variable call_function()
        {
            _called = true;
            //List<Variable> arg_list = _arg_expr_list.Select(x => x.evaluate()).ToList();
            object[] args = _arg_expr_list.Select(x => (object) x).ToArray();
            Variable result = Functions.callFunctionByName(_function_name, args);
            return result;
        }

        public override dynamic getValue() => call_function().getValue();

        public override void setValue(Variable other_value)
        {
            throw Global.aquilaError(); // should never set a value to a function call
        }

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
