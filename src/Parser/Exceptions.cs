using System;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    internal static class AquilaControlFlowExceptions
    {
        /// <summary>
        /// This <see cref="Exception"/> is raised by the "return()" function in Aquila
        /// </summary>
        public class ReturnValueException : Exception
        {
            /// <summary>
            /// The literal representation of the returned value (as an <see cref="Exception"/> string)
            /// </summary>
            private readonly string _return_expr;
            
            /// <summary>
            /// Raised by the "return()" function in Aquila
            /// </summary>
            /// <param name="expr_as_msg"> The expression string which should be returned by the <see cref="Algorithm"/> or <see cref="Function"/></param>
            public ReturnValueException(string expr_as_msg)
            {
                Debugging.print("Resetting Context in Return Exception constructor. Context: " +
                                (Context.StatusEnum) Context.getStatus());
                Context.reset();
                _return_expr = expr_as_msg;
            }

            /// <summary>
            /// Getter for the <see cref="Expression"/> string
            /// </summary>
            /// <returns> <see cref="Expression"/> string</returns>
            public string getExprStr() => _return_expr;
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised by the "break()" function in Aquila
        /// </summary>
        public class BreakException : Exception
        {
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised by the "continue()" function in Aquila
        /// </summary>
        public class ContinueException : Exception
        {
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when the Local or Main scope is getting reset when it shouldn't
        /// </summary>
        public class InvalidScopeResetError : Exception
        {
            /// <summary>
            /// Raised when the Local or Main scope is getting reset when it shouldn't
            /// </summary>
            public InvalidScopeResetError()
            {
            }
            
            /// <summary>
            /// Raised when the Local or Main scope is getting reset when it shouldn't
            /// </summary>
            public InvalidScopeResetError(string message) : base(message)
            {
            }
        }
    }
    
    /// <summary>
    /// This holds all the defined custom <see cref="Exception"/>s in Aquila.
    /// All the other <see cref="Exception"/>s will be raised as raw <see cref="Exception"/>s
    /// </summary>
    internal static class AquilaExceptions // Aquila -> name of the programming language
    {
        /// <summary>
        /// Syntactic exceptions are all the Exceptions that occur when reading pure code.
        /// They are basically unclosed brackets, parentheses or incomprehensible instructions
        /// </summary>
        internal static class SyntaxExceptions
        {
            /// <summary>
            /// This <see cref="Exception"/> is raised when a sentence (instruction, expression, etc) is not understood
            /// </summary>
            public class SyntaxError : Exception
            {
                /// <summary>
                /// Usage: throw new AquilaExceptions.SyntaxExceptions.SyntaxError($"The sentence \"{sentence}\" is not understood");
                /// </summary>
                /// <param name="message"></param>
                public SyntaxError(string message) : base(message)
                {
                }
            }
            
            /// <summary>
            /// This <see cref="Exception"/> is raised when a tag (comment tag, parenthesis, bracket, etc.) does not have a corresponding closing tag
            /// </summary>
            public class UnclosedTagError : Exception
            {
                /// <summary>
                /// Usage: throw new AquilaExceptions.SyntaxExceptions.UnclosedTagError("Unclosed parenthesis");
                /// </summary>
                /// <param name="message"> Error message</param>
                public UnclosedTagError(string message) : base(message)
                {
                }
            }

            /// <summary>
            /// This <see cref="Exception"/> is raised when list expression is invalid
            /// </summary>
            public class InvalidListExpressionError : Exception
            {
                /// <summary>
                /// Usage: throw new AquilaExceptions.SyntaxExceptions.InvalidListExpressionError($"List expression: \"{list_expr}\" is invalid");
                /// </summary>
                /// <param name="message"> Error message</param>
                public InvalidListExpressionError(string message) : base(message)
                {
                }
                
                /// <summary>
                /// Used indirectly by other, external, <see cref="Exception"/>s
                /// </summary>
                /// <param name="message"> Error message</param>
                /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
                public InvalidListExpressionError(string message, Exception inner) : base(message, inner)
                {
                }
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when the Runtime Environment finds itself in a state where
        /// it cannot continue code execution. This Error is not caused by the code directly, but by a bug
        /// in the Interpreter itself
        /// </summary>
        public class RuntimeError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownKeywordError("The inner ReturnValueException in the TargetInvocationException is null");
            /// </summary>
            /// <param name="message">Error message</param>
            public RuntimeError(string message) : base(message)
            {
            }
            
            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public RuntimeError(string message, Exception inner) : base(message, inner)
            {
            }
        }
        
        /// <summary>
        /// This <see cref="Exception"/> is raised when an unknown instruction keyword is used in the code
        /// </summary>
        public class UnknownKeywordError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownKeywordError($"The keyword\"{keyword}\" is unknown");
            /// </summary>
            /// <param name="message"> Error message</param>
            public UnknownKeywordError(string message) : base(message)
            {
            }
    
            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public UnknownKeywordError(string message, Exception inner) : base(message, inner)
            {
            }
        }
        
        /// <summary>
        /// This <see cref="Exception"/> is raised when another variable type was expected
        /// <para/>Example trigger cases:
        /// <para/>* decl int x 0
        /// <para/>* decl listr l2 // "listr" instead of "list"
        /// </summary>
        public class InvalidTypeError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidTypeError($"The type \"{var_type}\" was not expected");
            /// </summary>
            /// <param name="message"> Error message</param>
            public InvalidTypeError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public InvalidTypeError(string message, Exception inner) : base(message, inner)
            {
            }
        }
        
        /// <summary>
        /// This <see cref="Exception"/> is raised when a variable type name is not recognized
        /// <para/>Example trigger cases:
        /// <para/>* decl inz x 0 // "inz" instead of "int"
        /// <para/>* decl listr l2 // "listr" instead of "list"
        /// </summary>
        public class UnknownTypeError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownTypeError($"The type \"{var_type}\" is not recognized");
            /// </summary>
            /// <param name="message"> Error message</param>
            public UnknownTypeError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public UnknownTypeError(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when an invalid variable classifier is used
        /// <para/>Example trigger cases:
        /// <para/>* decl int x random // a random number cannot be a constant
        /// <para/>* a variable declaration using the value of an argument in a function
        /// </summary>
        public class InvalidVariableClassifierException : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidVariableClassifierException($"The \"{var_classifier}\" was unexpected in this context");
            /// </summary>
            /// <param name="message"> Error message</param>
            public InvalidVariableClassifierException(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public InvalidVariableClassifierException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a variable name does not exist in the current context
        /// <para/>Example trigger cases:
        /// <para/>* decl auto var $var2 // when "var2" does was never declared
        /// <para/>* $var1 = $var3 // when "var3" was never declared
        /// </summary>
        public class NameError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.NameError($"The variable \"{var_name}\"" does not exist in the current Context");
            /// </summary>
            /// <param name="message"> Error message</param>
            public NameError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public NameError(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a function name does not exist
        /// </summary>
        public class FunctionNameError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.FunctionNameError($"The function \"{function_name}\" does not exist");
            /// </summary>
            /// <param name="message"> Error message</param>
            public FunctionNameError(string message) : base(message)
            {
            }
            
            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public FunctionNameError(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a <see cref="Variable"/> representing a numeric value is divided by
        /// the corresponding numerical representation of 0 (zero)
        /// </summary>
        public class ZeroDivisionException : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.ZeroDivisionException("Cannot divide by Zero");
            /// </summary>
            /// <param name="message"> Error message</param>
            public ZeroDivisionException(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public ZeroDivisionException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when the loading of a library failed
        /// </summary>
        public class LibraryLoadingFailedError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.LibraryLoadingFailedError($"Unable to load library at \"{lib_path}\");
            /// </summary>
            /// <param name="message"></param>
            public LibraryLoadingFailedError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public LibraryLoadingFailedError(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a macro keyword does not exist
        /// </summary>
        public class UnknownMacroError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownMacroError($"Macro keyword \"{macro_keyword}\" is unknown");
            /// </summary>
            /// <param name="message"> Error message</param>
            public UnknownMacroError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public UnknownMacroError(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a variable or function is not used in the right way
        /// </summary>
        public class InvalidUsageException : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidUsageException($"The variable \"{var_name}\" is not assigned but its value is accessed");
            /// </summary>
            /// <param name="message"></param>
            public InvalidUsageException(string message) : base(message)
            {
            }
            
            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public InvalidUsageException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when the index to access a list is out of the list's bounds
        /// </summary>
        public class InvalidIndexException : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidIndexException($"Index {index} out of bounds");
            /// </summary>
            /// <param name="message"></param>
            public InvalidIndexException(string message) : base(message)
            {
            }
            
            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public InvalidIndexException(string message, Exception inner) : base(message, inner)
            {
            }
        }
    }
}
