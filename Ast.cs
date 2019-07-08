namespace Roboot.Ast {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Util;

    public class Location {
        public string Filename;
        public int StartLine;
        public int StartColumn;
        public int EndLine;
        public int EndColumn;

        public Location() { }

        public Location(string filename, int line) {
            this.Filename = filename;
            this.StartLine = line;
            this.StartColumn = 1;
            this.EndLine = line;
            this.EndColumn = 1;
        }
    }

    abstract public class Node {
        public Location Location { get; set; }
    }

    abstract public class BlockStmt : Node {
    }

    abstract public class Expr : Node {
    }

    public class ModuleStmt : Node {
    }

    public class ModuleDefStmt : ModuleStmt {
        public readonly string Name;

        public readonly List<ModuleStmt> Body;
    }

    public class ModuleLetStmt : ModuleStmt {
        public readonly string Name;

        public readonly Expr Value;

        public readonly Optional<Expr> Type;

        public ModuleLetStmt(string name, Expr value, Optional<Expr> type) {
            this.Name = name;
            this.Value = value;
            this.Type = type;
        }
    }

    public class BlockLet : BlockStmt {
        public readonly string Name;
        public readonly Optional<Expr> Type;
        public readonly Expr Value;

        public BlockLet(string name, Optional<Expr> type, Expr value) {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }

        public override string ToString() {
            if (this.Type.IsSome())
                return $"(let {this.Name} :{this.Type.Get()} {this.Value})";
            else
                return $"(let {this.Name} {this.Value})";
        }
    }

    //

    public class Param : Expr {
        public readonly string Name;
        public readonly Expr Value;
        public readonly bool IsNamed;
        public readonly bool IsOptional;
        public readonly Optional<Expr> DefaultValue;

        public Param(string name, Expr value, bool isNamed = false, bool isOptional = false, Optional<Expr> defaultValue = default(Optional<Expr>)) {
            this.Name = name;
            this.Value = value;
            this.IsNamed = isNamed;
            this.IsOptional = isOptional;
            this.DefaultValue = defaultValue;
        }
    }

    public class Params : Expr {
        public readonly IReadOnlyList<Param> ParamList;

        public Params(IReadOnlyList<Param> paramList) {
            this.ParamList = paramList;
        }
    }

    public class Call : Expr {
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

        public Call(Expr func, IReadOnlyList<Expr> args, IReadOnlyList<KeyValuePair<string, Expr>> namedArgs = null) {
            this.Func = func;
            this.Args = args;
            this.NamedArgs = namedArgs ?? new List<KeyValuePair<string, Expr>>();
        }

        public override string ToString() {
            var parameters = new List<String>();
            parameters.AddRange(Args.Select(x => x == null ? "null" : x.ToString()));
            parameters.AddRange(NamedArgs.Select(x => $"~{x.Key}:{x.Value}"));
            var p = string.Join(" ", parameters);
            return $"(call {Func} {p})";
        }
    }

    public class MakeList : Expr {
        public readonly IReadOnlyList<Expr> Items;

        public MakeList(IReadOnlyList<Expr> items) {
            this.Items = items;
        }

        public override string ToString() {
            return $"(list {string.Join(" ", Items)})";
        }
    }

    public class MakeTuple : Expr {
        public readonly IReadOnlyList<Expr> Items;

        public MakeTuple(IReadOnlyList<Expr> items) {
            this.Items = items;
        }

        public override string ToString() {
            return $"(tuple {string.Join(" ", Items)})";
        }
    }

    public class Block : Expr {
        public readonly IReadOnlyList<BlockStmt> Stmts;

        public Block(List<BlockStmt> stmts) {
            this.Stmts = stmts;
        }

        public override string ToString() {
            return $"(block {string.Join(" ", Stmts)})";
        }
    }

    public class BlockExpr : BlockStmt {
        public readonly Expr Expr;

        public BlockExpr(Expr expr) {
            this.Expr = expr;
        }

        public override string ToString() {
            return this.Expr.ToString();
        }
    }

    public class IntLiteral : Expr {
        public readonly Int64 Value;

        public IntLiteral(Int64 val) {
            this.Value = val;
        }

        public override string ToString() {
            return this.Value.ToString();
        }
    }

    public class StringLiteral : Expr {
        public readonly string Value;

        public StringLiteral(string val) {
            this.Value = val;
        }

        public override string ToString() {
            return "\"" + Value + "\"";
        }
    }

    public class Name : Expr {
        public readonly string Str;

        public Name(string str) {
            this.Str = str;
        }

        public override string ToString() {
            return Str;
        }
    }

    public enum ParamDefKind {
        positional, named, implicit_
    }

    public class ParamDef : Expr {
        public readonly string Name;
        public readonly ParamDefKind Kind;
        public readonly Optional<Expr> Type;
        public readonly Optional<Expr> DefaultValue;

        public ParamDef(string name, ParamDefKind kind, Optional<Expr> defaultValue = default(Optional<Expr>), Optional<Expr> type = default(Optional<Expr>)) {
            this.Name = name;
            this.Kind = kind;
            this.DefaultValue = defaultValue;
            this.Type = type;
        }
    }

    public class FunDefExpr : Expr {
        public readonly IReadOnlyList<ParamDef> Params;
        public readonly Expr Body;

        public FunDefExpr(IReadOnlyList<ParamDef> params_, Expr body) {
            this.Params = params_;
            this.Body = body;
        }
    }

    public class MatchCase : Node {
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

    public class If : Expr {
        public readonly Expr Cond;
        public readonly Expr Then;
        public readonly Optional<Expr> Else;

        public If(Expr cond, Expr then, Optional<Expr> else_) {
            this.Cond = cond;
            this.Then = then;
            this.Else = else_;
        }

        public override string ToString() {
            return $"(if {this.Cond} {this.Then} {this.Else.GetOrNull()})";
        }
    }

    public class Coerce : Expr {
        public readonly Expr Value;
        public readonly Expr Type;

        public Coerce(Expr value, Expr type) {
            this.Value = value;
            this.Type = Type;
        }

        public override string ToString() {
            return $"(coerce {this.Value} {this.Type})";
        }
    }

    // "Native"

    public class NativeValue : Expr {
        public readonly object Value;

        public NativeValue(object value) {
            this.Value = value;
        }
    }

    public class CallNative : Expr {
        public readonly LambdaExpression Func;
        public readonly IReadOnlyList<Type> ArgTypes;
        public readonly Type ReturnType;

        public readonly IReadOnlyList<Expr> Args;

        public CallNative(LambdaExpression func, IReadOnlyList<Expr> args, Type returnType) {
            this.Func = func;
            this.Args = args;
            this.ReturnType = returnType;
        }
    }
}
