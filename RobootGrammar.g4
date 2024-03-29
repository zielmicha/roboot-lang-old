grammar RobootGrammar;

INT : [0-9]+ ;

OP3 : '|' | '|.';
OP4 : 'and' | 'or' | 'xor' ;
OP5 : ('=' | '<' | '>' | '!') ('.' | '+' | '-' | '=' | '>' | '<' | '/' | '*')+
    | '<' | '>';
OP6 : '..' ;
OP7 : '+';
OP8 : (('*' | '/') ('.' | '+' | '-' | '=' | '>' | '<' | '/' | '*')*) | 'div' | 'mod' ;

module : ((module_stmt ';')+ module_stmt? | module_stmt);

module_def_stmt : 'module' ident '{' module '}';

module_stmt :
        method_base_stmt |
        import_stmt |
        include_stmt |
        fun_stmt |
        let_stmt |
        datatype_stmt |
        module_def_stmt;

method_base_stmt : 'method_base' ident;
import_stmt : 'import' ident;
include_stmt : 'include' ident;
fun_stmt : 'fun' ident fundef_expr;
let_stmt : 'let' ident (':' expr)? '=' expr;
val_stmt : 'val' ident ':' expr;

datatype_stmt : 'datatype' ident fundef_expr;

block_stmt :
        expr |
        let_stmt |
        fun_stmt |
        return_stmt;

return_stmt : 'return' expr;

fundef_expr : (fundef_arg*) '=>' expr;
fundef_arg :
        ('~'|'~~'|) ident |
        ('~'|'~~'|) '(' ident (':' expr)? ('=' expr)? ')';

expr :
     fundef_expr |
     expr3;  // room for expansion

expr3 : expr3 OP3 expr4 | expr4;
expr4 : expr4 OP4 expr5 | expr5;
expr5 : expr5 OP5 expr6 | expr6;
expr6 : expr6 OP6 expr7 | expr7;
expr7 : expr7 (OP7 | '-') expr8 | expr8;
expr8 : expr8 OP8 expr9 | expr9;
expr9 : funcall |
    expr10 '(' ')' |
    '-' expr10;

funcall : funcall funcallarg | expr10;
funcallarg :
    expr10 |
    '~' ident ':' expr10;

expr10 : expr10 '.[' (expr_tuple | expr |) ']' | // translated to getItem
         expr10 '.' ident |
         expr11;
expr11: if_expr | expr_atom;

expr_atom : '(' expr_tuple ')' |
    '[' expr_tuple ']' |
    '(' expr_block ')' |
    '(' expr ')' |
    struct_expr |
    atom;

expr_in_parens : '(' expr_block ')' |
    '(' expr ')';
expr_tuple : (expr ',')+ expr?;
expr_block : (block_stmt ';')+ block_stmt?;

if_expr : 'if' expr_atom expr_atom ('else' expr_atom)?;
struct_expr : 'struct' '(' (struct_field ';')* struct_field? ')';
struct_field : ident ':' expr attributes;

attributes : ('@' expr_atom)*;

atom : INT | STRING | ident;

ident : QUOTED_IDENT | IDENT;

WS : [ \t\n] -> skip ;
INLINE_COMMENT : '/*' .*? '*/' -> skip ;
BLOCK_COMMENT : '#' .*? '\n' -> skip ;

STRING : ["] ~["]* ["];

IDENT : [a-zA-Z_] [a-zA-Z0-9_]* ;
QUOTED_IDENT : '\'' [^\']* '\'';
