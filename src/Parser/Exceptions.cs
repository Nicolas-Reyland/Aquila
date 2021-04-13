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
            /// <summary>
            /// Raised by the "break()" function in Aquila
            /// </summary>
            public BreakException() : base()
            {
            }
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised by the "continue()" function in Aquila
        /// </summary>
        public class ContinueException : Exception
        {
            /// <summary>
            /// Raised by the "continue()" function in Aquila
            /// </summary>
            public ContinueException() : base()
            {
            }
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
        }
    }
    /// <summary>
    /// This holds all the defined custom <see cref="Exception"/>s in Aquila.
    /// All the other <see cref="Exception"/>s will be raised as raw <see cref="Exception"/>s
    /// </summary>
    internal static class AquilaExceptions // Aquila -> name of the programming language
    {
        public class UnknownKeywordError : Exception
        {
            public UnknownKeywordError()
            {
            }

            public UnknownKeywordError(string message) : base(message)
            {
            }

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
            /// Usage: throw new AquilaExceptions.InvalidTypeError();
            /// </summary>
            public InvalidTypeError()
            {
            }

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
        /// This <see cref="Exception"/> is raised when an invalid variable classifier is used
        /// <para/>Example trigger cases:
        /// <para/>* decl int x random // a random number cannot be a constant
        /// <para/>* a variable declaration using the value of an argument in a function
        /// </summary>
        public class InvalidVariableClassifier : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidVariableClassifier();
            /// </summary>
            public InvalidVariableClassifier()
            {
            }

            /// <summary>
            /// Usage: throw new AquilaExceptions.InvalidVariableClassifier($"The \"{var_classifier}\" was unexpected in this context");
            /// </summary>
            /// <param name="message"> Error message</param>
            public InvalidVariableClassifier(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public InvalidVariableClassifier(string message, Exception inner) : base(message, inner)
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
            /// Usage: throw new AquilaExceptions.UnknownTypeError();
            /// </summary>
            public UnknownTypeError()
            {
            }

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
        /// This <see cref="Exception"/> is raised when a variable name does not exist in the current context
        /// <para/>Example trigger cases:
        /// <para/>* decl auto var $var2 // when "var2" does was never declared
        /// <para/>* $var1 = $var3 // when "var3" was never declared
        /// </summary>
        public class NameError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.NameError();
            /// </summary>
            public NameError()
            {
            }
            
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
            /// Usage: throw new AquilaExceptions.FunctionNameError();
            /// </summary>
            public FunctionNameError()
            {
            }
            
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
            /// Usage: throw new AquilaExceptions.ZeroDivisionException();
            /// </summary>
            public ZeroDivisionException()
            {
            }

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
            /// Usage: throw new AquilaExceptions.LibraryLoadingFailedError();
            /// </summary>
            public LibraryLoadingFailedError()
            {
            }

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
            /// Usage: throw new AquilaExceptions.UnknownMacroError();
            /// </summary>
            public UnknownMacroError()
            {
            }

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
        /// This <see cref="Exception"/> is raised when a tag (comment tag, parenthesis, bracket, etc.) does not have a corresponding closing tag
        /// </summary>
        public class UnclosedTagError : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnclosedTagError();
            /// </summary>
            public UnclosedTagError()
            {
            }

            /// <summary>
            /// Usage: throw new AquilaExceptions.UnclosedTagError("Unclosed parenthesis");
            /// </summary>
            /// <param name="message"> Error message</param>
            public UnclosedTagError(string message) : base(message)
            {
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public UnclosedTagError(string message, Exception inner) : base(message, inner)
            {
            }
        }
    }
}
