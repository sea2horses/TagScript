// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using TagScript.models;
using TagScript.models.exceptions;

namespace TagScript.main;


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

        try {
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
        } catch(Exception ex) {
            Console.WriteLine($"Code interpreting has stopped due to an exception");
            if(ex.Message != string.Empty) Console.WriteLine(ex.Message);
            Console.WriteLine($"\nWould you like to open the documentation? (Y/n)");
            if( (Console.ReadLine() ?? "").ToUpper() == "Y" ) {
                // Thanks a lot BERZ in StackOverflow :33
                // https://stackoverflow.com/questions/10989709/open-a-html-file-using-default-web-browser
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(@"docs\index.html")
                {
                    UseShellExecute = true
                };
                p.Start();
            }
        }
    }
}