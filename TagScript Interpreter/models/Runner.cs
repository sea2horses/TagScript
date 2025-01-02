using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace TagScript.models {

    class Variable {
        private DataTypes.BaseDataType _valueHolder;
        public object Value { get => _valueHolder.Get(); }
        public bool Set { get; private set; }
        public DataType Type { get => _valueHolder.Type(); }
        public string Name { get; private set; }

        public Variable(string name, DataType type, string? value) {
            Name = name;
            SetValueHolder(value, type);
            Set = true;
        }

        public Variable(string name, DataType type)
            : this(name, type, null) {
                Set = false;
        }

        [MemberNotNull(nameof(_valueHolder))]
        void SetValueHolder(string? value, DataType type) {
            // Switch of the type
            switch(type) {
                // In case it's a string
                case(DataType.STRING): {
                    _valueHolder = new DataTypes.DTString(value ?? "");
                    break;
                }
                // In case it's a number
                case(DataType.NUMBER): {
                    if(!double.TryParse(value ?? default(double).ToString(), out double parsedValue))
                        throw new Exception($"variable {Name} cannot parse '{value}' to type {Type}");
                    _valueHolder = new DataTypes.DTNumber(parsedValue);
                    break;
                }
                // In case it's a boolean
                case(DataType.BOOLEAN): {
                    if(!bool.TryParse(value ?? default(bool).ToString(), out bool parsedValue))
                        throw new Exception($"variable {Name} cannot parse '{value}' to type {Type}");
                    _valueHolder = new DataTypes.DTBoolean(parsedValue);
                    break;
                }
                // In case it's another value
                default: {
                    throw new Exception($"Type {Type} not supported in variable value assignment");
                }
            }
        }

        public DataTypes.BaseDataType GetAsExpression() {
            if(!Set)
                throw new Exception($"Trying to access variable {Name} while it is unset");
            return _valueHolder.Clone();
        }

        public void SetValue(string value) {
            // Set value with fixed type
            SetValueHolder(value, Type);
            // Set is now true
            Set = true;
        }

        public override string ToString()
        {
            return Set ? $"{Type} {Name} = {Value}" : $"{Type} {Name} ?";
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
                        tagRunner.RunOutputTag(tag, variables);
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
            public void RunOutputTag(Tag outputTag, List<Variable> scope) {
                // If the no-autobreak attribute doesn't exist, set autobreak to true
                bool autobreak = !outputTag.AttributeExists("no-autobreak");
                // Go foreach tag in the body
                foreach(Tag tag in outputTag.Body) {
                    switch(tag.Type) {
                        // If it's literal text, print it to the console
                        case(TagType.LITERAL_TEXT): {
                            // If the <lit-text/> tag doesn't have a body, throw an exception
                            string body = tag.GetAttribute("body");
                            // Print the body
                            Console.Write(body);
                            // If autobreak is on, add a line break
                            if(autobreak) Console.WriteLine();

                            break;
                        }
                        // If it's a break tag, add a line break
                        case(TagType.BREAK): {
                            int amount = 1;
                            // Try to get the amount attribute
                            string? amountString = tag.GetOptionalAttribute("amount");
                            if(amountString is not null) {
                                if(!int.TryParse(amountString, out amount) || amount <= 0)
                                    throw new Exception("br's 'amount' attribute must be a valid integer");
                            }
                            // Print the requested amount
                            for(int i = 0; i < amount; i++) Console.Write('\n');

                            break;
                        }
                        // If it's a get tag, let's get the var
                        case(TagType.GET): {
                            DataTypes.BaseDataType getData = RunGetTag(tag, scope);
                            // Print it with no arguments
                            Console.WriteLine(getData.Format([]));
                            // If autobreak is on add a line brea
                            if(autobreak) Console.WriteLine();
                            
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
                string name = varTag.GetAttribute("name"), type = varTag.GetAttribute("type");
                // Non-required attributes
                string? value = varTag.GetOptionalAttribute("value");

                // Is it a valid type?
                if(!DataTypes.dataTypeBindings.TryGetValue(type, out DataType parsedDataType))
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

            public DataTypes.BaseDataType RunGetTag(Tag getTag, List<Variable> scope) {
                // Get the necessary attribute
                string lookupName = getTag.GetAttribute("name");
                // Look for the variable in the scope
                Variable? returnVariable = null;
                // ForEach
                foreach(Variable variable in scope) {
                    if(variable.Name == lookupName) {
                        returnVariable = variable;
                        break;
                    }
                }
                // If we didn't find it, go fuck yourself
                if(returnVariable is null)
                    throw new Exception($"No variable named {lookupName} in the current scope");
                // Else, let's get it
                DataTypes.BaseDataType resultingExpression = returnVariable.GetAsExpression();
                // Return the thing
                return resultingExpression;
            }
        }
    }
}