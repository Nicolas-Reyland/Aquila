using System.Collections.Generic;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace Parser
{
    public abstract class Instruction
    {
        protected int line_index;

        // methods
        public abstract void execute();
        protected void setLineIndex() => Global.current_line_index = line_index;
    }

    public abstract class NestedInstruction : Instruction // ex: for, while, if, etc.
    {
        // attributes
        protected int depth;
        internal List<Instruction> instructions;

        public override void execute()
        {
            setLineIndex();
            foreach (Instruction instr in instructions)
            {
                instr.execute();
                // update all tracers
                Tracer.updateTracers();
            }
        }
    }

    public abstract class Loop : NestedInstruction
    {
        private readonly Expression _condition;
        protected bool in_loop = false;

        protected Loop(int line_index, Expression condition, List<Instruction> instructions)
        {
            this.line_index = line_index;
            this._condition = condition;
            this.instructions = instructions;
            this.line_index = line_index;
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
        public WhileLoop(int line_index, Expression condition, List<Instruction> instructions) : base(line_index, condition, instructions)
        {
            //
        }
        
        public override void execute() //!
        {
            setLineIndex();
            while (test())
            {
                in_loop  = true;
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                    // update all tracers
                    Tracer.updateTracers();
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

        public ForLoop(int line_index, Instruction start, Expression condition, Instruction step,
            List<Instruction> instructions) : base(line_index, condition, instructions)
        {
            this._start = start;
            this._step = step;
            this._while_loop = new WhileLoop(line_index, condition, instructions);
            this._while_loop.instructions.Add(_step);
        }
        
        /*
        for (decl_index(), $index < 5, $index = $index + 1)
            instruction 1
            instruction 2
            instruction 3
        end-for
        ==
        decl_index()
        while ($index < 5)
            instruction 1
            instruction 2
            instruction 3
            $index = $index + 1
        */

        public override void execute()
        {
            setLineIndex();
            _start.execute();
            // update all tracers
            Tracer.updateTracers();
            Global.current_line_index--;

            _while_loop.execute();
            // update all tracers again
            Tracer.updateTracers();
          }
    }

    public class IfCondition : NestedInstruction
    {
        private readonly Expression _condition;
        private readonly List<Instruction> _else_instructions;

        public IfCondition(int line_index, Expression condition, List<Instruction> instructions, List<Instruction> else_instructions)
        {
            this.line_index = line_index;
            this._condition = condition;
            this.instructions = instructions;
            this._else_instructions = else_instructions;
        }

        public override void execute()
        {
            setLineIndex();
            if (((BooleanVar) _condition.evaluate()).getValue())
            {
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                    // update all tracers
                    Tracer.updateTracers();
                }
            }
            else
            {
                foreach (Instruction instr in _else_instructions)
                {
                    instr.execute();
                    // update all tracers
                    Tracer.updateTracers();
                }
            }
        }
    }

    public class Declaration : Instruction
    {
        private readonly string _var_name;
        private readonly Expression _var_expr;
        private readonly string _var_type;
        private readonly bool _assignment;
        
        public Declaration(int line_index, string var_name, Expression var_expr, string var_type = "auto", bool assignment = true)
        {
            this.line_index = line_index;
            this._var_name = var_name;
            this._var_expr = var_expr;
            this._var_type = var_type;
            this._assignment = assignment;
            Debugging.print("new declaration: var_name = " + var_name + ", var_expr = " + var_expr.expr + ", var_type = " + var_type + ", assignment = ", assignment);
            // add variable to dictionary
            Debugging.assert(!Global.variables.ContainsKey(var_name)); // DeclaredExistingVarException
            Variable temp_value = _var_type == "auto"
                ? new NullVar() // temporary variable. doesn't have any real value
                : (Variable) Global.default_values_by_var_type[var_type].evaluate();
            Global.variables.Add(var_name, temp_value);
        }

        public override void execute()
        {
            setLineIndex();
            // set Context
            Context.setStatus(10); // 10: Declaration (should rly do an enum ...)
            Context.setInfo(this);

            // already in dictionary
            Variable variable = _var_expr.evaluate();
            variable.assertAssignment();
            if (_var_type != "auto")
            {
                Debugging.print("checking variable explicit type");
                Expression default_value = Global.default_values_by_var_type[_var_type];
                Debugging.assert( variable.hasSameParent(default_value.evaluate()) ); // TypeException
            }
            // actually declare it to its value
            Global.variables[_var_name] = variable;
            if (_assignment) Global.variables[_var_name].assign();
            else Global.variables[_var_name].assigned = false;
            Global.variables[_var_name].setName(_var_name);
            Debugging.print("finished declaration with value assignment: ", Global.variables[_var_name].assigned);
            
            // reset Context
            Context.reset();
            // update all tracers
            Tracer.updateTracers();
        }
    }

    public class Assignment : Instruction
    {
        private readonly string _var_name;
        private readonly Expression _var_value;

        public Assignment(int line_index, string var_name, Expression var_value)
        {
            this.line_index = line_index;
            _var_name = var_name;
            _var_value = var_value;
            Debugging.assert(_var_name != "");
        }

        public override void execute()
        {
            setLineIndex();
            // set Context
            Context.setStatus(10);
            Context.setInfo(this);
            
            Variable val = _var_value.evaluate();
            Debugging.print("assigning " + _var_name + " with expr " + _var_value.expr + " (2nd value assigned: " + val.assigned + ") and type: " + val.getTypeString());
            // special list_at case
            val.assertAssignment();
            Expression.parse(_var_name).setValue(val);
            
            // reset Context
            Context.reset();
            // update all tracers
            Tracer.updateTracers();
        }
    }
    
    public class VoidFunctionCall : Instruction
    {
        private readonly string _function_name;
        private readonly object[] _args;
        private bool _called;

        public VoidFunctionCall(int line_index, string function_name, params object[] args)
        {
            this.line_index = line_index;
            _function_name = function_name;
            _args = args;
        }
        
        public override void execute()
        {
            setLineIndex();
            Context.setStatus(7);
            Context.setInfo(this);
            _called = true;
            Functions.callFunctionByName(_function_name, _args);
            Context.reset();
            // update all tracers
            Tracer.updateTracers();
        }
        
        public object[] getArgs() => _args;

        public bool hasBeenCalled() => _called;
    }

    public class Tracing : Instruction
    {
        private readonly List<Expression> _traced_vars = new List<Expression>();
        
        public Tracing(int line_index, List<Expression> traced_vars)
        {
            this.line_index = line_index;
            this._traced_vars = traced_vars;
        }
        public override void execute()
        {
            setLineIndex();
            // the tracing instruction execution doesn't take any Tracer.updateTracers() calls
            foreach (Expression traced_expr in _traced_vars)
            {
                Variable traced_var = traced_expr.evaluate();
                Debugging.assert(!traced_var.isTraced());
                traced_var.startTracing();
            }
        }
    }
}