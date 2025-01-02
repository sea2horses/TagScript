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
        BREAK
    }


    public class Tag {

        static Dictionary<string, TagType> tagBindings = new(){
            {"lit-text", TagType.LITERAL_TEXT},
            {"output", TagType.OUTPUT},
            {"br", TagType.BREAK},
            {"variable", TagType.VARIABLE}
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
        public bool AttributeExists(string name, out string value) {
            string? return_value;
            bool exists = this.Attributes.TryGetValue(name, out return_value);
                value = (return_value is null) ? "" : return_value;
            return exists;

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