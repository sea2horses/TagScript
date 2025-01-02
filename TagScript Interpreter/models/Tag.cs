using System;
using System.Drawing;

namespace TagScript.models {

    public enum TagType {
        UNIVERSAL,
        INPUT,
        OUTPUT,
        LITERAL_TEXT,
        VARIABLE,
        CONSTANT,
        GET,
        EVALUATE,
        BREAK,
        OPERATIVE
    }

    public enum OperativeTagType {
        SUM,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        RAISE,
        ROOT,
        MODULO
    }

    public class Tag {

        static Dictionary<string, TagType> tagBindings = new(){
            {"lit-text", TagType.LITERAL_TEXT},
            {"output", TagType.OUTPUT},
            {"br", TagType.BREAK},
            {"variable", TagType.VARIABLE},
            {"get", TagType.GET},
            {"input", TagType.INPUT},
            {"eval", TagType.EVALUATE},
            {"add", TagType.OPERATIVE}
        };

        static Dictionary<string, OperativeTagType> operativeBindings = new(){
            {"add", OperativeTagType.SUM},
            {"subtract", OperativeTagType.SUBTRACT},
            {"multiply", OperativeTagType.MULTIPLY},
            {"divide", OperativeTagType.DIVIDE},
            {"raise", OperativeTagType.RAISE},
            {"modulo", OperativeTagType.MODULO}
        };

        public string TagName { get; }
        public TagType Type { get; private set; } = TagType.UNIVERSAL;
        public Dictionary<string, string> Attributes { get; }
        public List<Tag> Body { get; }

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
                throw new Exception("Tag '{Name}' is missing attribute '{name}'");
            
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