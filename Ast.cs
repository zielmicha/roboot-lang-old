namespace MetaComputer.Ast {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using MetaComputer.Util;

    class Location {
        public string Filename;
        public int StartLine;
        public int StartColumn;
        public int EndLine;
        public int EndColumn;

        public Location() {}

        public Location(string filename, int line) {
            this.Filename = filename;
            this.StartLine = line;
            this.StartColumn = 1;
            this.EndLine = line;
            this.EndColumn = 1;
        }
    }

    abstract class Node {
        public Location Location { get; set; }
    }

    abstract class BlockStmt : Node {
    }

    abstract class Expr : Node {
    }

    class ModuleStmt : Node {

    }

    class ModuleDefStmt : ModuleStmt {
        public List<ModuleStmt> Body;
    }

    class ModuleLetStmt : ModuleStmt {

    }

    class BlockLet : BlockStmt {
        public readonly string Name;
        public readonly Expr Type;
        public readonly Expr Value;

        public BlockLet(string name, Expr type, Expr value) {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    //

    class Param : Expr {
        public readonly string Name;
        public readonly Expr Value;
        public readonly bool IsNamed;
        public readonly bool IsOptional;
        public readonly Optional<Expr> DefaultValue;

        public Param(string name, Expr value, bool isNamed=false, bool isOptional=false, Optional<Expr> defaultValue=default(Optional<Expr>)) {
            this.Name = name;
            this.Value = value;
            this.IsNamed = isNamed;
            this.IsOptional = isOptional;
            this.DefaultValue = defaultValue;
        }
    }

    class Params : Expr {
        public readonly IReadOnlyList<Param> ParamList;

        public Params(IReadOnlyList<Param> paramList) {
            this.ParamList = paramList;
        }
    }
    
    class Call : Expr {
        public readonly Expr Func;
        public readonly IReadOnlyList<Expr> Args;
        public readonly IReadOnlyList<KeyValuePair<string, Expr>> NamedArgs;

        public Params MakeParamsNode() {
            return new Params(
                Args
                .Select(arg => new Param(name: null, value: arg))
                .Concat(NamedArgs.Select(arg => new Param(name: arg.Key, value: arg.Value)))
                .ToList());
        }

        public Call(Expr func, List<Expr> args, List<KeyValuePair<string, Expr>> namedArgs = null) {
            this.Func = func;
            this.Args = args;
            this.NamedArgs = namedArgs ?? new List<KeyValuePair<string, Expr>>();
        }

        public override string ToString() {
            var parameters = new List<String>();
            parameters.AddRange(Args.Select(x => x == null ? "null" : x.ToString()));
            parameters.AddRange(NamedArgs.Select(x => "~{x.Item0}:{x.Item1}"));
            var p = string.Join(" ", parameters);
            return $"({Func} {p})";
        }
    }

    class Block : Expr {
        public readonly IReadOnlyList<BlockStmt> Stmts;

        public Block(List<BlockStmt> stmts) {
            this.Stmts = stmts;
        }
    }

    class IntLiteral : Expr {
        public readonly Int64 Value;

        public IntLiteral(Int64 val) {
            this.Value = val;
        }

        public override string ToString() {
            return this.Value.ToString();
        }
    }

    class StringLiteral : Expr {
        public readonly string Value;

        public StringLiteral(string val) {
            this.Value = val;
        }
    }

    class Name : Expr {
        public readonly string Str;

        public Name(string str) {
            this.Str = str;
        }

        public override string ToString() {
            return Str;
        }
    }

    class FunDefExpr : Expr {
        public Expr Body;
    }

    class MatchCase : Node {
        public readonly List<(string name, Expr type)> ImplicitVariables;
        public readonly Expr MatchedValue;
        public readonly Expr Body;

        public MatchCase(List<(string name, Expr type)> implicitVariables,
                         Expr matchedValue, Expr body) {
            this.ImplicitVariables = implicitVariables;
            this.MatchedValue = matchedValue;
            this.Body = body;
        }
    }

    // "Native"

    class NativeValue : Expr {
        public readonly object Value;

        public NativeValue(object value) {
            this.Value = value;
        }
    }

    class CallNative : Expr {
        public readonly LambdaExpression Func;
        public readonly IReadOnlyList<Type> ArgTypes;
        public readonly Type ReturnType;

        public readonly IReadOnlyList<Expr> Args;

        public CallNative(LambdaExpression func, IReadOnlyList<Expr> args) {
            this.Func = func;
            this.Args = args;
        }
    }
}
