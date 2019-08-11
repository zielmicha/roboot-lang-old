namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Roboot.Util;

    public class Struct {
        public readonly IReadOnlyList<StructField> Fields;

        public readonly Optional<string> Name;

        public readonly IReadOnlyList<object> Attributes;

        public Struct(Optional<string> name, IReadOnlyList<StructField> fields, IReadOnlyList<object> attributes) {
            this.Name = name;
            this.Fields = fields;
            this.Attributes = attributes;
        }
    }

    public class StructField {
        public readonly string Name;

        public readonly object Type;

        public readonly IReadOnlyList<object> Attributes;

        public StructField(string name, object type, IReadOnlyList<object> attributes) {
            this.Name = name;
            this.Type = type;
            this.Attributes = attributes;
        }
    }
}
