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

        protected void assign() => assigned = true;

        protected void assertAssignment()
        {
            if (!assigned) throw Global.aquilaError();
        }
    }

    public class BooleanVar : Variable
    {
        public bool bool_value;
        
        public BooleanVar(bool bool_value)
        {
            assigned = true;
            this.bool_value = bool_value;
        }

        public BooleanVar not()
        {
            assertAssignment();
            return new BooleanVar(!bool_value);
        }

        public BooleanVar or(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(bool_value || other.getValue());
        }
        
        public BooleanVar and(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(bool_value && other.getValue());
        }
        
        public BooleanVar xor(BooleanVar other)
        {
            assertAssignment();
            return new BooleanVar(bool_value ^ other.getValue());
        }
        
        public override dynamic getValue() => assigned ? bool_value : throw Global.aquilaError();

        public override void setValue(Variable other_value)
        {
            bool_value = other_value.getValue();
            if (!assigned) assign();
        }
    }

    public class Integer : Variable
    {
        private int _int_value;
        
        public Integer(int val)
        {
            assigned = true;
            _int_value = val;
        }

        public override dynamic getValue() => assigned ? _int_value : throw Global.aquilaError();

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

        /* addition
         * soustraction (addition mais avec l'opposé)
         * mutliplication
         * division
         * modulo
         * compariason élémentaire -> renvoie -1, 0 ou 1
         * comparaisons annexes (<, ==, >, <=, >=)
        */

        // Integer x = new Integer(5);
        // Integer y = new Integer(2);
        // Integer z = x.Modulo(y);

        public override void setValue(Variable other_value)
        {
            _int_value = other_value.getValue();
            if (!assigned) assign();
        }

        public override string ToString()
        {
            return _int_value.ToString();
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
        public Integer length() => assigned ? new Integer(_list.Count) : throw Global.aquilaError();

        public void validateIndex(Integer index)
        {
            assertAssignment();
            Debugging.assert(index.getValue() < _list.Count);
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
            if (i < _list.Count)
            {
                _list.RemoveAt(i);
            }
            else
            {
                throw Global.aquilaError();
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
            string s = "";
            foreach (Variable variable in _list)
            {
                s += variable.ToString() + ", ";
            }
            
            s = "[" + s.Substring(0, s.Length - 2) + "]";
            return s;
        }
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

        public override dynamic getValue()
        {
            _called = true;
            List<Variable> arg_list = _arg_expr_list.Select(x => x.evaluate()).ToList();
            object[] args = arg_list.Select(x => (object) x).ToArray();
            return Functions.callFunctionByName(_function_name, args).getValue();
        }

        public override void setValue(Variable other_value)
        {
            throw Global.aquilaError(); // should never set a value to a function call
        }

        public bool hasBeenCalled() => _called;
    }
}