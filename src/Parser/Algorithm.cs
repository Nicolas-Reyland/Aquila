using System;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// The <see cref="Algorithm"/> class tries to simulate a function.
    /// <para/>List of attributes :
    /// <para/>* <see cref="_name"/> : string
    /// <para/>* <see cref="_instructions"/> : List(Instruction)
    /// <para/>* <see cref="_return_value"/> : Expression
    /// </summary>
    public class Algorithm
    {
        /// <summary>
        /// Algorithm/Function name
        /// </summary>
        private readonly string _name;
        
        /// <summary>
        /// List of <see cref="Instruction"/>s. This is the basic definition of an algorithm.
        /// </summary>
        private readonly List<Instruction> _instructions;
        
        /// <summary>
        /// Expression of the return value of the Algorithm/Function
        /// </summary>
        private Expression _return_value;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"> name of the Algorithm/Function</param>
        /// <param name="instructions"> list of <see cref="Instruction"/>s</param>
        public Algorithm(string name, List<Instruction> instructions)
        {
            _name = name;
            _instructions = instructions;
        }

        /// <summary>
        /// Getter function for the name
        /// </summary>
        /// <returns> Function name</returns>
        public string getName() => _name;

        /// <summary>
        /// Getter function for the instructions (returns a copy to the list)
        /// </summary>
        /// <returns> Copy of the instructions</returns>
        public IEnumerable<Instruction> getInstructions() => new List<Instruction>(_instructions);

        /// <summary>
        /// Set the Context status to <see cref="Context.StatusEnum.instruction_main_loop"/>.
        /// Set the Context info to this <see cref="Algorithm"/>
        /// </summary>
        private void setStartContext()
        {
            Context.setStatus(Context.StatusEnum.instruction_main_loop);
            Context.setInfo(this);
        }

        /// <summary>
        /// Set the Context status to <see cref="Context.StatusEnum.instruction_main_finished"/>.
        /// Set the Context info to this <see cref="Algorithm"/>
        /// </summary>
        private void setEndContext()
        {
            Context.setStatus(Context.StatusEnum.instruction_main_finished);
            Context.setInfo(this);
        }

        /// <summary>
        /// Execute the Algorithm/Function. <see cref="Instruction"/> by <see cref="Instruction"/>,
        /// until the list of instructions is exhausted and we can return the <see cref="_return_value"/>,
        /// using <see cref="Expression.parse"/> on it (it is an <see cref="Expression"/>)
        /// </summary>
        /// <returns> The evaluated <see cref="_return_value"/> after all the <see cref="_instructions"/> have been executed</returns>
        /// <exception cref="AquilaExceptions.RuntimeError"> ReturnValueException is null</exception>
        public Variable run()
        {
            try
            {
                // Algorithm start
                setStartContext();

                foreach (Instruction instr in _instructions)
                {
                    try
                    {
                        instr.execute();
                    }
                    catch (System.Reflection.TargetInvocationException out_exception)
                    {
                        // normal TargetInvocationException
                        if (!(out_exception.InnerException is AquilaControlFlowExceptions.ReturnValueException)) throw;
                        // casted ReturnValueException
                        AquilaControlFlowExceptions.ReturnValueException exception =
                            (AquilaControlFlowExceptions.ReturnValueException) out_exception.InnerException;

                        if (exception == null) throw new AquilaExceptions.RuntimeError("The inner ReturnValueException in the TargetInvocationException is null"); // something went wrong

                        _return_value = new Expression(exception.getExprStr());
                        Context.reset();
                        return _return_value.evaluate();
                    }
                }

                // no resetting here. algorithm finished
                setEndContext();

                return new NullVar(); // NoReturnCallWarning
            }
            catch (Exception e)
            {
                Global.stdoutWriteLine(e.ToString());
                Global.closeStdout();

                return new NullVar();
            }
        }

        /// <summary>
        /// Initialize the test run mode
        /// </summary>
        private static void initTestMode()
        {
            Debugging.assert(!Global.getSetting("test mode"),
                new AquilaExceptions.RuntimeError("Already in test mode"));
            Global.setSetting("test mode", true);
            if (Global.test_values.Count != 0) Global.test_values.Clear();
            Stopwatch stopwatch = new Stopwatch();
            Global.test_values.Add("stopwatch", stopwatch);
            Global.test_values.Add("max elapsed ms", 10000); // n/1000 seconds
            Global.test_values.Add("instruction counter", 0);
            Global.test_values.Add("max instruction counter", 120);
            Global.test_values.Add("unfinished run", false);
            Global.test_values.Add("variable names", new List<string>());
            Global.test_values.Add("value variables", new List<string>());
            Global.test_values.Add("index variables", new List<string>());
            stopwatch.Start();
        }

        internal static void testModeInstructionUpdate()
        {
            Debugging.assert(Global.getSetting("test mode"),
                new AquilaExceptions.RuntimeError("Not in test mode"));
            // update instruction counter
            Global.test_values["instruction counter"]++;
            Debugging.print("- test run - Instruction count: ", Global.test_values["instruction counter"]);
            // still valid runtime ?
            if (Global.test_values["stopwatch"].ElapsedMilliseconds <= Global.test_values["max elapsed ms"] &&
                Global.test_values["instruction counter"] <= Global.test_values["max instruction counter"]) return;
            // unfinished algorithm
            Global.test_values["unfinished run"] = true;
            throw new AquilaExceptions.RuntimeError("Test max run time exceeded");
        }

        /// <summary>
        /// Run the Algorithm as a test run. <see cref="Instruction"/> by <see cref="Instruction"/>,
        /// until the list of instructions is exhausted and we can return the <see cref="_return_value"/>,
        /// using <see cref="Expression.parse"/> on it (it is an <see cref="Expression"/>)
        /// </summary>
        /// <returns> The evaluated <see cref="_return_value"/> after all the <see cref="_instructions"/> have been executed</returns>
        /// <exception cref="AquilaExceptions.RuntimeError"> ReturnValueException is null</exception>
        public Variable testRun()
        {
            initTestMode();
            try
            {
                // Run start
                Debugging.print("Starting Algorithm test run");
                setStartContext();

                foreach (Instruction instr in _instructions)
                {
                    try
                    {
                        instr.execute();
                    }
                    catch (System.Reflection.TargetInvocationException out_exception)
                    {
                        // normal TargetInvocationException
                        if (!(out_exception.InnerException is AquilaControlFlowExceptions.ReturnValueException)) throw;
                        // casted ReturnValueException
                        AquilaControlFlowExceptions.ReturnValueException exception =
                            (AquilaControlFlowExceptions.ReturnValueException) out_exception.InnerException;

                        if (exception == null) throw new AquilaExceptions.RuntimeError("The inner ReturnValueException in the TargetInvocationException is null"); // something went wrong

                        _return_value = new Expression(exception.getExprStr());
                        Context.reset();
                        Debugging.print("Ended Algorithm test run with return value");
                        Global.setSetting("test mode", false);
                        return _return_value.evaluate();
                    }
                }

                // no resetting here. algorithm finished
                setEndContext();

                Debugging.print("Ended Algorithm test run with no return");
                Global.setSetting("test mode", false);
                return new NullVar(); // NoReturnCallWarning
            }
            catch (Exception e)
            {
                Global.stdoutWriteLine(e.ToString());

                Debugging.print("Ended Algorithm test run with exception");
                Global.setSetting("test mode", false);
                return new NullVar();
            }
        }
    }
}