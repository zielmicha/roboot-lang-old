grammar McGrammar;

INT : [0-9]+ ;

OP3 : '|' ;
OP4 : 'and' | 'or' | 'xor' ;
OP5 : ('=' | '<' | '>' | '!') ('.' | '+' | '-' | '=' | '>' | '<' | '/' | '*')+
    | '<' | '>';
OP6 : '..' ;
OP7 : '+' | '-' ;
OP8 : (('*' | '/') ('.' | '+' | '-' | '=' | '>' | '<' | '/' | '*')*) | 'div' | 'mod' ;

program : module_def_stmt*;

module_def_stmt : 'module' ident '{' ((module_stmt ';')+ module_stmt? | module_stmt) '}';

module_stmt :
        method_base_stmt |
        import_stmt |
        include_stmt |
        fun_stmt |
        let_stmt |
        type_stmt |
        module_def_stmt;

method_base_stmt : 'method_base' ident;
import_stmt : 'import' ident;
include_stmt : 'include' ident;
fun_stmt : 'fun' ident fundef_expr;
let_stmt : 'let' ident (':' expr)? '=' expr;
val_stmt : 'val' ident ':' expr;

type_stmt : 'type' ident fundef_expr;

block_stmt :
        expr |
        let_stmt |
        return_stmt;

return_stmt : 'return' expr;

fundef_expr : (fundef_arg | '(' fundef_arg* ')') '=>' expr;
fundef_arg :
        ('~'|'~~'|) ident |
        ('~'|'~~'|) '(' ident (':' expr)? ('=' expr)? ')'

expr :
     fundef_expr |
     expr3;  // room for expansion

expr3 : expr3 OP3 expr4 | expr4;
expr4 : expr4 OP4 expr5 | expr5;
expr5 : expr5 OP5 expr6 | expr6;
expr6 : expr6 OP6 expr7 | expr7;
expr7 : expr7 OP7 expr8 | expr8;
expr8 : expr8 OP8 expr9 | expr9;
expr9 : funcall;

funcall : funcall funcallarg | expr10;
funcallarg :
    expr10 |
    '~' ident ':' expr10;

expr10 : expr10 '[' (expr_tuple | expr |) ']' | // translated to getItem
        expr10 '.' ident | // translated to getField
        expr_atom;

expr_atom : '(' expr_tuple ')' |
    '{' expr_block '}' |
    '(' expr ')' |
    if_expr |
    struct_expr |
    atom;

expr_tuple : (expr ',')+ expr?;
expr_block : block_stmt | (block_stmt ';')+ block_stmt?;

if_expr : 'if' expr '{' expr_block '}' ('else' '{' expr_block '}')?;
struct_expr : 'struct' '{' struct_field* '}';
struct_field : ident ':' expr ';';

atom : INT | ident;

ident : QUOTED_IDENT | IDENT;

WS : [ \t\n] -> skip ;
INLINE_COMMENT : '/*' .*? '*/' -> skip ;
BLOCK_COMMENT : '#' .*? '\n' -> skip ;

IDENT : [a-zA-Z_] [a-zA-Z0-9_]* ;
QUOTED_IDENT : '\'' [^\']* '\'';
