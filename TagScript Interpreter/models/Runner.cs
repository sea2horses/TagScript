using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
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

        [MemberNotNull(nameof(_valueHolder))]
        void SetValueHolder(DataTypes.BaseDataType value, DataType type) {
            // Assert that the value is the same type
            DataTypes.BaseDataType newVal = value.Assert(type);
            // Override the value holder
            _valueHolder = newVal.Clone();
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

        public void SetValue(DataTypes.BaseDataType value) {
            // Set the value with fixed type
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
                    // If it's an input, run it
                    case(TagType.INPUT): {
                        tagRunner.RunInputTag(tag, variables);
                        break;
                    }
                    // If it's an eval, run it
                    case(TagType.EVALUATE): {
                        tagRunner.RunEvalTag(tag, variables);
                        break;
                    }
                    // If it's anything else, except
                    default: {
                        throw new Exception($"Tag {tag.TagName} does not qualify as a stand-alone tag");
                    }
                }
            }
        }

        class TagRunner() {
            public Variable? LookUp(string name, List<Variable> scope) {
                // Look for the variable in the scope
                Variable? returnVariable = null;
                // ForEach
                foreach(Variable variable in scope) {
                    if(variable.Name == name) {
                        returnVariable = variable;
                        break;
                    }
                }
                // Return
                return returnVariable;
            }

            public void RunOutputTag(Tag outputTag, List<Variable> scope) {
                // If the no-autobreak attribute doesn't exist, set autobreak to true
                bool autobreak = !outputTag.AttributeExists("no-autobreak");
                // Go foreach tag in the body
                foreach(Tag tag in outputTag.Body) {
                    switch(tag.Type) {
                        // If it's literal text, print it to the console
                        case(TagType.LITERAL_TEXT): {
                            // If the <lit-text/> tag doesn't have a body, throw an exception
                            string body = RunLiteralTextTag(tag);
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
                // Look up the variable
                Variable? returnVariable = LookUp(lookupName, scope);
                // If we didn't find it, go fuck yourself
                if(returnVariable is null)
                    throw new Exception($"No variable named {lookupName} in the current scope");
                // Else, let's get it
                DataTypes.BaseDataType resultingExpression = returnVariable.GetAsExpression();
                // Return the thing
                return resultingExpression;
            }

            public string RunLiteralTextTag(Tag litTextTag) {
                // Get the necessary attribute
                string result = litTextTag.GetAttribute("body");
                // Return the result
                return result;
            }

            public void RunInputTag(Tag inputTag, List<Variable> scope) {
                // Get the necessary attribute
                string saveTo = inputTag.GetAttribute("save-to");
                // Get optional attributes
                string? prompt = inputTag.GetOptionalAttribute("prompt");
                bool autobreak = !inputTag.AttributeExists("no-autobreak");
                // Let's get the variable
                Variable? variable = LookUp(saveTo, scope);
                // Throw exception if we don't find it
                if(variable is null)
                    throw new Exception($"No variable named {variable} in the current scope");
                // Now let's do some shit
                Console.Write(prompt ?? "");
                if(autobreak && prompt is not null) Console.WriteLine();
                // Get the user input
                string userInput = Console.ReadLine() ?? "";
                // Save this into the variable
                variable.SetValue(userInput);
            }

            public DataTypes.BaseDataType RunAddTag(Tag addTag, List<Variable> scope) {
                // Get the amount of tag in its body
                int operandAmount = addTag.Body.Count;
                // There must be only two operands 
                if(operandAmount != 2)
                    throw new Exception("An add tag must have exactly 2 operands");
                // Else, let's go
                DataTypes.BaseDataType[] operands = new DataTypes.BaseDataType[2];
                // Check each one
                int i = 0;
                // Cycle through each tag
                foreach(Tag tag in addTag.Body) {
                    // Depending on the type of tag
                    switch(tag.Type) {
                        // In case it's a get tag
                        case(TagType.GET): {
                            operands[i] = RunGetTag(tag, scope);
                            break;
                        }
                        // If it's a literal text tag
                        case(TagType.LITERAL_TEXT): {
                            operands[i] = new DataTypes.DTString(RunLiteralTextTag(tag));
                            break;
                        }
                        // If it's another add tag
                        case(TagType.OPERATIVE): {
                            operands[i] = RunOperativeTag(tag, scope);
                            break;
                        }
                        // Else, throw an exception
                        default: {
                            throw new Exception($"Tag {tag.TagName} not supported as a sum operand");
                        }
                    }
                    i++;
                }
                // Now let's add!
                DataTypes.BaseDataType result = DataTypes.Add(operands[0], operands[1]);
                // Return the result
                return result;
            }

            public DataTypes.BaseDataType RunOperativeTag(Tag operativeTag, List<Variable> scope) {

                if(operativeTag.Type != TagType.OPERATIVE)
                    throw new Exception("Evaluation tag only supports operative tags");
                // Get the operative type
                OperativeTagType? operativeType = operativeTag.OperativeType(operativeTag.TagName);
                // If it's not an operative type, throw an exception
                if(operativeType is null)
                    throw new Exception($"Tag {operativeTag} is not bound to an operative type, therefore it is not supported for operating");
                // Result variable
                DataTypes.BaseDataType result;
                // Else, let's do a switch
                switch(operativeType) {
                    // In case it's a sum tag
                    case (OperativeTagType.SUM): {
                        result = RunAddTag(operativeTag, scope);
                        break;
                    }
                    // Else, except
                    default: {
                        throw new Exception($"Tag {operativeTag} is not yet supported by eval");
                    }
                }
                // Return the result
                return result;
            }

            public void RunEvalTag(Tag evalTag, List<Variable> scope) {
                // Get the save location
                string saveTo = evalTag.GetAttribute("save-to");
                // Now let's look for the variable
                Variable? variable = LookUp(saveTo, scope);
                // If it doesn't exist, except
                if(variable is null)
                    throw new Exception($"No variable named {variable} in the current scope");
                // Check the body has precisely 1 tag
                if(evalTag.Body.Count != 1)
                    throw new Exception($"Eval tags must have precisely 1 tag inside them");
                // Compute the result
                DataTypes.BaseDataType result = RunOperativeTag(evalTag.Body[0], scope);
                // Return it
                variable.SetValue(result);
            }
        }
    }
}