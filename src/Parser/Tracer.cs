using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    public class Event
    {
        private readonly int _status;
        private readonly object _info;
        public readonly Alteration alter;

        public Event(dynamic alter)
        {
            this._status = Context.getStatus();
            this._info = Context.getInfo();
            this.alter = alter;
        }

        public override string ToString()
        {
            string status_str = _status.ToString();
            string info_str = _info == null ? "null" : _info.ToString();
            string add_str = alter == null ? "null" : alter.ToString();
            return "status: " + status_str + " info: " + info_str + " alter: " + add_str;
        }
    }

    public class Alteration
    {
        public readonly string name;
        public readonly Variable affected;
        public dynamic main_value;
        public readonly dynamic[] minor_values;

        public Alteration(string name, Variable affected, dynamic main_value, dynamic[] minor_values)
        {
            this.name = name;
            this.affected = affected;
             this.main_value = main_value;
            this.minor_values = minor_values;
        }

        public override string ToString()
        {
            return "name: " + name + " var_name: " + affected.getName() + " main_value: " +
                   StringUtils.dynamic2Str(main_value) +
                   " minor values (num): " + minor_values.Length;
        }
    }

    public abstract class Tracer
    {
        private readonly object _traced;
        protected readonly Stack<Event> events = new Stack<Event>();
        private readonly Stack<Event> _awaiting_events = new Stack<Event>();
        protected bool corrupted;

        internal Tracer(object traced)
        {
            _traced = traced;
        }

        public object getTracedObject() => _traced;

        public int getStackCount() => events.Count;

        public Stack<Event> getEventStack() => events;

        public void awaitTrace(Event event_)
        {
            printTrace("await event call");
            Debugging.assert(!corrupted);
            _awaiting_events.Push(event_);
        }

        public abstract void update(Event event_);

        public static void updateTracers(string add_call_info = "")
        {
            printTrace("updating tracers");
            // printTrace alter info about the call if needed
            if (add_call_info != "") printTrace(add_call_info);
            // check for new usable variables
            foreach (var pair in Global.getCurrentDict().Where(pair => !Global.usable_variables.Contains(pair.Key)))
            {
                printTrace("checking potential var ", pair.Value is NullVar ? "null" : pair.Value.getName());
                if (!(pair.Value is NullVar) && pair.Value.assigned)
                {
                    printTrace("var ", pair.Value.getName(), " is usable (non-null & assigned)");
                    Global.usable_variables.Add(pair.Key);
                }
            }

            // checking tracers
            checkAllAwaitingEvents();

            // check tracer event stacks
            printTrace("checking variable tracers event stack counts");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                // unread tracers updates
                while (tracer.last_stack_count != tracer.getStackCount())
                {
                    printTrace("stack count changed for ", tracer.getVar().getName(), " from ", tracer.last_stack_count, " to ", tracer.getStackCount());
                    //Console.WriteLine("call graphical function " + StringUtils.varList2String(tracer.getVar().getRawValue()) + " & call event: " + tracer.peekEvent().ToString());

                    // traced functions have already been processed. checking awaiting stacks
                    int diff = tracer.getStackCount() - tracer.last_stack_count;
                    Debugging.assert(diff > 0); // will run forever

                    tracer.last_stack_count++; //! here
                }

                // awaiting tracer stacks
                while (tracer._awaiting_events.Count != 0)
                {
                    printTrace("awaiting stacks: ", tracer._awaiting_events.Count);
                    Alteration alter = tracer._awaiting_events.Peek().alter;
                    if (Global.graphical_function != null)
                    {
                        Global.graphical_function(alter);
                    }
                    else
                    {
                        printTrace("Graphical function is none.");
                        //throw new Exception("Graphical Function is null !!");
                    }
                    tracer.update(tracer._awaiting_events.Pop());
                }
            }
        }

        private static void checkAllAwaitingEvents()
        {
            printTrace("checking all var tracers");
            foreach (VarTracer tracer in Global.var_tracers)
            {
                printTrace("last tracer event: " + tracer.peekEvent());
                tracer.checkAwaiting();
            }

            printTrace("checking all func tracers");
            foreach (FuncTracer tracer in Global.func_tracers)
            {
                printTrace(" <-> " + tracer.getStackCount());
                tracer.checkAwaiting();
            }
        }

        private void checkAwaiting()
        {
            Debugging.assert(!corrupted);
            while (_awaiting_events.Count != 0)
            {
                printTrace("event count: " + getStackCount());
                printTrace("updating awaiting event " + _awaiting_events.Peek());
                Event event_ = _awaiting_events.Pop();
                printTrace("last event: " + event_.alter.affected.tracer.peekEvent());
                event_.alter.main_value = event_.alter.affected.getRawValue();
                printTrace("settings event value manually to " + StringUtils.dynamic2Str(event_.alter.main_value));
                update(event_);
            }
        }

        public dynamic peekValue() => events.Peek().alter.main_value;
        public Event peekEvent() => events.Peek();

        public static void printTrace(params object[] args)
        {
            // if not in debugging mode, return
            if (!Global.getSetting("trace debug")) return;

            // default settings
            int max_call_name_length = 30;
            int num_new_method_separators = 25;
            bool enable_function_depth = true;
            int num_function_depth_chars = 4;
            string prefix = "- TRACE";

            // print the args nicely
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            StringUtils.nicePrintFunction(max_call_name_length, num_new_method_separators, enable_function_depth,
                num_function_depth_chars, prefix, args);
        }

        public void printEventStack()
        {
            corrupted = true;
            int i = 0;
            int size = events.Count;
            for (; i < size; i++)
            {
                Console.Write("{0}: ", i);
                Console.WriteLine(events.Pop().ToString());
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
            events.Push(new Event(new Alteration("creation", _traced_var, _traced_var.getRawValue(), new dynamic[] { }))); // tracer creation event
            last_stack_count = 1;
        }

        public Variable getVar() => _traced_var;

        public override void update(Event event_)
        {
            // blocked context ?
            if (Context.isFrozen() && !Global.getSetting("allow tracing in frozen context")) return;
            // checks
            Debugging.assert(!corrupted);
            Debugging.assert(event_.alter != null); // variable events can only hold Alterations, so checking for null

            // update
            events.Push(event_);
            printTrace("updated (value: " + StringUtils.dynamic2Str(peekValue()) + ")");
        }

        public void rewind(int n = 1)
        {
            last_stack_count -= n;
            Debugging.assert(events.Count > 1); // 1 is for variable creation
            Debugging.assert(n < events.Count); // didn't rewind before creation of variable ?

            dynamic dyn_value;
            for (int i = 0; i < n; i++)
            {
                printTrace("rewind: ", i, "/", n);
                Event popped = events.Pop();
                printTrace("popped event: " + popped);
                dyn_value = popped.alter.main_value;
                printTrace("loop dyn_value: ", StringUtils.dynamic2Str(dyn_value));
            }

            Event new_ = peekEvent();
            printTrace("latest event after rewind: " + new_);
            dyn_value = peekValue();
            printTrace("final rewind value: " + StringUtils.dynamic2Str(dyn_value));
            _traced_var.forceSetValue(dyn_value);
        }

        public void overwriteLastEvent(Event event_)
        {
            last_stack_count--;
            Debugging.assert(!corrupted);
            //Debugging.assert(_events.Count > n); // cannot overwrite declaration ? // CAN overwrite declaration ! (but warning ?)

            printTrace("overwriting last event");

            events.Pop();
            events.Push(event_);
        }
    }

    public class FuncTracer : Tracer
    {
        public readonly string traced_func;

        public FuncTracer(string traced_func) : base(traced_func)
        {
            this.traced_func = traced_func;
        }

        public override void update(Event event_)
        {
            // blocked context ?
            if (Context.isFrozen() && !Global.getSetting("allow tracing in frozen context"))
            {
                printTrace("Context is blocked in function tracer update call. Normal behaviour ?");
                return;
            }
            // checks
            Debugging.assert(!corrupted);
            Debugging.assert(event_ != null);
            // extract
            events.Push(event_);

            printTrace("event stack size: " + events.Count);

            Alteration alter = event_.alter;
            Variable affected = alter.affected;
            Debugging.assert(affected != null);
            Debugging.assert(affected.isTraced());
            affected.tracer.update(new Event( new Alteration(traced_func, affected, event_.alter.main_value, event_.alter.minor_values))); //! just push event_ !

            printTrace("updated all in function tracer update");
        }
    }
}
