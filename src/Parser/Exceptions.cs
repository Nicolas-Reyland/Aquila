using System;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// This holds all the defined custom <see cref="Exception"/>s in Aquila.
    /// All the other <see cref="Exception"/>s will be raised as raw <see cref="Exception"/>s
    /// </summary>
    internal static class AquilaExceptions // Aquila -> name of the programming language
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
            /// This is raised by the "return()" function in Aquila
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
    }
}
