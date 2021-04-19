using System;
using System.Collections.Generic;

namespace Parser
{
    public class DataTree
    {
        private readonly string _request;
        private readonly List<string> _endpoints;
        private readonly Dictionary<string, DataTree> _data;
        private readonly byte _call;

        private DataTree(string request)
        {
            _call = 0;
            _request = request;
            switch (request)
            {
                case "Usable Variables":
                    _data = new Dictionary<string, DataTree>();
                    foreach (string variable in Global.usable_variables)
                    {
                        _data.Add(variable, variableDataTree(variable));
                    }

                    break;
                
                case "Tracers":
                    _data = new Dictionary<string, DataTree>
                    {
                        {"Variable Tracers", new DataTree("Variable Tracers")},
                        {"Function Tracers", new DataTree("Function Tracers")}
                    };

                    break;
                
                case "Variable Tracers":
                    _data = new Dictionary<string, DataTree>();
                    foreach (VarTracer tracer in Global.var_tracers)
                    {
                        _data.Add(tracer.getVar().getName(), varTracerDataTree(tracer));
                    }

                    break;
                
                case "Function Tracers":
                    _data = new Dictionary<string, DataTree>();
                    foreach (FuncTracer tracer in Global.func_tracers)
                    {
                        _data.Add(tracer.traced_func, funcTracerDataTree(tracer));
                    }

                    break;
                
                default:
                    throw new NotImplementedException("Unknown DataTree request: " + request);
            }
        }

        private static DataTree variableDataTree(string variable)
        {
            if (!Global.variableExistsInCurrentScope(variable))
            {
                return new DataTree(variable, null, new List<string> {$"The variable \"{variable}\" does not exist in the current scope"});
            }

            Variable v = Global.variableFromName(variable);
            string var_type = v.getTypeString();
            string value = v.ToString();
            string is_const = v.isConst().ToString();
            string is_traced = v.isTraced().ToString();

            return new DataTree(variable, null, new List<string>
            {
                "Name: " + variable,
                "Type: " + var_type,
                "Value: " + value,
                "Const: " + is_const,
                "Traced: " + is_traced
            });
        }
        
        private static DataTree variableDataTree(Variable v)
        {
            string var_type = v.getTypeString();
            string value = v.ToString();
            string is_const = v.isConst().ToString();
            string is_traced = v.isTraced().ToString();

            return new DataTree(v.getName(), null, new List<string>
            {
                "Name: " + v.getName(),
                "Type: " + var_type,
                "Value: " + value,
                "Const: " + is_const,
                "Traced: " + is_traced
            });
        }

        private static DataTree varTracerDataTree(VarTracer tracer)
        {
            string traced_var_name = tracer.getVar().getName();
            
            List<string> endpoints = new List<string>
            {
                "Stack Count: " + tracer.getStackCount(),
                "Last Event: " + tracer.peekEvent(),
                "Last Value: " + tracer.peekValue()
            };
            
            DataTree var_data_tree = variableDataTree(tracer.getVar());
            var dict = new Dictionary<string, DataTree>
            {
                {traced_var_name, var_data_tree}
            };

            return new DataTree("var-tracer:" + traced_var_name, dict, endpoints);
        }

        private static DataTree funcTracerDataTree(FuncTracer tracer)
        {
            var endpoints = new List<string>
            {
                "Name: " + tracer.traced_func,
                "Last Stack Count: " + tracer.numAwaitingEvents()
            };

            return new DataTree(tracer.traced_func, null, endpoints);
        }

        private DataTree(string request, Dictionary<string, DataTree> data, List<string> endpoints)
        {
            _call = 1;
            _request = request;
            _data = data;
            _endpoints = endpoints;
        }

        public DataTree()
        {
            _call = 2;
            _request = "root";
            _data = new Dictionary<string, DataTree>
            {
                {"Usable Variables", new DataTree("Usable Variables")},
                {"Tracers", new DataTree("Tracers")},
                //{"Animations", new DataTree("Animations")}
            };
        }

        public DataTree update()
        {
            switch (_call)
            {
                case 0:
                    return new DataTree(_request);
                case 1:
                    return this;
                case 2:
                    return new DataTree();
                default:
                    throw new NotImplementedException("Unknown call value: " + _call);
            }
        }

        public void Repr(int i = 0)
        {
            string prefix = new string('\t', i);
            // data
            if (_data != null)
            {
                foreach (var pair in _data)
                {
                    Global.stdoutWriteLine(prefix + "\t - " + pair.Key);
                    pair.Value.Repr(i + 1);
                }
            }
            // endpoint
            if (_endpoints != null)
            {
                foreach (string endpoint in _endpoints)
                {
                    Global.stdoutWriteLine(prefix + "\t * " + endpoint);
                }
            }
        }
    }
}