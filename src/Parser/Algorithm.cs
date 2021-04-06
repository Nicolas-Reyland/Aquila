using System.Collections.Generic;

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
            this._name = name;
            this._instructions = instructions;
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
        public Variable run()
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
                    if (!(out_exception.InnerException is AquilaExceptions.ReturnValueException)) throw;
                    // casted ReturnValueException
                    AquilaExceptions.ReturnValueException exception =
                        (AquilaExceptions.ReturnValueException) out_exception.InnerException;

                    if (exception == null)
                    {
                        throw Global.aquilaError(); // something went wrong
                    }

                    _return_value = new Expression(exception.getExprStr());
                    Context.reset();
                    return _return_value.evaluate();
                }
            }

            // no resetting here. algorithm finished
            setEndContext();

            return new NullVar(); // NoReturnCallWarning
        }
    }
}