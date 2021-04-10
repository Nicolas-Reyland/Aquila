using System.Collections.Generic;
using System.Linq;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// An <see cref="Event"/> describes a <see cref="Variable"/> alteration (<see cref="Alteration"/>).
    /// It saves the current <see cref="Context"/> and the <see cref="Alteration"/>.
    /// It should be saved into and Event-stack
    /// </summary>
    public class Event
    {
        /// <summary>
        /// The <see cref="Context"/> status at the alteration time
        /// </summary>
        private readonly int _status;
        /// <summary>
        /// The <see cref="Context"/> info at the alteration time
        /// </summary>
        private readonly object _info;
        /// <summary>
        /// The <see cref="Alteration"/> done at the event time.
        /// </summary>
        public readonly Alteration alter;

        /// <summary>
        /// Create a new <see cref="Event"/>
        /// </summary>
        /// <param name="alter"></param>
        public Event(dynamic alter)
        {
            _status = Context.getStatus();
            _info = Context.getInfo();
            this.alter = alter;
        }

        /// <summary>
        /// <see cref="string"/> representation of the <see cref="Event"/>
        /// </summary>
        /// <returns> $"status: {status_str} info: {info_str} alter: {add_str}"</returns>
        public override string ToString()
        {
            string status_str = _status.ToString();
            string info_str = _info == null ? "null" : _info.ToString();
            string add_str = alter == null ? "null" : alter.ToString();
            return $"status: {status_str} info: {info_str} alter: {add_str}";
        }
    }

    /// <summary>
    /// An <see cref="Alteration"/> represents a <see cref="Variable"/> alteration. It means that
    /// a <see cref="Variable"/> has changed (or its value has changed).
    /// </summary>
    public class Alteration
    {
        /// <summary>
        /// Name of the <see cref="Alteration"/>. If, for example, the <see cref="affected"/> variable
        /// is an <see cref="Integer"/> and it has just been modified from 2 to 3, the <see cref="name"/>
        /// of the <see cref="Alteration"/> would be "setValue" (<see cref="Variable.setValue"/>), because the value of the <see cref="Variable"/>
        /// has been set to a new value.
        /// </summary>
        public readonly string name;
        /// <summary>
        /// The <see cref="Variable"/> which this <see cref="Alteration"/> focuses on
        /// </summary>
        public readonly Variable affected;
        /// <summary>
        /// The new value of the <see cref="Variable"/> (the raw value: e.g. <see cref="Integer"/> -> <see cref="int"/>)
        /// </summary>
        public dynamic main_value;
        /// <summary>
        /// The values that were implicated in the <see cref="Alteration"/>. If the <see cref="Alteration"/> is a result of
        /// a function call on a variable, the <see cref="minor_values"/> would be the other arguments given to the function
        /// (as raw values: e.g. <see cref="Integer"/> -> <see cref="int"/>).
        /// </summary>
        public readonly dynamic[] minor_values;

        /// <summary>
        /// Create a new <see cref="Alteration"/>
        /// </summary>
        /// <param name="name"> name of the Alteration (e.g. "creation", "setValue", "Append")</param>
        /// <param name="affected"> the modified/changed <see cref="Variable"/></param>
        /// <param name="main_value"> the raw value of the changed <see cref="Variable"/> (raw values: e.g. <see cref="Integer"/> -> <see cref="int"/>)</param>
        /// <param name="minor_values"> teh raw values of the other variables which have been implicated in the change  (raw values: e.g. <see cref="Integer"/> -> <see cref="int"/>)</param>
        public Alteration(string name, Variable affected, dynamic main_value, dynamic[] minor_values)
        {
            this.name = name;
            this.affected = affected;
            this.main_value = main_value;
            this.minor_values = minor_values;
        }

        /// <summary>
        /// <see cref="string"/> representation of the <see cref="Alteration"/>
        /// </summary>
        /// <returns> $"name: {name} var_name: {affected.getName()}main_value: {StringUtils.dynamic2Str(main_value)} minor values (num): {minor_values.Length}"</returns>
        public override string ToString()
        {
            return $"name: {name} var_name: {affected.getName()}main_value: {StringUtils.dynamic2Str(main_value)} minor values (num): {minor_values.Length}";
        }
    }

    /// <summary>
    /// A <see cref="Tracer"/> is attached to a <see cref="Variable"/> or a function (user-defined or pre-defined).
    /// It keeps track of all the <see cref="Alteration"/>s that are done on the traced <see cref="Variable"/> or by
    /// the traced function.
    /// </summary>
    public abstract class Tracer
    {
        /// <summary>
        /// The traced object (a <see cref="Variable"/> or the <see cref="string"/> of the function name of the traced function)
        /// </summary>
        private readonly object _traced;
        /// <summary>
        /// The <see cref="Event"/>s that have happened on the <see cref="_traced"/> object
        /// </summary>
        protected readonly Stack<Event> events = new Stack<Event>();
        /// <summary>
        /// All the <see cref="Event"/>s that should be processed at the next update step in the execution loop
        /// </summary>
        private readonly Stack<Event> _awaiting_events = new Stack<Event>();

        /// <summary>
        /// Create a new <see cref="Tracer"/>
        /// </summary>
        /// <param name="traced"> the traced object (<see cref="Variable"/> or function name)</param>
        internal Tracer(object traced)
        {
            _traced = traced;
        }

        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> the traced object</returns>
        public object getTracedObject() => _traced;

        /// <summary>
        /// Gets the number of <see cref="Event"/>s in the <see cref="events"/> stack
        /// </summary>
        /// <returns> Number of <see cref="Event"/>s in the <see cref="events"/> stack</returns>
        public int getStackCount() => events.Count;

        /// <summary>
        /// Return a copy of the reversed event stack (first element at the bottom of stack)
        /// </summary>
        /// <returns> Reversed copy of the <see cref="events"/> stack</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public Stack<Event> getReversedEventStackCopy() => new Stack<Event>(events);

        /// <summary>
        /// Gives a copy of the event stack
        /// </summary>
        /// <returns> Copy of the <see cref="events"/> stack</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public Stack<Event> getEventStackCopy()
        {
            var reversed_stack_copy = getReversedEventStackCopy();
            return new Stack<Event>(reversed_stack_copy);
        }

        /// <summary>
        /// Add an <see cref="Event"/> on top of the <see cref="_awaiting_events"/> stack
        /// </summary>
        /// <param name="event_"> The pending <see cref="Event"/></param>
        public void awaitTrace(Event event_)
        {
            printTrace("await event call");
            _awaiting_events.Push(event_);
        }

        /// <summary>
        /// Process the input <see cref="Event"/> before putting it on top of the <see cref="events"/> stack
        /// </summary>
        /// <param name="event_"> New <see cref="Event"/></param>
        public abstract void update(Event event_);

        /// <summary>
        /// This function should be called at the end of every <see cref="Instruction.execute"/> call that is susceptible of
        /// changing the value of a <see cref="Variable"/>. It works in 3 steps:
        /// <para/>* Checks for any new assigned <see cref="Variable"/>s in the <see cref="Global._variable_stack"/> (if found: adds to the <see cref="Global.usable_variables"/>)
        /// <para/>* Update/Process all the awaiting <see cref="Event"/>s (<seealso cref="update"/>)
        /// <para/>* Check for any new <see cref="Event"/> which has not been processed by the <see cref="Global.tracer_update_handler_function"/>
        /// </summary>
        /// <param name="add_call_info"> stands for "additional calling information". If it has any value, it will be printed by the <see cref="Debugging.print"/> function for debugging</param>
        public static void updateTracers(string add_call_info = "")
        {
            printTrace("updating tracers");
            // printTrace alter info about the call if needed
            if (add_call_info != "") printTrace(add_call_info);
            // check for new usable variables
            foreach (var pair in Global.getCurrentDict().Where(pair => !Global.usable_variables.Contains(pair.Key)))
            {
                printTrace("checking potential var ", pair.Value is NullVar ? "null" : pair.Value.getName());
                if (pair.Value is NullVar || !pair.Value.assigned) continue;
                // new usable variable !
                printTrace("var ", pair.Value.getName(), " is usable (non-null & assigned)");
                Global.usable_variables.Add(pair.Key);
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
                    //Global.stdoutWriteLine("call graphical function " + StringUtils.varList2String(tracer.getVar().getRawValue()) + " & call event: " + tracer.peekEvent().ToString());

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
                    callUpdateHandler(alter);
                    tracer.update(tracer._awaiting_events.Pop());
                }
            }
        }

        /// <summary>
        /// Call the <see cref="Global.tracer_update_handler_function"/> with the Alteration.
        /// If no function was defined, prints it to the debugging stdout and does nothing
        /// </summary>
        /// <param name="alter"> Alteration you want to animate</param>
        protected static void callUpdateHandler(Alteration alter)
        {
            if (Global.tracer_update_handler_function != null)
            {
                printTrace("Calling graphical function");
                Global.tracer_update_handler_function(alter);
            }
            else
            {
                printTrace("Graphical function is none");
            }
        }

        /// <summary>
        /// Process all the pending/awaiting <see cref="Event"/>s for all the existing <see cref="Tracer"/>s
        /// </summary>
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

        /// <summary>
        /// Process the pending/awaiting <see cref="Event"/>s of this <see cref="Tracer"/>. If there are any, process them (<see cref="update"/>)
        /// </summary>
        private void checkAwaiting()
        {
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

        /// <summary>
        /// Peek at the last <see cref="Event"/> of the <see cref="events"/> stack
        /// </summary>
        /// <returns></returns>
        public Event peekEvent() => events.Peek();
        /// <summary>
        /// Peek at the <see cref="Alteration.main_value"/> of the <see cref="Alteration"/> of the most recently added <see cref="Event"/>
        /// </summary>
        /// <returns> the corresponding <see cref="Alteration.main_value"/></returns>
        public dynamic peekValue() => events.Peek().alter.main_value;

        /// <summary>
        /// Print the given values only if the "trace debug" setting is set to true.
        /// For debugging only
        /// </summary>
        /// <param name="args"></param>
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

        /// <summary>
        /// Print the whole event stack (deconstructing progressively). A copy of the event stack is used.
        /// The real event stack is not altered
        /// </summary>
        public void printEventStack()
        {
            var events_copy = getEventStackCopy();
            int i = 0;
            int size = events_copy.Count;
            for (; i < size; i++)
            {
                Global.stdoutWrite($"{i}: ");
                Global.stdoutWriteLine(events_copy.Pop().ToString());
            }
        }
    }

    /// <summary>
    /// A <see cref="Tracer"/> for <see cref="Variable"/>s
    /// </summary>
    public class VarTracer : Tracer
    {
        /// <summary>
        /// The traced <see cref="Variable"/>
        /// </summary>
        private readonly Variable _traced_var;
        /// <summary>
        /// The <see cref="last_stack_count"/> value at the last <see cref="Tracer.updateTracers"/> call
        /// </summary>
        public int last_stack_count;

        /// <summary>
        /// Create a new <see cref="VarTracer"/>
        /// </summary>
        /// <param name="traced"> the traced <see cref="Variable"/></param>
        public VarTracer(Variable traced) : base(traced)
        {
            _traced_var = traced;
            var creation_event =
                new Event(new Alteration("creation", _traced_var, _traced_var.getRawValue(), new dynamic[] { }));
            events.Push(creation_event); // tracer creation event
            last_stack_count = 1;
            callUpdateHandler(creation_event.alter);
        }

        /// <summary>
        /// explicit naming (/!\ Getter returns the actual <see cref="Variable"/>, not a copy)
        /// </summary>
        /// <returns> the traced <see cref="Variable"/> (not a copy)</returns>
        public Variable getVar() => _traced_var;

        /// <summary>
        /// Add teh <see cref="Event"/> on top of the <see cref="Tracer.events"/> stack and
        /// process the make some assertions to detect abnormal behaviours
        /// </summary>
        /// <param name="event_"></param>
        public override void update(Event event_)
        {
            // blocked context ?
            if (Context.isFrozen() && !Global.getSetting("allow tracing in frozen context")) return;
            // checks
            Debugging.assert(event_.alter != null); // variable events can only hold Alterations, so checking for null

            // update
            events.Push(event_);
            printTrace("updated (value: " + StringUtils.dynamic2Str(peekValue()) + ")");
            
            // handle
            callUpdateHandler(event_.alter);
        }

        /// <summary>
        /// Set the value of the <see cref="_traced_var"/> (traced <see cref="Variable"/>) to be 'n' steps in the past ('n' <see cref="Alteration"/>s)
        /// </summary>
        /// <param name="n"> number of times to rewind the value of <see cref="_traced_var"/> once</param>
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

        /// <summary>
        /// Overwrite the last pushed <see cref="Event"/> by a new one
        /// </summary>
        /// <param name="event_"> new <see cref="Event"/> that should overwrite the most recently added <see cref="Event"/></param>
        public void overwriteLastEvent(Event event_)
        {
            last_stack_count--;
            //Debugging.assert(_events.Count > 1); // can or cannot overwrite creation ? maybe warning ? idk

            printTrace("overwriting last event");

            events.Pop();
            events.Push(event_);
        }
    }

    /// <summary>
    /// A <see cref="Tracer"/> for functions
    /// </summary>
    public class FuncTracer : Tracer
    {
        /// <summary>
        /// The name of the traced function
        /// </summary>
        public readonly string traced_func;

        /// <summary>
        /// Create a new <see cref="FuncTracer"/>
        /// </summary>
        /// <param name="traced_func"></param>
        public FuncTracer(string traced_func) : base(traced_func)
        {
            this.traced_func = traced_func;
        }

        /// <summary>
        /// Process the <see cref="Event"/>
        /// </summary>
        /// <param name="event_"> <see cref="Event"/> that should be processed</param>
        public override void update(Event event_)
        {
            // blocked context ?
            if (Context.isFrozen() && !Global.getSetting("allow tracing in frozen context"))
            {
                printTrace("Context is blocked in function tracer update call. Normal behaviour ?");
                return;
            }
            // checks
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
