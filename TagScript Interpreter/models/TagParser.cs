using System;
using System.Reflection;
using Microsoft.Win32.SafeHandles;
using TagScript.main;

namespace TagScript.models {
    public class TagParser(List<Token> tokenList) {
        List<Token> tokenList = tokenList;
        Token currentToken = new Token(TokenType.UNRECOGNIZED, "", 0, 0);
        int Position = 0;

        void EatToken(TokenType type) {
            // If the types don't match, go fuck yourself
            if(type != tokenList[Position].Type)
                throw new Exception("EatToken given type and current token type do not match");
            
            Console.WriteLine($"Ate a token of type: {type} at position: {Position}. Yummy!");
            
            // Increase the position on list
            Position++;

            // If we're out of range, throw an exception
            if(Position >= tokenList.Count)
                throw new Exception("Token is out of range");

            // Refresh the current token
            currentToken = tokenList[Position];
        }

        Tag ParseTag() {
            // If the first token is an opening angle bracket, eat it
            if(currentToken.Type == TokenType.OPENING_ANGLE_BRACKET)
                EatToken(TokenType.OPENING_ANGLE_BRACKET);
            // If there's a forward slash, then it's an extra closing tag
            if(currentToken.Type == TokenType.FORWARD_SLASH)
                throw new Exception("Extra closing tag");

            // We need an identifier, if it isn't, fuck you
            if(currentToken.Type != TokenType.IDENTIFIER)
                throw new Exception("Was expecting identifier");
            
            // Get the value of the identifier
            string tagName = currentToken.Value;
            // Get the identifier token in case it's necessary
            Token idenToken = currentToken;
            // Eat Tokens yay!!!! :33333
            EatToken(TokenType.IDENTIFIER);

            // Prepare to catch attributes
            Dictionary<string, string> attributeList = [];
            // While loop to get the attributes
            while(currentToken.Type != TokenType.CLOSING_ANGLE_BRACKET
                && currentToken.Type != TokenType.FORWARD_SLASH) {
                    // It's gotta be an identifier guys
                    if(currentToken.Type != TokenType.IDENTIFIER)
                        throw new Exception("Was expecting identifier/attribute or a closing tag '>'");
                
                    // Read the name of the attribute
                    string attributeName = currentToken.Value;
                    // Eat the identifier token
                    EatToken(TokenType.IDENTIFIER);

                    // If the current token isn't an equal sign, it's a self contained attribute
                    if(currentToken.Type != TokenType.EQUALS) {
                        if(!attributeList.TryAdd(attributeName, ""))
                            throw new Exception($"Attribute {attributeName} has already been defined for this tag!");
                    } else {
                        // Yummy equals token
                        EatToken(TokenType.EQUALS);
                        // Next SHOULD be a string literal to mark a value
                        if(currentToken.Type != TokenType.STRING_LITERAL)
                            throw new Exception($"Was expecting string literal");
                        // Boom we're costco guys
                        if(!attributeList.TryAdd(attributeName, currentToken.Value))
                            throw new Exception($"Attribute {attributeName} has already been defined for this tag!");
                        
                        EatToken(TokenType.STRING_LITERAL);
                    }
                }
            
            // If the next is a forward slash, it's a self closing tag and everything is much easier
            if(currentToken.Type == TokenType.FORWARD_SLASH) {
                // Eat the forward slash
                EatToken(TokenType.FORWARD_SLASH);
                // SHOULD be followed by a closing angle bracket
                if(currentToken.Type != TokenType.CLOSING_ANGLE_BRACKET)
                    throw new Exception($"Was expecting closing angle bracket");
                // Eat the closing angle bracket
                EatToken(TokenType.CLOSING_ANGLE_BRACKET);
                // Enjoy the meal :)
                Tag returnTag = new Tag(tagName, attributeList, []);
                returnTag.Position = (idenToken.Line, idenToken.Column);
                return returnTag;
            } else {
                // Else, we need to actually do shit
                
                // This SHOULD be a closing angle bracket
                if(currentToken.Type != TokenType.CLOSING_ANGLE_BRACKET)
                    throw new Exception($"Was expecting a closing angle bracket");
                
                EatToken(TokenType.CLOSING_ANGLE_BRACKET);

                // Create the body container
                List<Tag> body = [];
                // Now let's analyze!
                bool terminate_loop = false;
                while (!terminate_loop) {
                    if(Position >= tokenList.Count) {
                        throw new Exception("Tag was never closed");
                    }

                    switch (currentToken.Type) {
                        case (TokenType.OPENING_ANGLE_BRACKET): {
                            // Eat the Opening Angle Bracket
                            EatToken(TokenType.OPENING_ANGLE_BRACKET);
                            // If the next token is a forward slash, the time has come
                            if(currentToken.Type != TokenType.FORWARD_SLASH)
                                body.Add(ParseTag());
                            else {
                                // Eat the token
                                EatToken(TokenType.FORWARD_SLASH);
                                // You know the drill
                                if(currentToken.Type != TokenType.IDENTIFIER)
                                    throw new Exception("Was expecting identifier");
                                // Get the value of the identifier
                                string closingName = currentToken.Value;
                                // Check that the names match
                                if(closingName != tagName)
                                    throw new Exception($"Mispelled/Extra closing tag - Opening: {tagName}, Closing: {closingName}");
                                EatToken(TokenType.IDENTIFIER);
                                // Eat the final square bracket
                                if(currentToken.Type != TokenType.CLOSING_ANGLE_BRACKET)
                                    throw new Exception("Was expecting a closing angle bracket");
                                EatToken(TokenType.CLOSING_ANGLE_BRACKET);
                                // Flag to terminate the loop
                                terminate_loop = true;
                            }
                            break;
                        }

                        case (TokenType.STRING_LITERAL): {
                            // Create tag from string literal
                            Tag newTag = new("text-lit", new(){ {"body", currentToken.Value } }, []);
                            // Add it to the body
                            body.Add(newTag);
                            // Eat it, yummy :3
                            EatToken(TokenType.STRING_LITERAL);
                            // Break!
                            break;
                        }

                        case (TokenType.NUMBER_LITERAL): {
                            // Create tag from number literal
                            Tag newTag = new("number-lit", new(){ {"value", currentToken.Value } }, []);
                            // Add it to the body
                            body.Add(newTag);
                            // Eat it :333
                            EatToken(TokenType.NUMBER_LITERAL);
                            // Break it
                            break;
                        }

                        case (TokenType.END_OF_FILE): {
                            // Tag was never closed
                            currentToken = idenToken;
                            throw new Exception($"Tag '{tagName}' was never closed");
                        }

                        default: {
                            // Except
                            throw new Exception($"Unexpected token type: {currentToken.Type}");
                        }
                    }
                }

                // This SHOULD be what's returned basically
                Tag returnTag = new Tag(tagName, attributeList, body);
                returnTag.Position = (idenToken.Line, idenToken.Column);
                return returnTag;
            }
        }

