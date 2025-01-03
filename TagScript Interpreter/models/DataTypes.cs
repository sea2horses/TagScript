namespace TagScript.models {

    public enum DataType {
        NUMBER,
        STRING,
        BOOLEAN,
        ARRAY
    }

    public class DataTypes {
        public static Dictionary<string, DataType> dataTypeBindings = new() {
            {"string", DataType.STRING},
            {"number", DataType.NUMBER},
            {"boolean", DataType.BOOLEAN},
            {"array", DataType.ARRAY}
        };


        public abstract class DTGeneric {
            // Generic function to get the type of a child
            public abstract DataType Type();
            // Function that returns the value of the datatype, independently of it's type
            public abstract object Get();
            // ToString equivalent with arguments
            public abstract string Format(string[] args);
            // Clone function
            public abstract DTGeneric Clone();
            public static DTGeneric GenericParse(string textToParse, DataType type) {
                switch(type) {
                    case(DataType.NUMBER): return DTNumber.Parse(textToParse);
                    case(DataType.STRING): return DTString.Parse(textToParse);
                    case(DataType.BOOLEAN): return DTBoolean.Parse(textToParse);
                    default: throw new Exception($"{type} does not have a generic parsing method");
                }
            }
            // Assert generic function
            public DTGeneric Assert(DataType type) {
                if(type != Type())
                    throw new Exception($"Was expecting {Type()} but got {type}");
                return this;
            }
        }

        public class DTNumber : DTGeneric {
            public double Value { get; set; }

            public DTNumber(double val) {
                Value = val;
            }

            public override string Format(string[] args) { return Value.ToString(); }
            public override DataType Type() => DataType.NUMBER;
            public override object Get() => Value;
            public override DTGeneric Clone() => new DTNumber(Value);
            public static DTNumber Parse(string textToParse)
            {
                if(!double.TryParse(textToParse, out double parsedValue))
                    throw new Exception($"String {textToParse} cannot be parsed to DataType {DataType.NUMBER}");
                else return new DTNumber(parsedValue);
            }
            public static bool TryParse(string textToParse, out DTNumber? numberHolder)
            {
                numberHolder = null;
                try {
                    numberHolder = DTNumber.Parse(textToParse);
                    return true;
                } catch (Exception) {
                    return false;
                }
            }
        }

        public class DTString : DTGeneric {
            public string Value { get; set; }

            public DTString(string val) {
                Value = val;
            }

            public override string Format(string[] args) { return $"\"{Value}\""; }
            public override DataType Type() { return DataType.STRING; }
            public override object Get() => Value;
            public override DTGeneric Clone() => new DTString(Value);

            public static DTString Parse(string textToParse)
            {
                return new DTString(textToParse);
            }
            public static bool TryParse(string textToParse, out DTString? stringHolder)
            {
                stringHolder = null;
                try {
                    stringHolder = DTString.Parse(textToParse);
                    return true;
                } catch (Exception) {
                    return false;
                }
            }
        }

        public class DTBoolean : DTGeneric {
            public bool Value { get; set; }

            public DTBoolean(bool val) {
                Value = val;
            }

            public override string Format(string[] args) { return (Value ? "True" : "False"); }
            public override DataType Type() { return DataType.BOOLEAN; }
            public override object Get() => Value;
            public override DTGeneric Clone() => new DTBoolean(Value);
            public static DTBoolean Parse(string textToParse)
            {
                if(!bool.TryParse(textToParse, out bool parsedValue))
                    throw new Exception($"String {textToParse} cannot be parsed to DataType {DataType.BOOLEAN}");
                else return new DTBoolean(parsedValue);
            }
            public static bool TryParse(string textToParse, out DTBoolean? booleanHolder)
            {
                booleanHolder = null;
                try {
                    booleanHolder = DTBoolean.Parse(textToParse);
                    return true;
                } catch (Exception) {
                    return false;
                }
            }
        }

        public class DTArray : DTGeneric {
            public List<DTGeneric> Container { get; set; }

            public DTArray(List<DTGeneric> val) {
                Container = val;
            }

            public override string Format(string[] args) {
                string result = "[ ";
                for(int i = 0; i < Container.Count; i++) {
                    if(i > 0) result += ", ";
                    result += Container[i].Format([]);
                }
                if(Container.Count == 0) result += "'empty'";
                result += " ]";

                return result;
            }
            public override DataType Type() { return DataType.ARRAY; }
            public override object Get() => Container;
            public override DTGeneric Clone() => new DTArray(Container);
        }

        public class TypeOperations {
            // Class to abstract away unary opartions
            private class UnaryOperations {
                private Dictionary<DataType, Func<DTGeneric, DTGeneric>>
                    functionDictionary;

                public UnaryOperations(Dictionary<DataType, Func<DTGeneric, DTGeneric>> dictionary) {
                    this.functionDictionary = dictionary;
                }

                public Func<DTGeneric, DTGeneric>? GetFunction(DataType O) {
                    Func<DTGeneric, DTGeneric>? opFunc;
                    if(functionDictionary.TryGetValue(O, out opFunc))
                        return opFunc;
                    return null;
                }
            }

            private static UnaryOperations allowedNegations = new(
                new() {
                    {DataType.BOOLEAN, (a) => new DTBoolean( !((DTBoolean)a).Value )},
                    {DataType.NUMBER, (a) => new DTNumber( ((DTNumber)a).Value * (-1) )}
                }
            );

            private static Dictionary<string, UnaryOperations> unaryOperations = new() {
                {"negate", allowedNegations}
            };

            // Class to abstract away since I can't be bothered to write these long ass datatypes everytime
            private class BinaryOperations {
                private Dictionary<(DataType, DataType), Func<DTGeneric, DTGeneric, DTGeneric>>
                    functionDictionary;

                public BinaryOperations(Dictionary<(DataType, DataType), Func<DTGeneric, DTGeneric, DTGeneric>> dictionary) {
                    this.functionDictionary = dictionary;
                }

                public Func<DTGeneric, DTGeneric, DTGeneric>? GetFunction(DataType L, DataType R) {
                    Func<DTGeneric, DTGeneric, DTGeneric>? opFunc;
                    if(functionDictionary.TryGetValue( (L, R), out opFunc))
                        return opFunc;
                    if(functionDictionary.TryGetValue( (R, L), out opFunc))
                        return opFunc;
                    return null;
                }
            }

            // Allowed additions dictionary
            private static BinaryOperations allowedAdditions = new(
                new() // Creates the dictionary
                {
                    // Addition of two numbers
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( ((DTNumber)a).Value + ((DTNumber)b).Value )},
                    // Addition of a number and a boolean
                    {(DataType.NUMBER, DataType.BOOLEAN),
                        (a,b) => new DTNumber( ((DTNumber)a).Value + (((DTBoolean)b).Value ? 1 : 0) )},
                    // Addition of two booleans
                    {(DataType.BOOLEAN, DataType.BOOLEAN),
                        (a,b) => new DTNumber( (((DTBoolean)a).Value ? 1 : 0) + (((DTBoolean)b).Value ? 1 : 0) )},
                    // Addition of two strings
                    {(DataType.STRING, DataType.STRING),
                        (a,b) => new DTString( ((DTString)a).Value + ((DTString)b).Value )}
                });
            
            private static BinaryOperations allowedSubtractions = new(
                new() {
                    // Subtraction of two numbers
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( ((DTNumber)a).Value - ((DTNumber)b).Value )},
                    // Subtraction of a number and a boolean
                    {(DataType.NUMBER, DataType.BOOLEAN),
                        (a,b) => new DTNumber( ((DTNumber)a).Value - (((DTBoolean)b).Value ? 1 : 0) )},
                    // Subtraction of two booleans
                    {(DataType.BOOLEAN, DataType.BOOLEAN),
                        (a,b) => new DTNumber( (((DTBoolean)a).Value ? 1 : 0) - (((DTBoolean)b).Value ? 1 : 0) )},
                }
            );

            private static BinaryOperations allowedMultiplications = new(
                new() {
                    // Multiplication of two numbers
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( ((DTNumber)a).Value * ((DTNumber)b).Value )},
                    // Multiplication of a number and a boolean
                    {(DataType.NUMBER, DataType.BOOLEAN),
                        (a,b) => new DTNumber( ((DTNumber)a).Value * (((DTBoolean)b).Value ? 1 : 0) )},
                    // Multiplication of two booleans
                    {(DataType.BOOLEAN, DataType.BOOLEAN),
                        (a,b) => new DTNumber( (((DTBoolean)a).Value ? 1 : 0) * (((DTBoolean)b).Value ? 1 : 0) )},
                }
            );

            private static BinaryOperations allowedDivisions = new(
                new() {
                    // Division of two numbers
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( ((DTNumber)a).Value / ((DTNumber)b).Value )},
                }
            );

            private static BinaryOperations allowedPowers = new(
                new() {
                    // Raise a number to another number
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( Math.Pow(((DTNumber)a).Value, ((DTNumber)b).Value) )}
                }
            );

            private static BinaryOperations allowedRoots = new(
                new() {
                    // Raise a number to another number
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( Math.Pow(((DTNumber)a).Value, 1 / ((DTNumber)b).Value) )}
                }
            );

            private static BinaryOperations allowedModulos = new(
                new() {
                    // Raise a number to another number
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTNumber( ((DTNumber)a).Value % ((DTNumber)b).Value)}
                }
            );
            
            // Allowed equalities dictionary
            private static BinaryOperations allowedEqualities = new(
                new() {
                    // Equality of two numbers
                    {(DataType.NUMBER, DataType.NUMBER),
                        (a,b) => new DTBoolean( ((DTNumber)a).Value == ((DTNumber)b).Value )},
                    // Equality of a number and a boolean
                    {(DataType.NUMBER, DataType.BOOLEAN),
                        (a,b) => new DTBoolean( ((DTNumber)a).Value == (((DTBoolean)b).Value ? 1 : 0) )},
                    // Equality of two booleans
                    {(DataType.BOOLEAN, DataType.BOOLEAN),
                        (a,b) => new DTBoolean( ((DTBoolean)a).Value == ((DTBoolean)b).Value)},
                    // Equality of two strings
                    {(DataType.STRING, DataType.STRING),
                        (a,b) => new DTBoolean( ((DTString)a).Value == ((DTString)b).Value )}
                });
            

            
            // Dictionary of the binary operations
            private static Dictionary<string, BinaryOperations> binaryOperations = new() {
                {"add", allowedAdditions},
                {"subtract",allowedSubtractions},
                {"multiply",allowedMultiplications},
                {"divide",allowedDivisions},
                {"modulo",allowedModulos},
                {"power",allowedPowers},
                {"root",allowedRoots},
                {"equals", allowedEqualities}
            };

            private static DTGeneric? PerformBinaryOperation(DTGeneric L, DTGeneric R, string operationName) {
                // Get the operation dictionary
                binaryOperations.TryGetValue(operationName, out BinaryOperations? operationIndex);
                if(operationIndex is null)
                    throw new Exception($"Operation {operationName} does not exist");
                // Create the function holder
                Func<DTGeneric, DTGeneric, DTGeneric>? functionHolder;
                // Set the inverse order in case the types are switched
                bool inverseOrder = false;
                // Let's try and get it
                functionHolder = operationIndex.GetFunction(L.Type(), R.Type());
                // If we couldn't, try it with the types switched
                if(functionHolder is null) {
                    functionHolder = operationIndex.GetFunction(R.Type(), L.Type());
                    inverseOrder = true;
                }
                // If it's still null give up a null value
                if(functionHolder is null) return null; 
                // Else, return the result
                return (inverseOrder) ? functionHolder(R,L) : functionHolder(L,R);
            }

            private static DTGeneric? PerformUnaryOperation(DTGeneric O, string operationName) {
                // Get the operation dictionary
                unaryOperations.TryGetValue(operationName, out UnaryOperations? operationIndex);
                if(operationIndex is null)
                    throw new Exception($"Operation {operationName} does not exist");
                // Create the function holder
                Func<DTGeneric, DTGeneric>? functionHolder = operationIndex.GetFunction(O.Type());
                
                return functionHolder is null ? null : functionHolder(O);
            }

            public static DTGeneric Add(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"add");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be added");
                // Else, return the result
                return result;
            }

            public static DTGeneric Subtract(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"subtract");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be subtracted");
                // Else, return the result
                return result;
            }

            public static DTGeneric Multiply(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"multiply");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be multiplied");
                // Else, return the result
                return result;
            }

            public static DTGeneric Divide(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"divide");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be divided");
                // Else, return the result
                return result;
            }

            public static DTGeneric Power(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"power");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be raised");
                // Else, return the result
                return result;
            }

            public static DTGeneric Root(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"root");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} have it's root taken");
                // Else, return the result
                return result;
            }

            public static DTGeneric Modulo(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"modulo");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be modulated");
                // Else, return the result
                return result;
            }

            public static DTGeneric Equals(DTGeneric L, DTGeneric R) {
                // Perform add binary operation
                DTGeneric? result = PerformBinaryOperation(L,R,"equals");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Values of type {L.Type()} and {R.Type()} cannot be compared");
                // Else, return the result
                return result;
            }

            public static DTGeneric Negate(DTGeneric O) {
                // Perform the negation operation
                DTGeneric? result = PerformUnaryOperation(O, "negate");
                // If result is null, throw an exception
                if(result is null)
                    throw new Exception($"Value of type {O.Type()} cannot be negated");
                // Else, return the result
                return result;
            }
        }
    }
}