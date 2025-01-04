using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Principal;

namespace TagScript.models {

    class ParameterDefinition {
        public string ParameterName { get; }
        public DataType DataType { get; }
        public DataTypes.DTGeneric? DefaultValue { get; } = null;

        public ParameterDefinition(string parameterName, DataType dataType) {
            ParameterName = parameterName;
            DataType = dataType;
        }

        public ParameterDefinition(string parameterName, DataType dataType, DataTypes.DTGeneric defaultValue)
            : this(parameterName, dataType) {
            // Assert the default value
            defaultValue.Assert(dataType);
            // Assign it
            DefaultValue = defaultValue;
        }
    }

    class PassedArgument {
        public string? ArgumentName { get; }
        public DataTypes.DTGeneric ArgumentValue { get; }

        public PassedArgument(DataTypes.DTGeneric argumentValue) {
            ArgumentValue = argumentValue;
        }

        public PassedArgument(DataTypes.DTGeneric argumentValue, string? argumentName)
            : this(argumentValue) {
                ArgumentName = argumentName;
            }
    }

    class BuiltInFunction {
        public string Name { get; }
        public ParameterDefinition[] Parameters;
        private Dictionary<string, int> NameIndexMap;
        // This is for the caller
        Func<DataTypes.DTGeneric[], DataTypes.DTGeneric?> DelegateHolder;
        DataType? ReturnType;

        public BuiltInFunction(string name, ParameterDefinition[] parameters,
            Func<DataTypes.DTGeneric[], DataTypes.DTGeneric?> delegateHolder, DataType? returnType) {
                this.Name = name;
                this.Parameters = new ParameterDefinition[parameters.Length];
                this.NameIndexMap = [];
                // Populate the array and the map
                for(int i = 0; i < parameters.Length; i++) {
                    Parameters[i] = parameters[i];
                    if(!NameIndexMap.TryAdd(parameters[i].ParameterName, i))
                        throw new Exception($"BuiltIn Definition error: Parameter '{parameters[i].ParameterName}' already exists");
                }
                this.DelegateHolder = delegateHolder;
                this.ReturnType = returnType;
            }
        
        public int? GetParameterIndex(string name) {
            if(NameIndexMap.TryGetValue(name, out int resultIndex))
                return resultIndex;
            return null;
        }

        public DataTypes.DTGeneric? Call(PassedArgument[] arguments) {
            // Array for the arguments we were able to catch
            DataTypes.DTGeneric?[] caughtArguments = new DataTypes.DTGeneric?[Parameters.Length];
            // If there's more arguments than parameters, error
            if(arguments.Length > Parameters.Length)
                throw new Exception($"Function '{Name}' argument excess, expected {Parameters.Length} but got {arguments.Length}");
            // Go through parameters
            for(int i = 0; i < arguments.Length; i++) {
                PassedArgument argument = arguments[i];
                int? indexToAdd =
                    (argument.ArgumentName is null) ? i :
                    GetParameterIndex(argument.ArgumentName);
                
                if(indexToAdd is null)
                    throw new Exception($"Function '{Name}': Argument {argument.ArgumentName} does not exist");
                if(caughtArguments[(int) indexToAdd] is not null)
                    throw new Exception($"Function '{Name}': Argument #{i} has already been defined");

                // Add the value to caught arguments
                caughtArguments[(int) indexToAdd] = argument.ArgumentValue;
            }

            DataTypes.DTGeneric[] finalArgumentList = new DataTypes.DTGeneric[Parameters.Length];
            // Make the final list, replacing all the null values for default values
            for(int i = 0; i < Parameters.Length; i++) {
                // If no argument on this index was caught, use the default value if exists
                finalArgumentList[i] = (caughtArguments[i] is null) ?
                    Parameters[i].DefaultValue ?? throw new Exception($"Non-default parameter {Parameters[i].ParameterName} was not provided")
                    : caughtArguments[i]!;
            }
            // Now pass the final argument list
            DataTypes.DTGeneric? result = DelegateHolder(finalArgumentList);
            // Assert the return type
            if (result is null) {
                if (ReturnType is not null)
                    throw new Exception($"Function {Name} not all codepaths return a value");
            } else {
                if (ReturnType is null)
                    throw new Exception($"Function {Name} returned a value when it was marked as void");

                result.Assert((DataType)ReturnType);
                return result;
            }

            return null;
        }
    }

    class BuiltInFunctions {
        private static DataTypes.DTGeneric Round(DataTypes.DTGeneric[] parameters) {
            // This only accepts one parameter
            DataTypes.DTGeneric number = parameters[0];
            // Assert the type
            number.Assert(DataType.NUMBER);
            // Cast it, round it and create a number with it
            return new DataTypes.DTNumber( Math.Round( ((DataTypes.DTNumber)number).Value ) );
        }

        private static void Register(BuiltInFunction function) {
            if(FunctionRegistry.ContainsKey(function.Name))
                throw new Exception($"Function {function.Name} is already registered");

            FunctionRegistry[function.Name] = function;
        }

        public static BuiltInFunction? GetFunction(string name) {
            BuiltInFunction? obtainedFunction = null;
            FunctionRegistry.TryGetValue(name, out obtainedFunction);
            return obtainedFunction;
        }

        private static readonly Dictionary<string, BuiltInFunction> FunctionRegistry = [];

        static BuiltInFunctions() {
            // Round function
            Register(new BuiltInFunction("round", new[] { new ParameterDefinition("x", DataType.NUMBER) }, Round, DataType.NUMBER));
        }
    }
}