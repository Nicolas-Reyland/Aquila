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
        public abstract dynamic[] getTranslatorInfo();
        protected abstract void updateTestModeValues();
    }

    public class MultiInstruction : Instruction
    {
        private readonly Instruction[] _instructions;
        
        public MultiInstruction(Instruction[] instructions)
        {
            _instructions = instructions;
        }
        
        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();

            foreach (Instruction instruction in _instructions)
            {
                instruction.execute();
            }
            
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.multi_instructions);
            Context.setInfo(this);
            updateTestModeValues();
        }

        // ReSharper disable once CoVariantArrayConversion
        public override dynamic[] getTranslatorInfo() => _instructions;

        protected override void updateTestModeValues()
        {
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }
    }

    public abstract class NestedInstruction : Instruction // ex: for, while, if, etc.
    {
        // attributes
        internal List<Instruction> instructions;
    }

    public abstract class Loop : NestedInstruction //! NEVER NEED TO UPDATE TRACERS IN LOOPS (already in all sub-instructions)
    {
        private readonly Expression _condition;
        protected bool in_loop;

        protected Loop(int line_index, Expression condition, List<Instruction> instructions)
        {
            this.line_index = line_index;
            _condition = condition;
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
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => new dynamic[] {_condition, instructions};
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
            Global.newLocalContextScope();
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();
            while (test())
            {
                in_loop  = true;
                int local_context_stack_count = Global.getLocalScopeDepth();
                foreach (Instruction instr in instructions)
                {
                    Global.newLocalContextScope();
                    try
                    {
                        instr.execute();
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        if (e.InnerException == null) throw;

                        if (e.InnerException is AquilaControlFlowExceptions.BreakException)
                        {
                            goto EndOfWhileLoopLabel;
                        }

                        if (e.InnerException is AquilaControlFlowExceptions.ContinueException)
                        {
                            break;
                        }
                    }
                    catch (AquilaControlFlowExceptions.BreakException)
                    {
                        goto EndOfWhileLoopLabel;
                    }
                    catch (AquilaControlFlowExceptions.ContinueException)
                    {
                        // just pass to the next while-loop iteration
                        break;
                    }
                    Global.resetLocalContextScope();
                }
                
                Global.resetLocalScopeUntilDepthReached(local_context_stack_count);
            }
            EndOfWhileLoopLabel:
            in_loop = false;
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
            Global.resetLocalContextScope();
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
            Global.newLocalContextScope();
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();
            _start.execute();
            while (test())
            {
                in_loop  = true;
                int local_context_stack_count = Global.getLocalScopeDepth();
                
                foreach (Instruction instr in instructions)
                {
                    Global.newLocalContextScope();
                    try
                    {
                        instr.execute();
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        if (e.InnerException == null) throw;

                        if (e.InnerException is AquilaControlFlowExceptions.BreakException)
                        {
                            goto EndOfForLoopLabel;
                        }

                        if (e.InnerException is AquilaControlFlowExceptions.ContinueException)
                        {
                            break;
                        }
                    }
                    catch (AquilaControlFlowExceptions.BreakException)
                    {
                        goto EndOfForLoopLabel;
                    }
                    catch (AquilaControlFlowExceptions.ContinueException)
                    {
                        // just pass to the next for-loop iteration
                        break;
                    }

                    Global.resetLocalContextScope();
                }

                Global.resetLocalScopeUntilDepthReached(local_context_stack_count);
                // executing step independently bc of continue & break
                _step.execute();
            }
            EndOfForLoopLabel:
            in_loop = false;
            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
            Global.resetLocalContextScope();
        }

        public override dynamic[] getTranslatorInfo()
        {
            dynamic[] base_info = base.getTranslatorInfo();
            return new[] {_start, base_info[0], _step, base_info[1]};
        }
    }

    public class IfCondition : NestedInstruction // Don't need to update tracers here either
    {
        private readonly Expression _condition;
        private readonly List<Instruction> _else_instructions;

        public IfCondition(int line_index, Expression condition, List<Instruction> instructions, List<Instruction> else_instructions)
        {
            this.line_index = line_index;
            _condition = condition;
            this.instructions = instructions;
            _else_instructions = else_instructions;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.if_execution);
            Context.setInfo(this);
            Global.newLocalContextScope();
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();
            
            if ( ((BooleanVar) _condition.evaluate()).getValue() )
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

            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
            Global.resetLocalContextScope();
        }
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => new dynamic[] { _condition, instructions, _else_instructions };
    }

    public class Declaration : Instruction
    {
        private readonly string _var_name;
        private readonly Expression _var_expr;
        private readonly string _var_type;
        private readonly bool _assignment;
        private readonly bool _constant;
        private readonly bool _global;

        public Declaration(int line_index, string var_name, Expression var_expr, string var_type = StringConstants.Types.AUTO_TYPE, bool assignment = true,
            bool safe_mode = false,
            bool overwrite = false,
            bool constant = false,
            bool global = false)
        {
            // mode: 0 -> new var (must not exist); 1 -> force overwrite (must exist); 2 -> safe overwrite (can exist)
            this.line_index = line_index;
            _var_name = var_name;
            _var_expr = var_expr;
            _var_type = var_type;
            _assignment = assignment;
            _constant = constant;
            _global = global;
            // check variable naming
            Debugging.assert(StringUtils.validObjectName(var_name),
                new AquilaExceptions.SyntaxExceptions.SyntaxError($"Invalid object name \"{var_name}\""));
            // the declaration is not initiated in the right scope... cannot do this here
            bool var_exists = Global.variableExistsInCurrentScope(var_name);
            Debugging.print("new declaration: var_name = " + var_name +
                            ", var_expr = ", var_expr.expr,
                            ", var_type = ", var_type,
                            ", assignment = ", assignment,
                            ", mode = ", StringUtils.boolString(safe_mode, overwrite, _constant, _global),
                            ", exists = ", var_exists);
            // should not check overwriting modes if this is true
            if (Global.getSetting("implicit declaration in assignment"))
            {
                Debugging.print("implicit declaration in assignments, so skipping var existence checks (var exists: ", var_exists, ")");
                return;
            }

            if (safe_mode && var_exists) Debugging.assert(!Global.variableFromName(var_name).isTraced());
            if (overwrite) Debugging.assert(var_exists);
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.declaration_execution);
            Context.setInfo(this);
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();

            // get variable value
            Variable variable_value = _var_expr.evaluate();
            // is the value assigned ? (only relevant if other variable)
            variable_value.assertAssignment();
            Variable variable = Variable.fromRawValue(variable_value.getRawValue());
            // keep track of source vars -> should do something generic for lots of attributes ...
            if (variable is NumericalValue)
            {
                ((NumericalValue) variable).source_vars = new Dictionary<string, NumericalValue>(((NumericalValue) variable_value).source_vars);
            }
            variable.setName(_var_name);
            // explicit typing
            if (_var_type != StringConstants.Types.AUTO_TYPE)
            {
                Debugging.print("checking variable explicit type");
                Expression default_value = Global.default_values_by_var_type[_var_type];
                Debugging.assert( variable_value.hasSameParent(default_value.evaluate()) ); // TypeException
            }
            // constant
            if (_constant)
            {
                if (variable_value.isConst())
                {
                    variable.setConst();
                }
                else
                {
                    throw new AquilaExceptions.InvalidVariableClassifierException(
                        "The \"const\" cannot be used when assigning to a non-const value");
                }
            }
            // actually declare it to its value
            if (_global)
            {
                Global.addGlobalVariable(_var_name, variable);
            }
            else
            {
                Global.getCurrentDict()[_var_name] = variable; // overwriting is mandatory
                if (_assignment) variable.assign(); // should not need this, but doing anyway
                else variable.assigned = false;
            }
            Debugging.print("finished declaration with value assignment: ", variable.assigned);

            // automatic tracing ?
            if (_assignment && Global.getSetting("auto trace"))
            {
                Debugging.print("Tracing variable: \"auto trace\" setting set to true");
                // Does NOT work by simply doing "variable.startTracing()", and idk why
                Tracing tracing_instr = new RawInstruction($"trace ${_var_name}", line_index).toInstr() as Tracing;
                //Tracing tracing_instr = new Tracing(line_index, new List<Expression>{_var_expr}); // <- does not work either :(
                tracing_instr.execute();
            }

            // update all tracers
            Tracer.updateTracers();

            // reset Context
            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
        }
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
            Global.test_values["variable names"].Add(_var_name);
        }

        public override dynamic[] getTranslatorInfo() => new dynamic[] { _var_name, _var_type, _var_expr };
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
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();

            Debugging.print("Assignment: ", _var_name, " = ", _var_value.expr);
            
            // set the new value
            Variable variable;
            try
            {
                variable = Expression.parse(_var_name);
            }
            catch (AquilaExceptions.NameError)
            {
                // implicit declaration
                if (!Global.getSetting("implicit declaration in assignment")) throw;
                Debugging.print("Implicit declaration in Assignment!");
                Declaration decl = new Declaration(line_index, _var_name.Substring(1), _var_value); // in the Assignment constructor: already check if _var_name != ""
                decl.execute();

                // update all tracers
                Tracer.updateTracers();

                // reset Context
                // Smooth Context
                Context.resetUntilCountReached(context_integrity_check);
                Context.reset();

                return;
            }

            // parsing new value
            Variable val = _var_value.evaluate();
            Debugging.print("assigning " + _var_name + " with expr " + _var_value.expr + " with value " + val + " (2nd value assigned: " + val.assigned + ") and type: " + val.getTypeString());
            // assert the new is not an unassigned (only declared) variable
            val.assertAssignment();

            if (variable.hasSameParent(val))
            {
                variable.setValue(val);
            }
            else
            {
                throw new AquilaExceptions.InvalidTypeError("You cannot change the type of your variables (" + variable.getTypeString() + " -> " + val.getTypeString() + "). This will never be supported because it would be considered bad style.");
            }

            // update all tracers
            Tracer.updateTracers();

            // reset Context
            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
        }
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => new dynamic[] { _var_name, _var_value };
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
            _args = function_name == StringConstants.Keywords.RETURN_KEYWORD && args.Length == 0 ? new object[]{ new Expression(StringConstants.Other.VARIABLE_PREFIX + StringConstants.Other.NULL_VARIABLE_NAME) } : args;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.predefined_function_call);
            Context.setInfo(this);
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            // int context_integrity_check = Context.getStatusStackCount();

            _called = true;
            Functions.callFunctionByName(_function_name, _args);
            // update all tracers
            Tracer.updateTracers();

            Context.reset();
        }

        public object[] getArgs() => _args;

        public bool hasBeenCalled() => _called;
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => new dynamic[] { _function_name, _args };
    }

    public class FunctionDef : Instruction
    {
        private readonly Function _func;
        public FunctionDef(int line_index, Function func)
        {
            this.line_index = line_index;
            _func = func;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.user_function_call);
            Context.setInfo(this);
            updateTestModeValues();
        }
        
        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();
            Functions.addUserFunction(_func);
            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
        }
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => _func.translatorInfo();
    }

    public class Tracing : Instruction // no updateTracers bc values can't be changed here ...
    {
        private readonly List<Expression> _traced_vars;

        public Tracing(int line_index, List<Expression> traced_vars)
        {
            this.line_index = line_index;
            _traced_vars = traced_vars;
        }

        protected override void setContext()
        {
            setLineIndex();
            Context.setStatus(Context.StatusEnum.trace_execution);
            Context.setInfo(this);
            updateTestModeValues();
        }

        public override void execute()
        {
            setContext();
            int context_integrity_check = Context.getStatusStackCount();

            // the tracing instruction execution doesn't take any Tracer.updateTracers() calls
            foreach (Expression traced_expr in _traced_vars)
            {
                Variable traced_var = traced_expr.evaluate();
                if (traced_var.isTraced())
                {
                    Debugging.print("trying to trace a variable that is already traced: " + traced_expr.expr);
                    continue;
                }
                traced_var.startTracing();
            }

            // Smooth Context
            Context.resetUntilCountReached(context_integrity_check);
            Context.reset();
        }
        
        protected override void updateTestModeValues()
        {
            Global.instruction_count++;
            if (!Global.getSetting("test mode")) return;
            Algorithm.testModeInstructionUpdate();
        }

        public override dynamic[] getTranslatorInfo() => null;
    }
}
