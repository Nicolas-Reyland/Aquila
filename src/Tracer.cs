using System;
using System.Collections.Generic;
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
        private Event _awaiting_event;
        protected bool corrupted;
        
        internal Tracer(object traced)
        {
            this._traced = traced;
        }

        public object getTracedObject() => _traced;

        public int getStackCount() => _events.Count;

        public void awaitEvent(Event event_)
        {
            Debugging.print("await event call");
            Debugging.assert(!corrupted);
            Debugging.assert(_awaiting_event == null); // cannot have two awaiting events ? (_value of the first one ???)
            _awaiting_event = event_;
        }

        private void checkAwaiting()
        {
            Debugging.assert(!corrupted);
            if (_awaiting_event != null)
            {
                Debugging.print("updating awaiting event " + _awaiting_event);
                update(_awaiting_event);
                _awaiting_event = null;
            }
        }

        public abstract void update(Event event_);

        public abstract void rewind(int n = 2);

        public static void updateTracers()
        {
            // check for new usable variables
            foreach (KeyValuePair<string,Variable> pair in Global.variables)
            {
                if (!Global.usable_variables.Contains(pair.Key))
                {
                    Debugging.print("checking potential non null-var ", pair.Value.getName());
                    if (!(pair.Value is NullVar) && pair.Value.assigned)
                    {
                        Debugging.print("var ", pair.Value.getName(), " is usable (non-null & assigned)");
                        Global.usable_variables.Add(pair.Key);
                    }
                }
            }
            
            // checking tracers
            checkAllAwaitingEvent();
            
            // check tracer event stacks
            Debugging.print("checking tracer event stack");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                if (tracer.last_stack_count != tracer.getStackCount())
                {
                    Debugging.print("stack count changed for ", tracer.getVar().getName(), " from ", tracer.last_stack_count, " to ", tracer.getStackCount());
                    
                    // only one change at a time (traced functions have already be processed)
                    int diff = tracer.getStackCount() - tracer.last_stack_count;
                    Debugging.print("stack difference is ", diff);
                    Debugging.assert(diff == 1);
                    
                    // peek at variable change
                    Event event_ = tracer.peekEvent();
                    Alteration alter = event_.additional;
                    Debugging.print(alter);
                    Debugging.assert(alter != null);

                    if (Global.graphical_function != null)
                    {
                        Global.graphical_function(alter);
                    }
                    else
                    {
                        Debugging.print("Graphical function is none.");
                        //throw Global.aquilaError();
                    }
                }
            }

            Global.current_line_index++;
        }

        private static void checkAllAwaitingEvent()
        {
            Debugging.print("checking all var tracers");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                tracer.checkAwaiting();
            }
            
            Debugging.print("checking all func tracers");
            foreach (FuncTracer tracer in Global.func_tracers)
            {
                tracer.checkAwaiting();
            }
        }

        public dynamic peekValue() => _values.Peek();
        public dynamic peekEvent() => _events.Peek();

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

        public override void update(Event event_)
        {
            Debugging.assert(!corrupted);
            Debugging.assert(event_.additional is Alteration); // variable events can only hold Alterations !
            _values.Push(_traced_var.getValue());
            _events.Push(event_);
            Debugging.print("updated (value: " + peekValue().ToString() + ")");
        }

        public override void rewind(int n = 2)
        {
            Debugging.assert(n > 1); // if n == 1: same value
            Debugging.assert(n <= _events.Count); // didn't rewind before creation of variable ?
 
            dynamic dyn_value = null;
            for (int i = 0; i < n; i++)
            {
                Debugging.print("rewind: ", i, "/", n);
                dyn_value = _values.Pop();
                Debugging.print("dyn_value: ", dyn_value.ToString());
                _events.Pop();
            }

            Debugging.assert(dyn_value != null);
            _traced_var.forceSetValue(dyn_value);
        }

        public void squeezeEvents(int n, Event event_)
        {
            Debugging.assert(!corrupted);
            Debugging.assert(_values.Count >= n); // _values and _events have the same length

            Debugging.print("squeezing ", n);
            for (int i = 0; i < n; i++)
            {
                _values.Pop();
                _events.Pop();
            }

            _values.Push(_traced_var.getValue());
            _events.Push(event_);

            Debugging.print("resulting length: ", _events.Count, " & ", _values.Count);
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

        public override void update(Event event_)
        {
            Debugging.assert(!corrupted);
            _values.Push(event_.additional); // array of variables (function args)
            _events.Push(event_);

            Debugging.print("value stack size: " + _values.Count);
            Debugging.print("event stack size: " + _events.Count);

            for (int i = 0; i < _affected_indexs.Length; i++)
            {
                int index = _affected_indexs[i];
                int num_squeeze_steps = _num_steps[i];
                Debugging.print("function squeezing args: ", i, " ", index, " ", num_squeeze_steps);
                Alteration alter = event_.additional as Alteration;
                Variable affected = alter.main_value as Variable;
                Debugging.assert(affected != null);
                Debugging.assert(affected.isTraced());
                affected.tracer.squeezeEvents(num_squeeze_steps, new Event( new Alteration(traced_func, peekValue().main_value, peekValue().minor_values)));
            }
            Debugging.print("updated");
        }

        public override void rewind(int n)
        {
            throw Global.aquilaError(); // New Bizarre Exception Type ( cannot rewind function calls. can ony rewind variable values :D )
        }
    }
}