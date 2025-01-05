
using System.Diagnostics.CodeAnalysis;

namespace TagScript.models.exceptions;


public enum ExceptionType {
    FATAL,
    WARNING,
    INFO
}

static class TagxExceptions {

    static Dictionary<int,string> ErrorCodes = new Dictionary<int, string>
    {
        // Tokenizer Errors
        { 1000, "UNRECOGNIZED TOKEN" },
        { 1001, "UNTERMINATED STRING LITERAL" },
        { 1002, "UNTERMINATED NUMBER LITERAL" },

        // Parser Errors
        { 2000, "IPARSER_ERROR | CURRENT TOKEN TYPE DOES NOT MATCH THE TOKEN PROCESSING CALL" },
        { 2001, "IPARSER_ERROR | CURRENT POSITION IS OUT OF RANGE" },
        { 2002, "IPARSER_ERROR | EMPTY TOKEN LIST" },
        { 2003, "UNEXPECTED TOKEN TYPE" },
        { 2004, "EXTRA CLOSING TAG" },
        { 2005, "TAG NAME NOT FOUND" },
        { 2006, "ATTRIBUTE NAME NOT FOUND" },
        { 2007, "TAG WASN'T CLOSED" },
        { 2008, "DUPLICATE ATTRIBUTE DECLARATION" },
        { 2009, "ATTRIBUTE VALUE NOT FOUND" },
        { 2010, "EXCEPTION WASN'T HANDLED" },

        // Runner Errors
        { 4000, "IRUNNER_ERROR | EXCEPTION WASN'T HANDLED" },
        { 4001, "TAG NOT SUPPORTED" },
        { 4003, "ATTRIBUTE VALUE ERROR" },
        { 4004, "REQUIRED ATTRIBUTE WASN'T FOUND" },
        { 4005, "VOID USED IN EXPRESSION" },
        { 4006, "DUPLICATE VARIABLE DECLARATION" },
        { 4007, "VARIABLE DOESN'T EXIST" },
        { 4008, "OPERAND NUMBER MISMATCH" },
        { 4009, "IRUNNER_ERROR | TAG BINDING MISSING" },
        { 4010, "UNEXPECTED VOID" },
        { 4011, "CONDITIONAL TYPE ERROR" },
        { 4012, "DUPLICATE CONDITIONAL" },
        { 4013, "MISSING CONDITION" },
        { 4014, "FUNCTION DOESN'T EXIST" },
        { 4015, "IRUNNER_ERROR | VARIABLE TYPE ISN'T SUPPORTED" },
        { 4016, "UNSET ACCESS ERROR" },

        // Exception handler
        { 5000, "IEXCEPTHANDLER_ERROR | ERROR CODE ISN'T REOCGNIZED" }
    };

    public static string? LatestFatalException { get; private set; }
    private static bool TryEnvironment = false;

    public static void CleanLatestException() {
        LatestFatalException = null;
    }

    public static void TryEnvironmentOn() {
        TryEnvironment = true;
    }

    public static void TryEnvironmentOff() {
        TryEnvironment = false;
    }

    public static string? SourceCode = null;

    [DoesNotReturn]
    public static Exception RaiseException(int errorNumber, string infoMessage,
        ExceptionType exType, (int, int)? LineColumn = null, int cursorLength = 1) {

            ErrorCodes.TryGetValue(errorNumber, out string? errorMessage);
            if(errorMessage is null)
                RaiseException(5000, $"Error code #{errorNumber} does not exist",
                    ExceptionType.FATAL);

            if(exType == ExceptionType.FATAL)
                LatestFatalException = errorMessage;

            if(!TryEnvironment) {
                switch(exType) {
                    case(ExceptionType.FATAL): {
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    }
                    case(ExceptionType.WARNING): {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    }
                    case(ExceptionType.INFO): {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    }
                }
 
                Console.WriteLine($"### {exType} ###");
                Console.WriteLine($"i: {infoMessage}");
                Console.WriteLine($"ERR-{errorNumber}: {errorMessage}");

                if(SourceCode is not null && LineColumn is not null) {
                    (int, int) lineColumn = ((int, int)) LineColumn;
                    Console.WriteLine($"| AT LINE {lineColumn.Item1} : COLUMN {lineColumn.Item2}");
                    string[] sourceLines = SourceCode.Split('\n');
                    // Print 2 lines before if they exist
                    for(int i = Math.Max(lineColumn.Item1 - 2, 0); i <= lineColumn.Item1; i++) {
                        Console.WriteLine($"|{i:0000}| {sourceLines[i - 1]}");
                    }
                    // Print the arrow that points
                    Console.Write("       ");
                    for(int i = 0; i < lineColumn.Item2 - 1; i++) {
                        char ch = sourceLines[lineColumn.Item1 - 1][i];
                        Console.Write( (ch == '\t') ? '\t' : ' ');
                    }
                    for(int i = 0; i < cursorLength; i++) Console.Write("^");
                    Console.WriteLine();
                }
                Console.ResetColor();
            } else {
                TryEnvironmentOff();
            }

            throw new Exception();
    }
}