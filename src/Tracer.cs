using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;

// ReSharper disable PossibleNullReferenceException

namespace Parser
{
    public class Event
    {
        public int status;
        public object info;
        public readonly Alteration additional;

        public Event(dynamic additional)
        {
            this.status = Context.getStatus();
            this.info = Context.getInfo();
            this.additional = additional;
        }

        public override string ToString()
        {
            string status_str = status.ToString();
            string info_str = info == null ? "null" : info.ToString();
            string add_str = additional == null ? "null" : additional.ToString();
            return "status: " + status_str + " info: " + info_str + " additional: " + add_str;
        }
    }

    public class Alteration
    {
        public string name;
        public dynamic main_value;
        public dynamic[] minor_values;

        public Alteration(string name, dynamic main_value, dynamic[] minor_values)
        {
            this.name = name;
            this.main_value = main_value;
            this.minor_values = minor_values;
        }

    }
    
    public abstract class Tracer
    {
        private readonly object _traced;
        protected readonly Stack<dynamic> _values = new Stack<dynamic>();
        protected readonly Stack<Event> _events = new Stack<Event>();
        private Stack<Event> _awaiting_event = new Stack<Event>();
        private Stack<dynamic> _awaiting_values = new Stack<dynamic>();
        protected bool corrupted;
        
        internal Tracer(object traced)
        {
            this._traced = traced;
        }

        public object getTracedObject() => _traced;

        public int getStackCount() => _events.Count;

        public void awaitTrace(Event event_, dynamic value)
        {
            printTrace("await event call");
            Debugging.assert(!corrupted);
            _awaiting_event.Push(event_);
            _awaiting_values.Push(value);
        }

        private void checkAwaiting()
        {
            Debugging.assert(!corrupted);
            while (_awaiting_event.Count != 0)
            {
                printTrace("updating awaiting event " + _awaiting_event.Peek().ToString());
                update(_awaiting_event.Pop(), _awaiting_values.Pop());
            }
        }

        public abstract void update(Event event_, dynamic manual_value = null);

        public abstract void rewind(int n = 2);

        public static void updateTracers(string add_call_info = "")
        {
            // printTrace additional info about the call if needed
            if (add_call_info != "") printTrace(add_call_info);
            // check for new usable variables
            foreach (KeyValuePair<string,Variable> pair in Global.variables)
            {
                if (!Global.usable_variables.Contains(pair.Key))
                {
                    printTrace("checking potential var ", pair.Value is NullVar ? "null" : pair.Value.getName());
                    if (!(pair.Value is NullVar) && pair.Value.assigned)
                    {
                        printTrace("var ", pair.Value.getName(), " is usable (non-null & assigned)");
                        Global.usable_variables.Add(pair.Key);
                    }
                }
            }
            
            // checking tracers
            checkAllAwaitingEvent();
            
            // check tracer event stacks
            printTrace("checking variable tracer event stacks");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                while (tracer.last_stack_count != tracer.getStackCount())
                {
                    printTrace("stack count changed for ", tracer.getVar().getName(), " from ", tracer.last_stack_count, " to ", tracer.getStackCount());
                    printTrace("call graphical function");

                    // traced functions have already been processed. checking awaiting stacks
                    int diff = tracer.getStackCount() - tracer.last_stack_count;
                    printTrace("stack difference is ", diff);
                    Debugging.assert(diff > 0); // cannot handle rewinding yet

                    tracer.last_stack_count++; //! here

                    while (tracer._awaiting_event.Count != 0)
                    {
                        printTrace("awaiting stacks: ", tracer._awaiting_event.Count);
                        Alteration alter = tracer._awaiting_event.Peek().additional;
                        printTrace("call graphical function");
                        if (Global.graphical_function != null)
                        {
                            Global.graphical_function(alter);
                        }
                        else
                        {
                            printTrace("Graphical function is none.");
                            //throw Global.aquilaError();
                        }
                        tracer.update(tracer._awaiting_event.Pop(), tracer._awaiting_values.Pop());
                    }

                    
                }
            }

            //Global.current_line_index++;
        }

        private static void checkAllAwaitingEvent()
        {
            printTrace("checking all var tracers");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                tracer.checkAwaiting();
            }
            
