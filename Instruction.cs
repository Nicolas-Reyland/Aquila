using System;
using System.Collections.Generic;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    public abstract class Instruction
    {
#pragma warning disable 649
        private bool _graphical_instruction; // #pragma : stop compiler warning due to unused _graphical_instruction variable
#pragma warning restore 649

        // getters
        public bool isGraphical() => _graphical_instruction;

        // methods
        public abstract void execute();
    }

    public abstract class NestedInstruction : Instruction // ex: for, while, if, etc.
    {
        // attributes
        protected int depth;
        protected List<Instruction> instructions;

        public override void execute()
        {
            foreach (Instruction instr in instructions)
            {
                instr.execute();
            }
        }
    }

    public abstract class Loop : NestedInstruction
    {
        private readonly Expression _condition;
        private bool _in_loop = false;

        protected Loop(Expression condition, List<Instruction> instructions)
        {
            this._condition = condition;
            this.instructions = instructions;
        }

        private bool test()
        {
            return ((BooleanVar) _condition.evaluate()).bool_value;
        }

        public abstract void next();
    }

    public class WhileLoop : Loop
    {
        public WhileLoop(Expression condition, List<Instruction> instructions) : base(condition, instructions)
        {
            //
        }

        public override void next()
        {
            throw new System.NotImplementedException();
        }
    }
    
    public class ForLoop : Loop
    {
        private readonly Instruction _start;
        private readonly Instruction _step;
        private Expression _condition;

        public ForLoop(Instruction start, Expression condition, Instruction step,
            List<Instruction> instructions) : base(condition, instructions)
        {
            this._start = start;
            this._condition = condition;
            this._step = step;
            this.instructions = instructions;
        }

        public void initialize()
        {
            this._start.execute();
        }

        public override void execute()
        {
            throw new System.NotImplementedException();
        }

        public override void next()
        {
            throw new System.NotImplementedException();
        }
    }

    public class IfCondition : NestedInstruction
    {
        private readonly Expression _condition;
        private readonly List<Instruction> _else_instructions;

        public IfCondition(Expression condition, List<Instruction> instructions, List<Instruction> else_instructions = null)
        {
            this._condition = condition;
            this.instructions = instructions;
            this._else_instructions = else_instructions;
        }

        public override void execute()
        {
            if (((BooleanVar) _condition.evaluate()).bool_value)
            {
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
            }
            else
            {
                foreach (Instruction instr in _else_instructions)
                {
                    instr.execute();
                }
            }
        }
    }

    public class Declaration : Instruction
    {
        private readonly string _var_name;
        private readonly Variable _var_value;
        
        public Declaration(string var_name, Variable var_value)
        {
            this._var_name = var_name;
            this._var_value = var_value;
            // add variable to dictionary
            Debugging.assert(!Global.variables.ContainsKey(var_name));
            Global.variables.Add(var_name, var_value);
        }
        
        public override void execute()
        {
            // already in dictionary
            Global.variables[_var_name].setValue( _var_value );
            Debugging.assert(Global.variables[_var_name].assigned);
        }
    }

    public class Assignment : Instruction
    {
        private readonly string _var_name;
        private readonly Expression _var_value;

        public Assignment(string var_name, Expression var_value) // doesn't work with "var l[0]"
        {
            _var_name = var_name;
            _var_value = var_value;
            //Debugging.assert(Global.variables.ContainsKey(_var_name)); // var l[var i] ???
            try
            {
                Expression.parse(var_name);
            }
            catch (Exception e)
            {
                Debugging.print("one of the variables in the assignment doesn't exist");
                throw Global.aquilaError();
            }
            Expression.variableFromName(_var_name).setName(_var_name);
        }

        public override void execute()
        {
            //! HERE PLS DO var l[var i] :)
            throw new NotImplementedException("var l[var i] not done. _var_name is \"var x\" (with the var prefix)");
            Debugging.print("executing assignment to ", _var_name);
            Expression.variableFromName(_var_name).setValue( _var_value.evaluate() );
        }
    }
    
    public class VoidFunctionCall : Instruction
    {
        private readonly Expression _function;
        private bool _called;

        public VoidFunctionCall(Expression function)
        {
            _function = function;
        }
        
        public override void execute()
        {
            _called = true;
            _function.evaluate();
        }

        public bool hasBeenCalled() => _called;
    }
}