// See https://aka.ms/new-console-template for more information
using System;
using TagScript.models;

static class Program {
    static void Main() {
        string sourceCode =
@"
# This is for declaring a constant variable
<variable type=""int"" name=""test"" value=""5.0"" constant/>
# This is the output field
<output>""I'm ""<get type=""variable"" name=""test""/>"" year's old!""</output>";
        Console.WriteLine($"Source Code: {sourceCode}");

        Tokenizer tokenizer = new(sourceCode);
        List<Token> tokenList = tokenizer.Parse();

        Console.WriteLine("Tokenizing Output:\n");
        foreach(Token token in tokenList) {
            Console.WriteLine(token);
        }

        TagParser tagParser = new(tokenList);
        Console.WriteLine("\n\nTag Parsing Output:\n");
        List<Tag> tagList = tagParser.ParseList();

        Console.WriteLine("\n\nTag Tree:");
        foreach(Tag tag in tagList) {
            TagFormatter.DisplayTag(tag);
        }
    }
}