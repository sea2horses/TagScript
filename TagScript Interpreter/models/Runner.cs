using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace TagScript.models {

    public enum DataType {
        NUMBER,
        STRING,
        BOOLEAN
    }

    class Variable {
        public object? Value { get; private set; } = null;
        public DataType Type { get; private set; }
        public string Name { get; private set; }

        public Variable(string name, DataType type, object? value = null) {
            Name = name;
            Type = type;
            Value = value;
        }

        public void SetValue(string value) {
            // Switch of the type
            switch(Type) {
                // In case it's a string
                case(DataType.STRING): {
                    Value = value;
                    break;
                }
                // In case it's a number
                case(DataType.NUMBER): {
                    if(!double.TryParse(value, out double parsedValue))
                        throw new Exception($"variable {Name} cannot parse '{value}' to type {Type}");
                    Value = parsedValue;
                    break;
                }
                // In case it's a boolean
                case(DataType.BOOLEAN): {
                    if(!bool.TryParse(value, out bool parsedValue))
                        throw new Exception($"variable {Name} cannot parse '{value}' to type {Type}");
                    Value = parsedValue;
                    break;
                }
                // In case it's another value
                default: {
                    throw new Exception($"Type {Type} not supported in variable value assignment");
                }
            }
        }

        public override string ToString()
        {
            return (Value is null) ? $"{Type} {Name}" : $"{Type} {Name} = {Value}";
        }
    }

    class TagScriptInterpreter {
        Tag MasterTag;
        public List<Variable> variables { get; } = [];

        public TagScriptInterpreter(Tag masterTag) {
            this.MasterTag = masterTag;
        }

        public void Run() {
            // Create a tagRunner
            TagRunner tagRunner = new();
            // Run each tag in the mastertag body
            foreach(Tag tag in MasterTag.Body) {
                switch(tag.Type) {
                    // If it's an output tag, run it
                    case(TagType.OUTPUT): {
                        tagRunner.RunOutputTag(tag);
                        break;
                    }
                    // If it's a variable tag, run it
                    case(TagType.VARIABLE): {
                        tagRunner.RunVariableTag(tag, variables);
                        break;
                    }
                }
            }
        }

        class TagRunner() {
            public Dictionary<string, DataType> dataTypeBindings = new() {
                {"string", DataType.STRING},
                {"number", DataType.NUMBER},
                {"boolean", DataType.BOOLEAN}
            };
            public void RunOutputTag(Tag outputTag) {
                // If the no-autobreak attribute doesn't exist, set autobreak to true
                bool autobreak = !outputTag.AttributeExists("no-autobreak");
                // Go foreach tag in the body
                foreach(Tag tag in outputTag.Body) {
                    switch(tag.Type) {
                        // If it's literal text, print it to the console
                        case(TagType.LITERAL_TEXT): {
                            // If the <lit-text/> tag doesn't have a body, throw an exception
                            if(!tag.AttributeExists("body", out string body))
                                throw new Exception("lit-text has no 'body' attribute");
                            // Print the body
                            Console.Write(body);
                            // If autobreak is on, add a line break
                            Console.WriteLine();

                            break;
                        }
                        // If it's a break tag, add a line break
                        case(TagType.BREAK): {
                            int amount = 1;
                            // Try to get the amount attribute
                            if(tag.AttributeExists("amount", out string amountString)) {
                                if(!int.TryParse(amountString, out amount) || amount <= 0)
                                    throw new Exception("br's 'amount' attribute must be a valid integer");
                            }
                            // Print the requested amount
                            for(int i = 0; i < amount; i++) Console.Write('\n');

                            break;
                        }
                        // Else print a generic exception
                        default: {
                            throw new Exception($"Tag '{tag.TagName}' is either not supported by the output tag or does not exist");
                        }
                    }
                }
            }

            public void RunVariableTag(Tag varTag, List<Variable> scope) {
                // Required attributes
                string name, type;
                // Non-required attributes
                string? value;

                // Check if the name attribute exists
                if(!varTag.AttributeExists("name", out name))
                    throw new Exception("variable tag has no 'name' attribute");
                
                // Check if the type attribute exists
                if(!varTag.AttributeExists("type", out type))
                    throw new Exception("variable tag has no 'type' attribute");
                
                // Check if the value attribute exists
                if(!varTag.AttributeExists("value", out value))
                    value = null;

                // Is it a valid type?
                if(!dataTypeBindings.TryGetValue(type, out DataType parsedDataType))
                    throw new Exception($"{type} is not a valid datatype");
                
                // Is the name not occupied?
                if(scope.Any(v => v.Name == name))
                    throw new Exception($"A variable with name '{name}' already exists in the current scope");

                // Create the new variable 
                Variable newVar = new(name, parsedDataType);
                // If there's a value, assign it
                // (The .setValue function takes care of everything)
                if(value is not null) newVar.SetValue(value);

                // Add the variable to the scope
                scope.Add(newVar);
            }
        }
    }
}