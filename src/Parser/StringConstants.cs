namespace Parser
{
    public static class StringConstants
    {
        public static class Types
        {
            public const string INT_TYPE = "int";
            public const string FLOAT_TYPE = "float";
            public const string BOOl_TYPE = "bool";
            public const string LIST_TYPE = "list";
            public const string NULL_TYPE = "null";
            public const string AUTO_TYPE = "auto";
        }

        public static class Keywords
        {
            // declaration
            public const string DECLARATION_KEYWORD = "decl";
            public const string SAFE_DECLARATION_KEYWORD = "safe";
            public const string OVERWRITE_DECLARATION_KEYWORD = "overwrite";
            public const string GLOBAL_DECLARATION_KEYWORD = "global";
            public const string CONST_DECLARATION_KEYWORD = "const";
            // conditional access blocks
            public const string IF_KEYWORD = "if";
            public const string ELSE_KEYWORD = "else";
            public const string END_IF_KEYWORD = "end-if";
            public const string FOR_KEYWORD = "for";
            public const string END_FOR_KEYWORD = "end-for";
            public const string WHILE_KEYWORD = "while";
            public const string END_WHILE_KEYWORD = "end-while";
            // functions
            public const string FUNCTION_KEYWORD = "function";
            public const string END_FUNCTION_KEYWORD = "end-function";
            public const string RECURSIVE_KEYWORD = "recursive";
            // control flow
            public const string RETURN_KEYWORD = "return";
            public const string BREAK_KEYWORD = "break";
            public const string CONTINUE_KEYWORD = "continue";
            // other
            public const string TRACE_KEYWORD = "trace";
        }

        public static class ALOperations
        {
            //
        }

        public static class PredefinedFunctions
        {
            public const string LENGTH_FUNCTION_NAME = "length";
            public const string LIST_AT_FUNCTION_NAME = "list_at";
            public const string COPY_LIST_FUNCTION_NAME = "copy_list";
            public const string FLOAT2INT_FUNCTION_NAME = "float2int";
            public const string INT2FLOAT_FUNCTION_NAME = "int2float";
            public const string RANDOM_FUNCTION_NAME = "random";
            public const string SQRT_FUNCTION_NAME = "sqrt";
            public const string INTERACTIVE_CALL_FUNCTION_NAME = "interactive_call";
            public const string PRINT_VALUE_FUNCTION_NAME = "print_value";
            public const string PRINT_VALUE_ENDL_FUNCTION_NAME = "print_value_endl";
            public const string PRINT_STR_FUNCTION_NAME = "print_str";
            public const string PRINT_STR_ENDL_FUNCTION_NAME = "print_str_endl";
            public const string PRINT_ENDL_FUNCTION_NAME = "print_endl";
            public const string DELETE_VAR_FUNCTION_NAME = "delete_var";
            public const string DELETE_VALUE_AT_FUNCTION_NAME = "delete_value_at";
            public const string INSERT_VALUE_AT_FUNCTION_NAME = "insert_value_at";
            public const string APPEND_VALUE_FUNCTION_NAME = "append_value";
            public const string SWAP_FUNCTION_NAME = "swap";
            public const string TRACE_MODE_FUNCTION_NAME = "trace_mode";
        }

        public static class Other
        {
            public const string VARIABLE_PREFIX = "$";
            public const string NULL_VARIABLE_NAME = "null";
        }
    }
}