        public List<Tag> ParseList() {
            // If the list is empty, go fuck yourself
            if(tokenList.Count == 0)
                throw new Exception("Cannot parse an empty list");
            
            // Assign the current token to the first token on the list
            // Position is 0 by default
            currentToken = tokenList[Position];

            // Return List
            List<Tag> returnList = [];

            do {
                switch(currentToken.Type) {
                    case(TokenType.OPENING_ANGLE_BRACKET): {
                        try {
                            // Parse the new tag
                            Tag newTag = ParseTag();
                            // Add it to the list
                            returnList.Add(newTag);
                        } catch(Exception ex) {
                            // Show exception information
                            TagxExceptions.RaiseException(ex.Message, TagxExceptions.ExceptionType.FATAL,
                                (currentToken.Line, currentToken.Column), currentToken.Value.Length);
                            return [];
                        }
                        break;
                    }

                    case(TokenType.STRING_LITERAL): {
                        // Create the new tag
                        Tag newTag = new Tag("lit-text", new(){ {"body", currentToken.Value} },[]);
                        newTag.Position = (currentToken.Line, currentToken.Column);
                        // Add it to the list
                        returnList.Add(newTag);
                        break;
                    }

                    case (TokenType.END_OF_FILE): {
                        break;
                    }

                    default: {
                        // Except
                        throw new Exception($"Unexpected token type: {currentToken.Type}");
                    }
                }
            } while(Position < tokenList.Count && currentToken.Type != TokenType.END_OF_FILE);

            return returnList;
        }
    }
}