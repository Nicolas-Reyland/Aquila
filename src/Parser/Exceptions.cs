using System;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Parser
{
    /// <summary>
    /// 
    /// </summary>
    public static class AquilaExceptions // Aquila -> name of the programming language
    {
        /// <summary>
        /// 
        /// </summary>
        public class ReturnValueException : Exception
        {
            private readonly string _return_expr;
            public ReturnValueException(string message)
            {
                _return_expr = message;
            }

            public string getExpr() => _return_expr;
        }

        /// <summary>
        /// This <see cref="Exception"/> is raised when a variable type name is not recognized
        /// <para/>Example trigger cases:
        /// <para/>* declare inz x 0 // "inz" instead of "int"
        /// <para/>* declare listr l2 // "listr" instead of "list"
        /// </summary>
        public class UnknownTypeException : Exception
        {
            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownTypeException();
            /// </summary>
            public UnknownTypeException()
            {
                //
            }

            /// <summary>
            /// Usage: throw new AquilaExceptions.UnknownTypeException("The variable type \"" + var_type + "\" is not recognized");
            /// </summary>
            /// <param name="message"> Error message</param>
            public UnknownTypeException(string message) : base(message)
            {
                //
            }

            /// <summary>
            /// Used indirectly by other, external, <see cref="Exception"/>s
            /// </summary>
            /// <param name="message"> Error message</param>
            /// <param name="inner"> Inner <see cref="Exception"/> instance</param>
            public UnknownTypeException(string message, Exception inner) : base(message, inner)
            {
                //
            }
        }
    }
}
