using System.Collections.Generic;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    public abstract class Instruction
    {
        protected int line_index;

        // methods
        public abstract void execute();
        protected void setLineIndex() => Global.current_line_index = line_index;
        protected abstract void setContext();
    }

    public abstract class NestedInstruction : Instruction // ex: for, while, if, etc.
    {
        // attributes
        protected int depth;
        internal List<Instruction> instructions;
    }

    public abstract class Loop : NestedInstruction //! NEVER NEED TO UPDATE TRACERS IN LOOPS (already in all sub-instructions)
    {
        private readonly Expression _condition;
        protected bool in_loop;

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

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.while_loop_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();
            while (test())
            {
                in_loop  = true;
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
            }
            in_loop = false;
            Context.reset();
        }
    }

    public class ForLoop : Loop
    {
        private readonly Instruction _start;
        private readonly Instruction _step;

        public ForLoop(int line_index, Instruction start, Expression condition, Instruction step,
            List<Instruction> instructions) : base(line_index, condition, instructions)
        {
            _start = start;
            _step = step;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.for_loop_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();
            _start.execute();
            while (test())
            {
                in_loop  = true;
                foreach (Instruction instr in instructions)
                {
                    instr.execute();
                }
                // executing step independently bc of continue & break
                _step.execute();
            }
            in_loop = false;
            Context.reset();
          }
    }

    public class IfCondition : NestedInstruction // Don't need to update tracers here either
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

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.if_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();
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

            Context.reset();
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
            // check variable naming
            Debugging.assert(StringUtils.validVariableName(var_name)); // InvalidNamingException
            // check variable existence
            Debugging.assert(!Global.variables.ContainsKey(var_name)); // DeclaredExistingVarException
            // give a temp or predefined value
            Variable temp_value = _var_type == "auto"
                ? new NullVar() // temporary variable. doesn't have any real value (if no explicit type)
                : (Variable) Global.default_values_by_var_type[var_type].evaluate(); // (if explicit type)
            // add variable to dictionary
            Global.variables.Add(var_name, temp_value);
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.declaration_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();

            // get variable value
            Variable variable = _var_expr.evaluate();
            // is the value assigned ? (only relevant if other variable)
            variable.assertAssignment();
            // explicit typing
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

            // update all tracers
            Tracer.updateTracers();

            // reset Context
            Context.reset();
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
            // cannot check if variable is in dict or not (e.g. $l[0])
            Debugging.assert(_var_name != ""); // check this now to prevent later errors
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.assignment_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();

            // parsing new value
            Variable val = _var_value.evaluate();
            Debugging.print("assigning " + _var_name + " with expr " + _var_value.expr + " (2nd value assigned: " + val.assigned + ") and type: " + val.getTypeString());
            // assert the new is not an unassigned (only declared) variable
            val.assertAssignment();
            // set the new value
            Expression.parse(_var_name).setValue(val);

            // update all tracers
            Tracer.updateTracers();

            // reset Context
            Context.reset();
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

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.function_void_call);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();

            _called = true;
            Functions.callFunctionByName(_function_name, _args);
            // update all tracers
            Tracer.updateTracers();

            Context.reset();
        }

        public object[] getArgs() => _args;

        public bool hasBeenCalled() => _called;
    }

    public class Tracing : Instruction // no updateTracers bc values can't be changed here ...
    {
        private readonly List<Expression> _traced_vars;

        public Tracing(int line_index, List<Expression> traced_vars)
        {
            this.line_index = line_index;
            this._traced_vars = traced_vars;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.trace_execution);
            Context.setInfo(this);
        }

        public override void execute()
        {
            setContext();

            // the tracing instruction execution doesn't take any Tracer.updateTracers() calls
            foreach (Expression traced_expr in _traced_vars)
            {
                Variable traced_var = traced_expr.evaluate();
                Debugging.assert(!traced_var.isTraced());
                traced_var.startTracing();
            }

            Context.reset();
        }
    }
}
