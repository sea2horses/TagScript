namespace TagScript.models {

    public enum DataType {
        NUMBER,
        STRING,
        BOOLEAN
    }

    public class DataTypes {
        public static Dictionary<string, DataType> dataTypeBindings = new() {
            {"string", DataType.STRING},
            {"number", DataType.NUMBER},
            {"boolean", DataType.BOOLEAN}
        };

        public static class AddDataTypes {
            public static DTNumber Add(DTNumber L, DTNumber R)
                => new DTNumber(L.Value + R.Value);
            
            public static DTNumber Add(DTNumber L, DTBoolean R)
                => new DTNumber(L.Value + (R.Value ? 1 : 0));
            
            public static DTNumber Add(DTBoolean L, DTBoolean R)
                => new DTNumber((L.Value ? 1 : 0) + (R.Value ? 1 : 0));
            
            public static DTString Add(DTString L, DTString R)
                => new DTString(L.Value + R.Value);
        }

        public static BaseDataType Add(BaseDataType L, BaseDataType R) {
            // This shit is actually fucked up
            Dictionary<
                (DataType, DataType),
                Func<BaseDataType,BaseDataType,BaseDataType>
            > allowedAdditions = new() {
                // Two numbers added up together
                {
                    (DataType.NUMBER, DataType.NUMBER),
                    (a,b) => AddDataTypes.Add( (DTNumber) a, (DTNumber) b)
                },
                // A number and a boolean
                {
                    (DataType.NUMBER, DataType.BOOLEAN),
                    (a,b) => AddDataTypes.Add( (DTNumber) a, (DTBoolean) b)
                },
                // Two booleans
                {
                    (DataType.BOOLEAN, DataType.BOOLEAN),
                    (a,b) => AddDataTypes.Add( (DTBoolean) a, (DTBoolean) b)
                },
                // Two strings
                {
                    (DataType.STRING, DataType.STRING),
                    (a,b) => AddDataTypes.Add( (DTString) a, (DTString) b)
                }
            };

            // Now check all
            Func<BaseDataType,BaseDataType,BaseDataType>? obtainedFunc = null;
            DataType LType = L.Type(), RType = R.Type();

            if(allowedAdditions.TryGetValue( (LType, RType), out obtainedFunc))
                allowedAdditions.TryGetValue( (RType, LType), out obtainedFunc);
            
            if(obtainedFunc is null)
                throw new Exception($"Data types of {LType} and {RType} cannot be added");

            return obtainedFunc(L, R);
        }

        public abstract class BaseDataType {
            public abstract DataType Type();
            public abstract object Get();
            public abstract string Format(string[] args);
            public abstract BaseDataType Clone();
            public BaseDataType Assert(DataType type) {
                if(type != Type())
                    throw new Exception($"Was expecting {Type()} but got {type}");
                return this;
            }
        }

        public class DTNumber : BaseDataType {
            public double Value { get; set; }

            public DTNumber(double val) {
                Value = val;
            }

            public override string Format(string[] args) { return Value.ToString(); }
            public override DataType Type() => DataType.NUMBER;
            public override object Get() => Value;
            public override BaseDataType Clone() => new DTNumber(Value);
        }

        public class DTString : BaseDataType {
            public string Value { get; set; }

            public DTString(string val) {
                Value = val;
            }

            public override string Format(string[] args) { return Value; }
            public override DataType Type() { return DataType.STRING; }
            public override object Get() => Value;
            public override BaseDataType Clone() => new DTString(Value);
        }

        public class DTBoolean : BaseDataType {
            public bool Value { get; set; }

            public DTBoolean(bool val) {
                Value = val;
            }

            public override string Format(string[] args) { return (Value ? "True" : "False"); }
            public override DataType Type() { return DataType.BOOLEAN; }
            public override object Get() => Value;
            public override BaseDataType Clone() => new DTBoolean(Value);
        }
    }
}