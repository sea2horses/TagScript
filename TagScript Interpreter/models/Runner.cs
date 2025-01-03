using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace TagScript.models {

    class Variable {
        private DataTypes.DTGeneric _valueHolder;
        public object Value { get => _valueHolder.Get(); }
        public bool Set { get; private set; } = false;
        public DataType Type { get => _valueHolder.Type(); }
        public string Name { get; private set; }

        public Variable(string name, DataType type) {
            Name = name;
            SetValueHolder( (string?) null, type );
        }

        [MemberNotNull(nameof(_valueHolder))]
        void SetValueHolder(string? value, DataType type) {
            // Switch of the type
            switch(type) {
                // In case it's a string
                case(DataType.STRING): {
                    _valueHolder = DataTypes.DTString.Parse(value ?? "");
                    break;
                }
                // In case it's a number
                case(DataType.NUMBER): {
                    _valueHolder = DataTypes.DTNumber.Parse(value ?? default(double).ToString() );
                    break;
                }
                // In case it's a boolean
                case(DataType.BOOLEAN): {
                    _valueHolder = DataTypes.DTBoolean.Parse(value ?? default(bool).ToString() );
                    break;
                }
                // In case it's another value
                default: {
                    throw new Exception($"Type {Type} not supported in variable value assignment");
                }
            }
        }

        [MemberNotNull(nameof(_valueHolder))]
        void SetValueHolder(DataTypes.DTGeneric value, DataType type) {
            // Assert that the value is the same type
            DataTypes.DTGeneric newVal = value.Assert(type);
            // Override the value holder
            _valueHolder = newVal.Clone();
        }


        public DataTypes.DTGeneric GetAsDataType() {
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

        public void SetValue(DataTypes.DTGeneric value) {
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

        public TagScriptInterpreter(Tag masterTag, List<Variable> initialVariables)
            : this(masterTag) {
                this.variables = initialVariables;
            }

        public void Run() {
            // Create a tagRunner
            TagRunner tagRunner = new();
            // Run each tag in the mastertag body
            for(int i = 0; i < MasterTag.Body.Count; i++) {
                Tag tag = MasterTag.Body[i];
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
                    // If it's an if, get all the adjacent blocks and run them
                    case(TagType.IF): {
                        List<Tag> ifBlock = [];

                        ifBlock.Add(tag); // Add the if tag

                        i++;
                        while(i < MasterTag.Body.Count) {
                            tag = MasterTag.Body[i];
                            if(tag.Type == TagType.ELSEIF || tag.Type == TagType.ELSE) {
                                ifBlock.Add(tag);
                                i++;
                                if(tag.Type == TagType.ELSE) break;
                            } else break;
                        }
                        i--;

                        tagRunner.RunIfBlock(ifBlock, variables);
                        break;
                    }
                    // If it's a while tag, run it
                    case(TagType.WHILE): {
                        tagRunner.RunWhileBlock(tag, variables);
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
                        // If it's a number literal, print it to the console
                        case(TagType.LITERAL_NUMBER): {
                            // Get the value
                            double value = RunLiteralNumberTag(tag);
                            // Print it
                            Console.Write(value:0.00);
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
                            DataTypes.DTGeneric getData = RunGetTag(tag, scope);
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
                // VALUE ATTRIBUTE HAS OFFICIALLY BEEN DEPRECATED

                // Is it a valid type?
                if(!DataTypes.dataTypeBindings.TryGetValue(type, out DataType parsedDataType))
                    throw new Exception($"{type} is not a valid datatype"); 
                // Is the name occupied?
                if(scope.Any(v => v.Name == name))
                    throw new Exception($"A variable with name '{name}' already exists in the current scope");
                // Create the new variable 
                Variable newVar = new(name, parsedDataType);
                // If there's a value, assign it
                // (The .setValue function takes care of everything)
                if(varTag.Body.Count != 0) {
                    newVar.SetValue( RunAutoevaluativeTag(varTag, scope) );
                }

                // Add the variable to the scope
                scope.Add(newVar);
            }

            public DataTypes.DTGeneric RunGetTag(Tag getTag, List<Variable> scope) {
                // Get the necessary attribute
                string lookupName = getTag.GetAttribute("name");
                // Look up the variable
                Variable? returnVariable = LookUp(lookupName, scope);
                // If we didn't find it, go fuck yourself
                if(returnVariable is null)
                    throw new Exception($"No variable named {lookupName} in the current scope");
                // Else, let's get it
                DataTypes.DTGeneric resultingExpression = returnVariable.GetAsDataType();
                // Return the thing
                return resultingExpression;
            }

            public string RunLiteralTextTag(Tag litTextTag) {
                // Get the necessary attribute
                string result = litTextTag.GetAttribute("body");
                // Return the result
                return result;
            }

            public double RunLiteralNumberTag(Tag litNumberTag) {
                // Get the necessary attribute
                string value = litNumberTag.GetAttribute("value");
                // Parse it
                if(!double.TryParse(value, out double result))
                    throw new Exception("Number literal tag content couldn't be converted");
                // Return the result
                return result;
            }

            public DataTypes.DTGeneric RunInputTag(Tag inputTag, List<Variable> scope) {
                // Get the necessary attribute
                string? saveTo = inputTag.GetOptionalAttribute("catch");
                string? targetType = inputTag.GetOptionalAttribute("target-type");
                // Get optional attributes
                string? prompt = inputTag.GetOptionalAttribute("prompt");
                bool autobreak = !inputTag.AttributeExists("no-autobreak");


                // Now let's do some shit
                Console.Write(prompt ?? "");
                if(autobreak && prompt is not null) Console.WriteLine();
                // Get the user input
                string userInput = Console.ReadLine() ?? "";

                if(saveTo is not null) {
                    // Let's get the variable
                    Variable? variable = LookUp(saveTo, scope);
                    // Throw exception if we don't find it
                    if(variable is null)
                        throw new Exception($"No variable named {variable} in the current scope");
                    // Save this into the variable
                    variable.SetValue(userInput);
                    // Return it too
                    return variable.GetAsDataType();
                } else if(targetType is not null) {
                    // Let's get the target type
                    if(!DataTypes.dataTypeBindings.TryGetValue(targetType, out DataType parsedDataType))
                        throw new Exception($"{targetType} is not a valid datatype");
                    // Generic Parse
                    DataTypes.DTGeneric newExpr = DataTypes.DTGeneric.GenericParse(userInput, parsedDataType);
                    // Return it
                    return newExpr;
                } else throw new Exception("Input tag requires either a 'catch' or a 'target-type' attribute");
            }

            public DataTypes.DTGeneric[] CatchOperands(Tag operativeTag, List<Variable> scope) {
                // Else, let's go
                DataTypes.DTGeneric[] operands = new DataTypes.DTGeneric[operativeTag.Body.Count];
                // Check each one
                int i = 0;
                // Cycle through each tag
                foreach(Tag tag in operativeTag.Body) {
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
                        // If it's a literal number tag
                        case(TagType.LITERAL_NUMBER): {
                            operands[i] = new DataTypes.DTNumber(RunLiteralNumberTag(tag));
                            break;
                        }
                        // If it's an input tag
                        case(TagType.INPUT): {
                            operands[i] = RunInputTag(tag, scope);
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
                // Return
                return operands;
            }

            public DataTypes.DTGeneric RunAddTag(Tag addTag, List<Variable> scope) {
                // Get the amount of tag in its body
                int operandAmount = addTag.Body.Count;
                // There must be only two operands 
                if(operandAmount != 2)
                    throw new Exception("An add tag must have exactly 2 operands");
                // Else, let's go
                DataTypes.DTGeneric[] operands = CatchOperands(addTag, scope);
                // Now let's add!
                DataTypes.DTGeneric result = DataTypes.TypeOperations.Add(operands[0], operands[1]);
                // Return the result
                return result;
            }

            public DataTypes.DTGeneric RunCompareTag(Tag equalsTag, List<Variable> scope) {
                // Get the amount of tag in its body
                int operandAmount = equalsTag.Body.Count;
                // There must be only two operands 
                if(operandAmount != 2)
                    throw new Exception("A compare tag must have exactly 2 operands");
                // Else, let's go
                DataTypes.DTGeneric[] operands = CatchOperands(equalsTag, scope);
                // Now let's add!
                DataTypes.DTGeneric result = DataTypes.TypeOperations.Equals(operands[0], operands[1]);
                // Return the result
                return result;
            }

            public DataTypes.DTGeneric RunNegateTag(Tag negateTag, List<Variable> scope) {
                // Get the amount of tags in its body
                int operandAmount = negateTag.Body.Count;
                // There must be only one operand
                if(operandAmount != 1)
                    throw new Exception("A negate tag must have exactly 1 operand");
                // Else, let's go
                DataTypes.DTGeneric[] operands = CatchOperands(negateTag, scope);
                // Now let's negate
                DataTypes.DTGeneric result = DataTypes.TypeOperations.Negate(operands[0]);
                // Return the result
                return result;
            }

            public DataTypes.DTGeneric RunOperativeTag(Tag operativeTag, List<Variable> scope) {

                if(operativeTag.Type != TagType.OPERATIVE)
                    throw new Exception("Evaluation tag only supports operative tags");
                // Get the operative type
                OperativeTagType? operativeType = operativeTag.OperativeType(operativeTag.TagName);
                // If it's not an operative type, throw an exception
                if(operativeType is null)
                    throw new Exception($"Tag {operativeTag} is not bound to an operative type, therefore it is not supported for operating");
                // Result variable
                DataTypes.DTGeneric result;
                // Else, let's do a switch
                switch(operativeType) {
                    // In case it's a sum tag
                    case (OperativeTagType.SUM): {
                        result = RunAddTag(operativeTag, scope);
                        break;
                    }
                    // In case it's an equals
                    case (OperativeTagType.EQUALS): {
                        result = RunCompareTag(operativeTag, scope);
                        break;
                    }
                    // If it's a negates
                    case (OperativeTagType.NEGATE): {
                        result = RunNegateTag(operativeTag, scope);
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

            public DataTypes.DTGeneric RunEvalTag(Tag evalTag, List<Variable> scope) {
                // Get the save location
                string? saveTo = evalTag.GetOptionalAttribute("catch");
                // Check the body has precisely 1 tag
                if(evalTag.Body.Count != 1)
                    throw new Exception($"Eval tags must have precisely 1 tag inside them");
                // Compute the result
                DataTypes.DTGeneric result = RunOperativeTag(evalTag.Body[0], scope);
                // If there's a variable ready for catching
                if(saveTo is not null) {
                    // Now let's look for the variable
                    Variable? variable = LookUp(saveTo, scope);
                    // If it doesn't exist, except
                    if(variable is null)
                        throw new Exception($"No variable named {variable} in the current scope");
                    // Return it
                    variable.SetValue(result);
                }   
                // Return it to whoever wants it
                return result;
            }

            public DataTypes.DTGeneric RunAutoevaluativeTag(Tag autoEvalTag, List<Variable> scope) {
                // Declare the expression result
                DataTypes.DTGeneric resultingExpression;
                // An autoevaluative tag must have only ONE master tag
                if(autoEvalTag.Body.Count != 1)
                    throw new Exception("An auto-evaluative tag must only have 1 tag inside it");
                // Depending on the tag found
                Tag masterTag = autoEvalTag.Body[0];
                // Let's do a switch on the type
                switch(masterTag.Type) {
                    // In case it's a number literal
                    case(TagType.LITERAL_NUMBER): {
                        resultingExpression = new DataTypes.DTNumber(RunLiteralNumberTag(masterTag));
                        break;
                    }
                    // In case it's a string literal
                    case(TagType.LITERAL_TEXT): {
                        resultingExpression = new DataTypes.DTString(RunLiteralTextTag(masterTag));
                        break;
                    }
                    // In case it's a get, run it
                    case(TagType.GET): {
                        resultingExpression = RunGetTag(masterTag, scope);
                        break;
                    }
                    // In case it's an operative tag, also run it
                    case(TagType.OPERATIVE): {
                        resultingExpression = RunOperativeTag(masterTag, scope);
                        break;
                    }
                    // If it's anything else, BOOM
                    default: {
                        throw new Exception($"Tag '{masterTag.TagName}' of type {masterTag.Type} is not supported in an auto-evaluative tag");
                    }
                }
                // Return the result
                return resultingExpression;
            }

            public bool RunConditionalTag(Tag conditionTag, List<Variable> scope) {
                // Store the boolean
                DataTypes.DTBoolean condition_result;
                // Normally you would get the condition tag body count (must be 1)
                // But auto-evaluative tags already do that check, so let's just get the resulting expression
                DataTypes.DTGeneric result = RunAutoevaluativeTag(conditionTag, scope);
                // Check if it's a boolean
                if(result.Type() != DataType.BOOLEAN)
                    throw new Exception("The body of a conditional tag must resolve to a boolean");
                // Now parse it
                condition_result = (DataTypes.DTBoolean) result;
                // Return the value
                return condition_result.Value;
            }

            public void RunIfBlock(List<Tag> ifBlock, List<Variable> scope) {
                // Get conditions and body tags
                Tag?[] conditions = new Tag[ifBlock.Count];
                List<Tag>[] bodies = new List<Tag>[ifBlock.Count];
                // Body tag is now deprecated

                // Foreach
                for(int i = 0; i < ifBlock.Count; i++) {
                    Tag block = ifBlock[i];
                    bodies[i] = [];
                    Tag? conditionTag = null;
                    // Let's look for the condition tag, everything else will be added to the body
                    foreach(Tag tag in block.Body) {
                        // If we found the condition tag
                        if(tag.Type == TagType.CONDITION) {
                            // If we already had a condition tag, error it
                            if(conditionTag is not null)
                                throw new Exception("There can't be more than one condition in an if/elif tag");
                            conditionTag = tag;
                        } else bodies[i].Add(tag);
                    }
                    // If there's not a condition tag and it's not an else block, kill yourself
                    if(block.Type != TagType.ELSE && conditionTag is null)
                        throw new Exception("Every if/elif tag must have a 'condition' tag inside it");
                    // If it's an else and there's a condition, also error it
                    if(block.Type == TagType.ELSE && conditionTag is not null)
                        throw new Exception("An else tag can't have a 'condition' tag inside it");
                    // Push it to the array
                    conditions[i] = conditionTag;
                }

                // Now run through each one
                for(int i = 0; i < ifBlock.Count; i++) {
                    Tag? conditionTag = conditions[i];
                    List<Tag> body = bodies[i];
                    // If any of them return true, the cycle is broken and any left stop executing
                    if(ifBlock[i].Type != TagType.ELSE) {
                        if(conditionTag is null) {
                            throw new Exception("if and elseif blocks must have a 'condition' tag");
                        }
                        if(RunConditionalTag(conditionTag, scope)) {
                            // New body tag
                            Tag bodyTag = new Tag("body", body);
                            // Create a new interpreter instance
                            TagScriptInterpreter interpreter = new(bodyTag, scope);
                            // Run it
                            interpreter.Run();
                            break;
                        }
                    } else {
                        // We've reached the 'else'
                        // Create a master tag for the interpreter
                        Tag bodyTag = new Tag("body", body);
                        // Create a new interpreter instance
                        TagScriptInterpreter interpreter = new(bodyTag, scope);
                        // Run it
                        interpreter.Run();
                    }
                }
            }

            public void RunWhileBlock(Tag whileBlock, List<Variable> scope) {
                Tag? conditionTag = null;
                List<Tag> body = [];
                // Let's go get the condition tag, the rest is considered body
                foreach(Tag tag in whileBlock.Body) {
                    // If we found the condition tag
                    if(tag.Type == TagType.CONDITION) {
                        // If we already had a condition tag, error it
                        if(conditionTag is not null)
                            throw new Exception("There can't be more than one condition in a while tag");
                        conditionTag = tag;
                    } else body.Add(tag);
                }
                // If there's no conditional tag, error
                if(conditionTag is null)
                    throw new Exception("while blocks must have a 'condition' tag");
                // Create the masterTag
                Tag bodyTag = new Tag("body", body);
                // Now, while the conditional tag is true
                while(RunConditionalTag(conditionTag, scope)) {
                    // Create the interpreter instance
                    TagScriptInterpreter interpreter = new(bodyTag, scope);
                    // Run it
                    interpreter.Run();
                }
            }
        }
    }
}