using System;
using System.Drawing;

namespace TagScript.models {
    public class Tag {
        public string TagName { get; }
        public Dictionary<string, string> Attributes { get; }
        public List<Tag> Body { get; }

        public Tag(string tagName, Dictionary<string, string> attributes, List<Tag> body) {
            this.TagName = tagName;
            this.Attributes = attributes;
            this.Body = body;
        }

        public Tag(string tagName, List<Tag> body) : this(tagName, [], body) {}

        public Tag(string tagName, Dictionary<string, string> attributes) : this(tagName, attributes, []) {}

        public Tag(string tagName) : this(tagName, [], []) {}
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