// See https://aka.ms/new-console-template for more information
using System;
using TagScript.models;

static class Program {
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