using System;
using System.Collections.Generic;

namespace Parser
{
    /// <summary>
    /// The Context defines where we are at runtime, and gives additional info (<see cref="Context.getInfo"/>).
    /// The info is often the <see cref="Instruction"/> that is being executed.*
    /// If the <see cref="Algorithm"/> execution time hasn't been reached yet,
    /// the info type can vary (string, List, etc.)
    /// </summary>
    internal static class Context
    {
        /// <summary>
        /// Status list:
        /// <para/>*  0: undefined
        /// <para/>*  1: reading &amp; purging code
        /// <para/>*  2: processing macros
        /// <para/>*  3: building raw instructions
        /// <para/>*  4: building instructions
        /// <para/>*  5: in the main algorithm main loop
        /// <para/>*  6: trace instruction
        /// <para/>*  7: while loop execution
        /// <para/>*  8: for loop execution
        /// <para/>*  9: if instruction execution
        /// <para/>* 10: executing a declaration instruction
        /// <para/>* 11: executing a assignment instruction
        /// <para/>* 12: executing a predefined function
        /// <para/>* 13: executing a user-defined function
        /// <para/>* 14: algorithm main loop finished
        /// </summary>
        private static int _status = (int) StatusEnum.undefined;
        /// <summary>
        /// Stack of the previous status'. An algorithm being naturally of a
        /// recursive nature, we need a stack to store the status'
        /// </summary>
        private static readonly Stack<int> previous_status = new Stack<int>();
        /// <summary>
        /// Additional information about the current state.
        /// <para/> This could be an <see cref="Instruction"/> or a <see cref="FunctionCall"/>
        /// </summary>
        private static object _info;
        /// <summary>
        /// Stack of the previous infos. An algorithm being naturally of a
        /// recursive nature, we need a stack to store the infos
        /// </summary>
        private static readonly Stack<object> previous_info = new Stack<object>();
        /// <summary>
        /// Is the context blocked ? (see <see cref="Tracer"/> &amp; <see cref="Functions.swapFunction"/>)
        /// </summary>
        private static bool _frozen; // default: false
        /// <summary>
        /// Enum of all the existing status'
        /// </summary>
        public enum StatusEnum
        {
            undefined,                  // 0
            read_purge,                 // 1
            macro_preprocessing,        // 2
            building_raw_instructions,  // 3
            building_instructions,      // 4
            instruction_main_loop,      // 5
            trace_execution,            // 6
            while_loop_execution,       // 7
            for_loop_execution,         // 8
            if_execution,               // 9
            declaration_execution,      // 10
            assignment_execution,       // 11
            predefined_function_call,   // 12
            user_function_call,         // 13
            instruction_main_finished   // 14
        }
        // status
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> current status</returns>
        public static int getStatus() => _status;
        /// <summary>
        /// Used for verifying set/reset calls
        /// </summary>
        /// <returns> status stack count</returns>
        public static int getStatusStackCount() => previous_status.Count;
        /// <summary>
        /// check for status
        /// </summary>
        /// <param name="status_enum"> supposed status</param>
        /// <returns> is the supposed status the actual status ?</returns>
        public static bool statusIs(StatusEnum status_enum) => _status == (int) status_enum;
        /// <summary>
        /// set the new status
        /// </summary>
        /// <param name="new_status"> new status</param>
        private static void setStatus(int new_status)
        {
            previous_status.Push(_status);
            _status = new_status;
        }
        /// <summary>
        /// set the new status
        /// </summary>
        /// <param name="status_enum"> new status</param>
        public static void setStatus(StatusEnum status_enum)
        {
            if (_frozen) return;
            int status_int = (int) status_enum;
            setStatus(status_int);
        }
        /// <summary>
        /// reset the status to the last one
        /// </summary>
        /// <exception cref="InvalidOperationException"> there is no last status (the current one is the first one set)</exception>
        private static void resetStatus()
        {
            if (previous_status.Count == 0)
            {
                throw new InvalidOperationException("No previous Context state");
            }

            _status = previous_status.Pop();
        }

        // info
        /// <summary>
        /// explicit naming
        /// </summary>
        /// <returns> current info</returns>
        public static dynamic getInfo() => _info;
        /// <summary>
        /// set the new info
        /// </summary>
        /// <param name="new_info"> new info</param>
        public static void setInfo(object new_info)
        {
            if (_frozen) return;
            previous_info.Push(_info);
            _info = new_info;
        }
        /// <summary>
        /// reset the info to the last one
        /// </summary>
        /// <exception cref="InvalidOperationException"> there is no last info (the current one is the first one set)</exception>
        private static void resetInfo()
        {
            if (previous_info.Count == 0)
            {
                throw new InvalidOperationException("No previous Context info");
            }

            _info = previous_info.Pop();
        }

        // freeze
        /// <summary>
        /// freeze the context (cannot be changed) (&amp; thus all the existing <see cref="VarTracer"/>s)
        /// </summary>
        public static void freeze()
        {
            if (Global.getSetting("flame mode")) return;
            Debugging.assert(!_frozen);
            _frozen = true;
        }
        /// <summary>
        /// Try to freeze the context without triggering any asserts. If the <see cref="Context"/>
        /// is already frozen, return false (did not achieve to freeze the <see cref="Context"/>).
        /// True otherwise
        /// </summary>
        /// <returns> Successfully freeze the <see cref="Context"/> ?</returns>
        public static bool tryFreeze()
        {
            if (_frozen) return false;
            freeze();
            return true;
        }
        /// <summary>
        /// unfreeze the status
        /// <para/>Exception will be raised if the context is not frozen
        /// </summary>
        public static void unfreeze()
        {
            if (Global.getSetting("flame mode")) return;
            Debugging.assert(_frozen);
            _frozen = false;
        }
        /// <summary>
        /// is the context frozen ?
        /// </summary>
        public static bool isFrozen() => _frozen;

        // all
        /// <summary>
        /// Reset the Context to its previous state.
        /// Rests the <see cref="_status"/> &amp; <see cref="_info"/>
        /// </summary>
        /// <exception cref="Exception"> there is not the same amount of stacked previous status' &amp; infos</exception>
        public static void reset()
        {
            if (_frozen) return;
            if (previous_info.Count != previous_status.Count) throw new Exception("inconsistent use of reset");
            resetStatus();
            resetInfo();
        }
        /// <summary>
        /// Assert that the status is the one given as input.
        /// Does not assert if:
        /// <para/>* the Context is not <see cref="Global.settings"/>["fail on context assertions"]
        /// <para/>* the Context is <see cref="_frozen"/>
        /// </summary>
        /// <param name="supposed"> wanted status</param>
        /// <exception cref="Exception"> the status is not the input status</exception>
        public static void assertStatus(StatusEnum supposed)
        {
            if (Global.getSetting("fail on context assertions") && !_frozen && (int) supposed != _status) // not sure about not being blocked ?
            {
                throw new Exception("Context Assertion Error. Supposed: " + supposed + " but actual: " + _status);
            }
        }
        /// <summary>
        /// Reset the whole Context to zero
        /// </summary>
        public static void resetContext()
        {
            _status = (int) StatusEnum.undefined;
            _info = null;
            _frozen = false;
            previous_status.Clear();
            previous_info.Clear();
        }
    }
}