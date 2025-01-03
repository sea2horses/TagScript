using Microsoft.Win32.SafeHandles;

namespace TagScript.models {
    public enum TokenType {
        IDENTIFIER,
        STRING_LITERAL,
        NUMBER_LITERAL,
        OPENING_ANGLE_BRACKET,
        CLOSING_ANGLE_BRACKET,
        FORWARD_SLASH,
        EQUALS,
        COMMA,
        OPENING_SQUARE_BRACKET,
        CLOSING_SQUARE_BRACKET,
        END_OF_FILE,
        UNRECOGNIZED
    }

    public class Token (TokenType type, string value, int column, int line) {
        public TokenType Type { get; } = type;
        public string Value { get; } = value;
        public int Column { get; } = column;
        public int Line { get; } = line;

        public override string ToString()
        {
            return $"< TYPE: {Type}, VALUE: '{Value}' > AT [{Column}:{Line}]";
        }
    }

    public class Tokenizer(string src) {
        string SourceCode { get; } = src;
        int Column { get; set; } = 1;
        int Line { get; set; } = 1;
        int Position { get; set; } = 0;

        Dictionary<char, TokenType> charTypes = new(){
            {'<', TokenType.OPENING_ANGLE_BRACKET},
            {'>', TokenType.CLOSING_ANGLE_BRACKET},
            {'=', TokenType.EQUALS},
            {'/', TokenType.FORWARD_SLASH},
            {',', TokenType.COMMA},
            {'[', TokenType.OPENING_SQUARE_BRACKET},
            {']', TokenType.CLOSING_SQUARE_BRACKET}
        };

        bool IsIdentifierFriendly(char ch) {
            return char.IsAsciiLetterOrDigit(ch) || ch.Equals('-');
        }

        void Pass() {
            Position++;
            Column++;
        }

        void Back() {
            Position--;
            Column--;
        }

        void HandleNewline() {
            Column = 0;
            Line++;
        }

        string ReadString() {
            // This function assumes the current position is at a "

            // We start with an empty string literal
            string stringLiteral = "";
            // Skip the " character
            Pass();
            // Refresh
            char ch = SourceCode[Position]; 
            // While the character isn't another "
            while(!ch.Equals('"') && Position < SourceCode.Length) {
                stringLiteral += ch;
                Pass();
                ch = SourceCode[Position];
            }

            // If the last character wasn't a ", the string wasn't terminated
            if(ch != '"') {
                throw new Exception("String wasn't terminated");
            }

            return stringLiteral;
        }

        string ReadNumber() {
            // This function assumes the current position is at a [

            // We start with an empty number literal
            string numberLiteral = "";
            // Skip the [ character
            Pass();
            // Refresh
            char ch = SourceCode[Position];
            // While the character isn't another "
            while(!ch.Equals(']') && Position < SourceCode.Length) {
                numberLiteral += ch;
                Pass();
                ch = SourceCode[Position];
            }

            // If the last character wasn't a ", the string wasn't terminated
            if(ch != ']') {
                throw new Exception("Number wasn't terminated");
            }

            return numberLiteral;
        }

        string ReadIdentifier() {
            // We start with an empty identifier name
            string identifierName = "";
            // The character to analyze
            char ch = SourceCode[Position];
            // While the character is alpha numeric
            while(IsIdentifierFriendly(ch) && Position < SourceCode.Length) {
                identifierName += ch;
                Pass();
                ch = SourceCode[Position];
            }
            // Return the name read
            return identifierName;
        }

        // string ReadExpression() {

        // }

        public List<Token> Parse() {
            List<Token> returnList = new();

            void PushToken(TokenType type, string value) {
                returnList.Add(new Token(type, value, Column, Line));
            }

            for(Position = 0; Position < SourceCode.Length; Pass()) {
                char ch = SourceCode[Position];

                if(ch.Equals('#')) {
                    Console.WriteLine("Starting commented out");
                    // While a newline isn't found, it will all be commented out
                    while(!ch.Equals('\n') && Position < SourceCode.Length) {
                        Pass();
                        ch = SourceCode[Position];
                    }
                    // Handle it if it exists
                    if(ch.Equals('\n')) {
                        // Handle new line
                        HandleNewline();
                        // Continue
                        continue;
                    }
                } else if(char.IsWhiteSpace(ch)) {
                    // Whitespace handling
                    if(ch.Equals('\n')) {
                        // Handle the new line character
                        HandleNewline();
                        // Continue
                        continue;
                    } 
                } else if(ch.Equals('"')) {
                    // Read the string
                    string stringLiteral = ReadString();
                    // Push the token
                    PushToken(TokenType.STRING_LITERAL, stringLiteral);
                } else if(ch.Equals('[')) {
                    // Read the number
                    string numberLiteral = ReadNumber();
                    // Push the token
                    PushToken(TokenType.NUMBER_LITERAL, numberLiteral);
                } else if(IsIdentifierFriendly(ch)) {
                    // Read the identifier name
                    string identifierName = ReadIdentifier();
                    // Push the token
                    PushToken(TokenType.IDENTIFIER, identifierName);
                    // Offset by 1 so that the last non-alphanumeric character isn't ignored
                    Back();
                } else {
                    // Try to get the token type
                    if(charTypes.TryGetValue(ch, out TokenType type)) {
                        PushToken(type, ch.ToString());
                    }
                    // Else, push an unrecognized token
                    else PushToken(TokenType.UNRECOGNIZED, ch.ToString());
                }
            }

            returnList.Add(new Token(TokenType.END_OF_FILE, "", -1, -1));

            return returnList;
        }
    }
}