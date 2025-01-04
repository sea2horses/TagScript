using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Formats.Asn1;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using TagScript.main;

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
                try {
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
                        // Officially <set></set> will replace <eval></eval>
                        case(TagType.SET): {
                            tagRunner.RunSetTag(tag, variables);
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
                        // If it's a call tag, run it
                        case(TagType.CALL): {
                            tagRunner.RunFunctionCall(tag, variables);
                            break;
                        }
                        // If it's a try block, get the try and the catch
                        case(TagType.TRY): {
                            if(i >= MasterTag.Body.Count)
                                throw new Exception($"Try tag must have a 'catch' tag");
                            Tag catchTag = MasterTag.Body[i + 1];
                            if(catchTag.Type != TagType.CATCH)
                                throw new Exception($"Try tag must have a 'catch' tag");
                            tagRunner.RunTryCatch(tag, catchTag, variables);
                            i++;
                            break;
                        }
                        // If it's anything else, except
                        default: {
                            throw new Exception($"Tag {tag.TagName} does not qualify as a stand-alone tag");
                        }
                    }
                } catch(Exception ex) {
                    TagxExceptions.RaiseException(ex.Message, TagxExceptions.ExceptionType.FATAL,
                        tag.Position, tag.TagName.Length);
                    return;
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

                    try {
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
                            // If it's an operative tag, run it and format the result
                            case(TagType.OPERATIVE): {
                                DataTypes.DTGeneric getData = RunOperativeTag(tag, scope);
                                // Print it
                                Console.WriteLine(getData.Format([]));
                                // If autobreak
                                if(autobreak) Console.WriteLine();

                                break;
                            }
                            // If it's a function call, run it and format the result
                            case(TagType.CALL): {
                                DataTypes.DTGeneric? getData = RunFunctionCall(tag, scope);
                                // If the function didn't return anything
                                if(getData is null)
                                    throw new Exception($"Function with no return value cannot be printed");
                                // Print it
                                Console.WriteLine(getData.Format([]));
                                // If autobreak
                                if(autobreak) Console.WriteLine();

                                break;
                            }
                            // Else print a generic exception
                            default: {
                                throw new Exception($"Tag '{tag.TagName}' is either not supported by the output tag or does not exist");
                            }
                        }
                    } catch(Exception ex) {
                        TagxExceptions.RaiseException(ex.Message, TagxExceptions.ExceptionType.FATAL,
                            tag.Position, tag.TagName.Length);
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

            public void RunSetTag(Tag setTag, List<Variable> scope) {
                // Get the necessary attribute
                string lookupName = setTag.GetAttribute("name");
                // Look up the variable
                Variable? returnVariable = LookUp(lookupName, scope);
                // If we didn't find it, go fuck yourself
                if(returnVariable is null)
                    throw new Exception($"No variable named {lookupName} in the current scope");
                // Else, let's get it
                DataTypes.DTGeneric resultingExpression = RunAutoevaluativeTag(setTag, scope);
                // Set it to the variable
                returnVariable.SetValue(resultingExpression);
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
                string? saveTo = inputTag.GetOptionalAttribute("save-to");
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
                    try {
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
                    } catch(Exception ex) {
                        TagxExceptions.RaiseException(ex.Message, TagxExceptions.ExceptionType.FATAL,
                            tag.Position, tag.TagName.Length);
                    }
                }
                // Return
                return operands;
            }

            public DataTypes.DTGeneric RunBinaryTag(Tag binaryTag, List<Variable> scope,
                Func<DataTypes.DTGeneric, DataTypes.DTGeneric, DataTypes.DTGeneric> function) {
                // Get the amount of tags in its body
                int operandAmount = binaryTag.Body.Count;
                // There must be only one operand
                if(operandAmount != 2)
                    throw new Exception($"A {binaryTag.TagName} tag must have exactly 2 operands");
                // Else, let's go
                DataTypes.DTGeneric[] operands = CatchOperands(binaryTag, scope);
                // Now let's negate
                DataTypes.DTGeneric result = function(operands[0], operands[1]);
                // Return the result
                return result;
            }

            public DataTypes.DTGeneric RunUnaryTag(Tag unaryTag, List<Variable> scope,
                Func<DataTypes.DTGeneric, DataTypes.DTGeneric> function) {
                // Get the amount of tags in its body
                int operandAmount = unaryTag.Body.Count;
                // There must be only one operand
                if(operandAmount != 1)
                    throw new Exception($"A {unaryTag.TagName} tag must have exactly 1 operand");
                // Else, let's go
                DataTypes.DTGeneric[] operands = CatchOperands(unaryTag, scope);
                // Now let's negate
                DataTypes.DTGeneric result = function(operands[0]);
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
                // Else, let's do a switch
                switch(operativeType) {
                    // In case it's a sum tag
                    case (OperativeTagType.SUM): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Add);
                    case (OperativeTagType.SUBTRACT): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Subtract);
                    case (OperativeTagType.MULTIPLY): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Multiply);
                    case (OperativeTagType.DIVIDE): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Divide);
                    case (OperativeTagType.MODULO): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Modulo);
                    case (OperativeTagType.RAISE): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Power);
                    case (OperativeTagType.ROOT): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Root);
                    case (OperativeTagType.EQUALS): return RunBinaryTag(operativeTag, scope, DataTypes.TypeOperations.Equals);
                    case (OperativeTagType.NEGATE): return RunUnaryTag(operativeTag, scope, DataTypes.TypeOperations.Negate);
                    // Else, except
                    default: {
                        throw new Exception($"Tag {operativeTag} is not yet supported by eval");
                    }
                }
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
                DataTypes.DTGeneric? resultingExpression = null;
                // An autoevaluative tag must have only ONE master tag
                if(autoEvalTag.Body.Count != 1)
                    throw new Exception("An auto-evaluative tag must only have 1 tag inside it");
                // Depending on the tag found
                Tag masterTag = autoEvalTag.Body[0];
                try {
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
                        // In case it's a function call, run it
                        case(TagType.CALL): {
                            resultingExpression = RunFunctionCall(masterTag, scope) ??
                                throw new Exception($"Function with no return value cannot be used in an expression");
                            break;
                        }
                        // In case it's input, run it
                        case(TagType.INPUT): {
                            resultingExpression = RunInputTag(masterTag, scope);
                            break;
                        }
                        // If it's anything else, BOOM
                        default: {
                            throw new Exception($"Tag '{masterTag.TagName}' of type {masterTag.Type} is not supported in an auto-evaluative tag");
                        }
                    }
                } catch(Exception ex) { 
                    TagxExceptions.RaiseException(ex.Message, TagxExceptions.ExceptionType.FATAL,
                        masterTag.Position, masterTag.TagName.Length);
                }

                if(resultingExpression is null) 
                    throw new Exception($"Expression cannot be null");

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

            public DataTypes.DTGeneric? RunFunctionCall(Tag functionTag, List<Variable> scope) {
                // Declare the list of passed arguments
                PassedArgument[] passedArguments = new PassedArgument[functionTag.Body.Count];
                // Get the name of the function
                string functionName = functionTag.GetAttribute("name");

                // Get the argument tags
                for(int i = 0; i < functionTag.Body.Count; i++) {
                    Tag tag = functionTag.Body[i];
                    // If the tag isn't an argument tag, error
                    if(tag.Type != TagType.ARGUMENT)
                        throw new Exception($"Function call tag only supports argument tags");
                    // Get the tag 'name' argument
                    string? argumentName = tag.GetOptionalAttribute("name");
                    // Get the expression within
                    DataTypes.DTGeneric result = RunAutoevaluativeTag(tag, scope);
                    // Create the passed argument
                    passedArguments[i] = new PassedArgument(result, argumentName);
                }

                // Check if it's a builtin function
                BuiltInFunction? builtInFunction = BuiltInFunctions.GetFunction(functionName);
                // If it's a BuiltInFunction
                if(builtInFunction is not null) {
                    DataTypes.DTGeneric? result = builtInFunction.Call(passedArguments);
                    return result;
                }
                // Else, let's throw an error
                throw new Exception($"Function {functionName} does not exist");
            }

            public void RunTryCatch(Tag tryTag, Tag catchTag, List<Variable> scope) {
                // Check if the tag has a save-to attribute
                string? value = catchTag.GetOptionalAttribute("save-to");
                // In that case, look up the variable
                Variable? variable = null;
                // Look
                if(value is not null) {
                    variable = LookUp(value, scope) ??
                        throw new Exception($"Variable {value} does not exist in the current scope");
                }
                // Let's try to run the try body
                TagScriptInterpreter interpreter = new(tryTag, scope);
                // Set up the try environment
                TagxExceptions.TryEnvironmentOn();
                // Run the interpreter
                interpreter.Run();
                // Check if there were any errors
                if(TagxExceptions.GetTryException() is not null) {
                    if(variable is not null)
                        variable.SetValue(new DataTypes.DTString(TagxExceptions.GetTryException() ?? ""));
                    // Run the catch block
                    interpreter = new(catchTag, scope);
                    // Run it
                    interpreter.Run();
                }
            }
        }
    }
}