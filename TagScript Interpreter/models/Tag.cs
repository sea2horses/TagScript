using System;

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
        string[] colors = new string[] {
            "#FF5733", "#33FF57", "#3357FF", "#FF33A1", "#A133FF", "#33FFF5", // Reds, Greens, Blues
            "#FFD433", "#FF8333", "#8AFF33", "#338AFF", "#D633FF", "#33FFD1", // Yellows, Oranges, Aquas
            "#FF3333", "#33FF8A", "#338AFF", "#FF5733", "#5733FF", "#A1FF33", // Pinks, Purples, Cyans
            "#FF333A", "#33FFA1", "#33A1FF", "#FFA133", "#D1FF33", "#33FFD4"  // Soft Muted Tones
        };

        public static void DisplayTag(Tag tag, int indentLevel = 0) {
            // Indentation level
            string indent = new string('\t', indentLevel);

            // Print the current tag name with all the attributes
            Console.Write($"{indent}<{tag.TagName}");
            // Display the attributes
            foreach(KeyValuePair<string, string> entry in tag.Attributes) {
                Console.Write($" {entry.Key}=\"{entry.Value}\"");
            }
            Console.WriteLine(">");
            // Recursively display the body
            foreach(Tag childTag in tag.Body) {
                DisplayTag(childTag, indentLevel + 1);
            }
            // Print closing tag
            Console.WriteLine($"{indent}</{tag.TagName}>");
        }
    }
}