            printTrace("checking all func tracers");
            foreach (FuncTracer tracer in Global.func_tracers)
            {
                tracer.checkAwaiting();
            }
        }

        public dynamic peekValue() => _values.Peek();
        public dynamic peekEvent() => _events.Peek();

        public static void printTrace(params object[] args)
        {
            // if not in debugging mode, return
            if (!Global.trace_debug) { return; }

            int max_call_name_length = 30;
            StackTrace stackTrace = new StackTrace();
            string call_name = stackTrace.GetFrame(1) == null ? "? stackTrace == null ?" : stackTrace.GetFrame(1).GetMethod().Name;
            int missing_spaces = max_call_name_length - call_name.Length - Global.current_line_index.ToString().Length - 2; // 2: parentheses
            
            // debugging mode is on
            Console.Write("TRACE " + call_name + "(" + Global.current_line_index.ToString() + ")");
            for (int i = 0; i < missing_spaces; i++) { Console.Write(" "); }
            
            Console.Write(" : ");
            foreach (dynamic arg in args)
            {
                Console.Write(arg.ToString());
            }
            Console.WriteLine();
        }

        public void printValueStack()
        {
            corrupted = true;
            int i = 0;
            int size = _values.Count;
            for (; i < size; i++)
            {
                Console.Write("{0}: ", i);
                Console.WriteLine(_values.Pop().ToString());
            }
        }

        public void printEventStack()
        {
            corrupted = true;
            int i = 0;
            int size = _events.Count;
            for (; i < size; i++)
            {
                Console.Write("{0}: ", i);
                Console.WriteLine(_events.Pop().ToString());
            }
        }
    }
    
    public class VarTracer : Tracer
    {
        private readonly Variable _traced_var;
        public int last_stack_count;
        
        public VarTracer(Variable traced) : base(traced)
        {
            _traced_var = traced;
            _values.Push(_traced_var.getValue());
            _events.Push(new Event(new Alteration("setValue", _traced_var.getValue(), new dynamic[] { }))); // tracer creation event
            last_stack_count = getStackCount();
        }

        public Variable getVar() => _traced_var;

        public override void update(Event event_, dynamic manual_value = null)
        {
            //last_stack_count++;
            Debugging.assert(!corrupted);
            Debugging.assert(event_.additional != null); // variable events can only hold Alterations, so checking for null
            _values.Push(manual_value == null ? _traced_var.getValue() : manual_value);
            _events.Push(event_);
            printTrace("updated (value: " + peekValue().ToString() + ")");
        }

        public override void rewind(int n = 2)
        {
            //last_stack_count -= n;
            Debugging.assert(n > 1); // if n == 1: same value
            Debugging.assert(n <= _events.Count); // didn't rewind before creation of variable ?
 
            dynamic dyn_value = null;
            for (int i = 0; i < n; i++)
            {
                printTrace("rewind: ", i, "/", n);
                dyn_value = _values.Pop();
                printTrace("dyn_value: ", dyn_value.ToString());
                _events.Pop();
            }

            Debugging.assert(dyn_value != null);
            _traced_var.forceSetValue(dyn_value);
        }

        public void squeezeEvents(int n, Event event_)
        {
            //last_stack_count -= n - 1; // - 1: adds manually a stack level at the end of the function
            Debugging.assert(!corrupted);
            Debugging.assert(_values.Count >= n); // _values and _events have the same length

            printTrace("squeezing ", n);
            for (int i = 0; i < n; i++)
            {
                _values.Pop();
                _events.Pop();
            }

            _values.Push(_traced_var.getValue());
            _events.Push(event_);

            printTrace("resulting length: ", _events.Count, " & ", _values.Count);
        }
    }

    public class FuncTracer : Tracer
    {
        public readonly string traced_func;
        private readonly int[] _affected_indexs; // index array of the args that should be traced & back-traced (through the squeeze function)
        private readonly int[] _num_steps; // number of steps to backtrace (squeeze) (one for every affected arg, in same order than _affected_indexs)
        
        public FuncTracer(string traced_func, int[] affected_indexs, int[] num_steps) : base(traced_func)
        {
            this.traced_func = traced_func;
            _affected_indexs = affected_indexs;
            _num_steps = num_steps;
            Debugging.assert(_affected_indexs.Length == _num_steps.Length); // one affected for each num_step
        }

        public override void update(Event event_, dynamic manual_value = null)
        {
            Debugging.assert(!corrupted);
            Debugging.assert(event_ != null);
            _values.Push(event_.additional); // Alteration
            _events.Push(event_);

            printTrace("value stack size: " + _values.Count);
            printTrace("event stack size: " + _events.Count);

            for (int i = 0; i < _affected_indexs.Length; i++)
            {
                int index = _affected_indexs[i];
                int num_squeeze_steps = _num_steps[i];
                printTrace("function squeezing args: ", i, " ", index, " ", num_squeeze_steps);
                Alteration alter = event_.additional;
                Variable affected = alter.main_value as Variable;
                Debugging.assert(affected != null);
                Debugging.assert(affected.isTraced());
                affected.tracer.squeezeEvents(num_squeeze_steps, new Event( new Alteration(traced_func, peekValue().main_value, peekValue().minor_values)));
            }
            printTrace("updated");
        }

        public override void rewind(int n)
        {
            throw Global.aquilaError(); // New Bizarre Exception Type ( cannot rewind function calls. can ony rewind variable values :D )
        }
    }
}