// See https://aka.ms/new-console-template for more information
using System;
using TagScript.models;

namespace TagScript.main;

static class TagxExceptions {
    public enum ExceptionType {
        FATAL,
        WARNING,
        INFO
    }

    private static bool TryEnvironment = false;
    private static string? TryException = null;

    public static void TryEnvironmentOn() { TryEnvironment = true; TryException = null; }
    public static void TryEnvironmentOff() { TryEnvironment = false; }
    public static string? GetTryException() { return TryException; }

    public static string? SourceCode = null;

    public static void RaiseException(string infoMessage,
        ExceptionType exType, (int, int) LineColumn, int cursorLength = 1) {

            if(TryEnvironment) {
                TryEnvironmentOff();
                TryException = infoMessage;
            } else {
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

                Console.WriteLine($"### {exType} ###: {infoMessage}");
                Console.WriteLine($"| AT LINE {LineColumn.Item1} : COLUMN {LineColumn.Item2}");

                if(SourceCode is not null) {
                    string[] sourceLines = SourceCode.Split('\n');
                    // Print 2 lines before if they exist
                    for(int i = Math.Max(LineColumn.Item1 - 2, 0); i <= LineColumn.Item1; i++) {
                        Console.WriteLine($"|{i:0000}| {sourceLines[i - 1]}");
                    }
                    // Print the arrow that points
                    Console.Write("       ");
                    for(int i = 0; i < LineColumn.Item2 - 1; i++) {
                        char ch = sourceLines[LineColumn.Item1 - 1][i];
                        Console.Write( (ch == '\t') ? '\t' : ' ');
                    }
                    for(int i = 0; i < cursorLength; i++) Console.Write("^");
                    Console.WriteLine();
                }
                Console.ResetColor();
                Environment.Exit(1);
            }
    }
}

static class Program {

    static class RunInformation {
        static private HashSet<string> Flags = [];
        static private string? File = null;
        static public string SourceCode = "";

        static public bool FlagExists(string flagName)
            => Flags.Contains(flagName);    
    }

    public static class TagxDebug {
        public static void Log(string message) {
            if(RunInformation.FlagExists("debug"))
                Console.Write(message);
        }

        public static void LogLine(string message) {
            if(RunInformation.FlagExists("debug"))
                Console.WriteLine(message);
        }
    }

    static void Main(string[] args) {
        string sourceCode = "<output>\"Hello World!\"</output>";
        if(args.Length != 0) {
            FileStream fStream = new FileStream(args[0], FileMode.Open);
            StreamReader reader = new StreamReader(fStream);

            sourceCode = reader.ReadToEnd();

            reader.Close();
            fStream.Close();
        }

        Console.WriteLine($"Source Code: \n{sourceCode}");
        TagxExceptions.SourceCode = sourceCode;

        Tokenizer tokenizer = new(sourceCode);
        List<Token> tokenList = tokenizer.Parse();

        Console.WriteLine("Tokenizing Output:\n");
        foreach(Token token in tokenList) {
            Console.WriteLine(token);
        }

        TagParser tagParser = new(tokenList);
        Console.WriteLine("\n\nTag Parsing Output:\n");
        List<Tag> tagList = tagParser.ParseList();
        Tag masterTag = new Tag("program", tagList);

        Console.WriteLine("\n\nTag Tree:");
        TagFormatter.DisplayTag(masterTag); 

        Console.WriteLine("\n\nRunner Output:");
        TagScriptInterpreter tagx = new(masterTag);

        tagx.Run();

        Console.WriteLine("\n\nVariable dump:");
        foreach(Variable var in tagx.variables) {
            Console.WriteLine(var);
        }
    }
}