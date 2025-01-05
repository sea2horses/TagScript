using System;
using System.Drawing;
using TagScript.models.exceptions;

namespace TagScript.models {

    public enum TagType {
        UNIVERSAL,
        INPUT,
        OUTPUT,
        LITERAL_TEXT,
        LITERAL_NUMBER,
        VARIABLE,
        CONSTANT,
        GET,
        EVALUATE,
        BREAK,
        OPERATIVE,
        IF,
        ELSE,
        ELSEIF,
        WHILE,
        CONDITION,
        FUNCTION,
        CALL,
        ARGUMENT,
        SET,
        TRY,
        CATCH,
        RETURN
    }

    public enum OperativeTagType {
        SUM,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        RAISE,
        ROOT,
        MODULO,
        EQUALS,
        NEGATE,
        OR,
        AND,
    }

    public class Tag {

        static Dictionary<string, TagType> tagBindings = new(){
            {"text-lit", TagType.LITERAL_TEXT},
            {"number-lit", TagType.LITERAL_NUMBER},
            {"txt", TagType.LITERAL_TEXT}, // Shorthand for Text Literal
            {"num", TagType.LITERAL_NUMBER}, // Shorthand for Number Literal
            {"output", TagType.OUTPUT},
            {"out", TagType.OUTPUT}, // Shorthand for Output
            {"br", TagType.BREAK},
            {"variable", TagType.VARIABLE},
            {"var", TagType.VARIABLE},
            {"get", TagType.GET},
            {"input", TagType.INPUT},
            {"in",TagType.INPUT},
            {"if", TagType.IF},
            {"elif", TagType.ELSEIF},
            {"else", TagType.ELSE},
            {"while", TagType.WHILE},
            {"condition", TagType.CONDITION},
            {"eval", TagType.EVALUATE},
            {"set", TagType.SET},
            {"call", TagType.CALL},
            {"arg", TagType.ARGUMENT},
            {"try", TagType.TRY},
            {"catch",TagType.CATCH},
            {"return", TagType.RETURN},
            {"add", TagType.OPERATIVE},
            {"subtract",TagType.OPERATIVE},
            {"multiply",TagType.OPERATIVE},
            {"divide", TagType.OPERATIVE},
            {"raise",TagType.OPERATIVE},
            {"root", TagType.OPERATIVE},
            {"modulo",TagType.OPERATIVE},
            {"compare", TagType.OPERATIVE},
            {"or", TagType.OPERATIVE},
            {"and", TagType.OPERATIVE},
            {"negate", TagType.OPERATIVE}
        };

        static Dictionary<string, OperativeTagType> operativeBindings = new(){
            {"add", OperativeTagType.SUM},
            {"subtract", OperativeTagType.SUBTRACT},
            {"multiply", OperativeTagType.MULTIPLY},
            {"divide", OperativeTagType.DIVIDE},
            {"raise", OperativeTagType.RAISE},
            {"root", OperativeTagType.ROOT},
            {"modulo", OperativeTagType.MODULO},
            {"compare", OperativeTagType.EQUALS},
            {"negate", OperativeTagType.NEGATE}
        };

        public string TagName { get; }
        public TagType Type { get; private set; } = TagType.UNIVERSAL;
        public Dictionary<string, string> Attributes { get; }
        public List<Tag> Body { get; }
        public (int, int) Position { get; set; } = (-1, -1);

        public Tag(string tagName, Dictionary<string, string> attributes, List<Tag> body) {
            this.TagName = tagName;
            this.Attributes = attributes;
            this.Body = body;

            if(tagBindings.TryGetValue(TagName, out TagType type))
                this.Type = type;
        }

        public Tag(string tagName, List<Tag> body) : this(tagName, [], body) {}

        public Tag(string tagName, Dictionary<string, string> attributes) : this(tagName, attributes, []) {}

        public Tag(string tagName) : this(tagName, [], []) {}

        public bool AttributeExists(string name) {
            return this.Attributes.TryGetValue(name, out _);
        }

        public string GetAttribute(string name) {
            if(!this.Attributes.TryGetValue(name, out string? value))
                throw TagxExceptions.RaiseException(4004, $"Tag '{this.TagName}' is missing attribute '{name}'",
                    ExceptionType.FATAL, this.Position, this.TagName.Length);
            
            return value ?? "";
        }

        public string? GetOptionalAttribute(string name) {
            this.Attributes.TryGetValue(name, out string? value);
            return value;
        }

        public OperativeTagType? OperativeType(string name) {
            if(operativeBindings.TryGetValue(name, out OperativeTagType type))
                return type;
            return null;
        }
    }

    public class TagFormatter {

        static ConsoleColor AttributeColor = ConsoleColor.Green;
        static ConsoleColor ValueColor = ConsoleColor.Yellow;
        static ConsoleColor[] colors = new ConsoleColor[] {
            ConsoleColor.Red, ConsoleColor.Cyan,
            ConsoleColor.Magenta
        };

        public static void DisplayTag(Tag tag, int indentLevel = 0) {
            // Indentation level
            string indent = new string(' ', indentLevel * 4);

            // Print the current tag name with all the attributes
            ConsoleColor col = colors[indentLevel % colors.Length];

            Console.ForegroundColor = col;

            Console.Write($"{indent}<{tag.TagName}");
            // Display the attributes
            foreach(KeyValuePair<string, string> entry in tag.Attributes) {
                Console.ForegroundColor = AttributeColor;
                Console.Write($" {entry.Key}");
                if(entry.Value != string.Empty) {
                    Console.ForegroundColor = col;
                    Console.Write("=");
                    Console.ForegroundColor = ValueColor;
                    Console.Write($"'{entry.Value}'");
                }
            }
            Console.ForegroundColor = col;
            Console.Write(">");
            if(tag.Body.Count != 0) Console.WriteLine();
            // Recursively display the body
            foreach(Tag childTag in tag.Body) {
                DisplayTag(childTag, indentLevel + 1);
            }
            // Print closing tag
            Console.ForegroundColor = col;
            if(tag.Body.Count != 0) Console.Write($"{indent}");
            Console.WriteLine($"</{tag.TagName}>");

            Console.ResetColor();
        }
    }
}