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
        internal List<Instruction> instructions;

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
        protected bool in_loop = false;

        protected Loop(Expression condition, List<Instruction> instructions)
        {
            this._condition = condition;
            this.instructions = instructions;
        }

        protected bool test()
        {
           Variable cond = _condition.evaluate();
           Debugging.assert(cond is BooleanVar); // TypeError
           bool bool_cond = cond.getValue();
           return bool_cond;
        }
        
        public bool isInLoop() => in_loop;
    }

    public class WhileLoop : Loop
    {
        public WhileLoop(Expression condition, List<Instruction> instructions) : base(condition, instructions)
        {
            //
        }
        
        public override void execute() //!
        {
            while (test())
            {
                in_loop  = true;
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
            }
            in_loop = false;
        }
    }
    
    public class ForLoop : Loop
    {
        private readonly Instruction _start;
        private readonly Instruction _step;
        private readonly WhileLoop _while_loop;

        public ForLoop(Instruction start, Expression condition, Instruction step,
            List<Instruction> instructions) : base(condition, instructions)
        {
            this._start = start;
            this._step = step;
            this._while_loop = new WhileLoop(condition, instructions);
            this._while_loop.instructions.Add(_step);
        }
        
        /*
        for (decl_index(), var index < 5, var index = var index + 1)
            instruction 1
            instruction 2
            instruction 3
        end-for
        ==
        decl_index()
        while (var index < 5)
            instruction 1
            instruction 2
            instruction 3
            var index = var index + 1
        */

        public override void execute()
        {
            _start.execute();
            _while_loop.execute();
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
            if (((BooleanVar) _condition.evaluate()).getValue())
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
        private readonly Expression _var_expr;
        private readonly string _var_type;
        
        public Declaration(string var_name, Expression var_expr, string var_type = "auto")
        {
            this._var_name = var_name;
            this._var_expr = var_expr;
            this._var_type = var_type;
            Debugging.print("var expr: ", var_expr._expr); //! remove, or debug == 2 or something, idk
            // add variable to dictionary
            Debugging.assert(!Global.variables.ContainsKey(var_name)); // DeclaredExistingVarException
            Variable temp_value = _var_type == "auto"
                ? new TempVar() // temporary variable. doesn't have any real value
                : (Variable) Global.default_values_by_var_type[var_type].evaluate();
            Global.variables.Add(var_name, temp_value);
        }

        public override void execute()
        {
            // already in dictionary
            Variable variable = _var_expr.evaluate();
            if (_var_type != "auto")
            {
                Debugging.print("checking variable explicit type");
                Expression default_value = Global.default_values_by_var_type[_var_type];
                Debugging.assert( variable.hasSameParent(default_value.evaluate()) ); // TypeException
            }
            // actually declare it to its value
            Global.variables[_var_name] = variable;
            Global.variables[_var_name].assign(); // AssignmentError, be sure it worked nicely
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
        }

        public override void execute()
        {
            Expression.parse(_var_name).setValue( _var_value.evaluate() );
        }
    }
    
    public class VoidFunctionCall : Instruction
    {
        private readonly string _function_name;
        private readonly object[] _args;
        private bool _called;

        public VoidFunctionCall(string function_name, params object[] args)
        {
            _function_name = function_name;
            _args = args;
        }
        
        public override void execute()
        {
            _called = true;
            Functions.callFunctionByName(_function_name, _args);
        }

        public bool hasBeenCalled() => _called;
    }
}