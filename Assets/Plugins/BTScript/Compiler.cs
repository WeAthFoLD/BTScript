using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTScript.Internal;
using UnityEngine;

namespace BTScript {

    public static class Compiler {
        class TokenType {
            readonly string id;

            public TokenType (string _id) { id = _id; }

            public override string ToString () {
                return id;
            }
        }

        readonly static TokenType
        TK_SPACE = new TokenType ("space"),
            TK_INT = new TokenType ("int"),
            TK_FLOAT = new TokenType ("float"),
            TK_STR = new TokenType ("string"),
            TK_ID = new TokenType ("id"),
            TK_LEFT_BRACE = new TokenType ("'{'"),
            TK_RIGHT_BRACE = new TokenType ("'}'"),
            TK_LEFT_PAREN = new TokenType ("'('"),
            TK_RIGHT_PAREN = new TokenType ("')'"),
            TK_COMMA = new TokenType ("','"),
            TK_AT = new TokenType ("'@'"),
            TK_QUESTION = new TokenType ("'?'"),
            TK_SEMI = new TokenType ("';'"),
            TK_GTEQ = new TokenType ("'>='"),
            TK_LTEQ = new TokenType ("'<='"),
            TK_GT = new TokenType ("'>'"),
            TK_LT = new TokenType ("'<'"),
            TK_EQ = new TokenType ("'=='"),
            TK_NEQ = new TokenType ("'!='"),
            TK_ASSIGN = new TokenType ("'='"),
            TK_BOOL = new TokenType ("bool"),
            TK_COLON = new TokenType ("':'"),
            TK_COMMENT = new TokenType ("comment"),
            TK_EXCLAIM = new TokenType ("'!'");

        readonly static TokenDefinition
        TD_SPACE = new TokenDefinition (@"[\n\r ]+", TK_SPACE, true),
            TD_COMMENT = new TokenDefinition (@"(?m)//.*$", TK_COMMENT, true),
            TD_INT = new TokenDefinition (@"[+\-]?([0-9])+", TK_INT),
            TD_FLOAT = new TokenDefinition (@"[+\-]?[0-9]+\.[0-9]*", TK_FLOAT),
            TD_STR = new TokenDefinition (@"""[^""]*""", TK_STR),
            TD_ID = new TokenDefinition (@"[A-Za-z][A-Za-z0-9\-_]*", TK_ID),
            TD_LEFT_BRACE = new TokenDefinition (@"\{", TK_LEFT_BRACE),
            TD_RIGHT_BRACE = new TokenDefinition (@"\}", TK_RIGHT_BRACE),
            TD_LEFT_PAREN = new TokenDefinition (@"\(", TK_LEFT_PAREN),
            TD_RIGHT_PAREN = new TokenDefinition (@"\)", TK_RIGHT_PAREN),
            TD_COMMA = new TokenDefinition (@",", TK_COMMA),
            TD_AT = new TokenDefinition (@"@", TK_AT),
            TD_QUESTION = new TokenDefinition (@"\?", TK_QUESTION),
            TD_SEMI = new TokenDefinition (@";", TK_SEMI),
            TD_GTEQ = new TokenDefinition (@">=", TK_GTEQ),
            TD_LTEQ = new TokenDefinition (@"<=", TK_LTEQ),
            TD_GT = new TokenDefinition (@">", TK_GT),
            TD_LT = new TokenDefinition (@"<", TK_LT),
            TD_EQ = new TokenDefinition (@"==", TK_EQ),
            TD_NEQ = new TokenDefinition (@"!=", TK_NEQ),
            TD_ASSIGN = new TokenDefinition (@"=", TK_ASSIGN),
            TD_BOOL = new TokenDefinition (@"true|false", TK_BOOL),
            TD_COLON = new TokenDefinition (@":", TK_COLON),
            TD_EXCLAIM = new TokenDefinition (@"\!", TK_EXCLAIM);

        class ScriptParseContext {
            public Lexer lexer;
        }

        public static Node CreateNodeFromAST (ASTNode aNode, NodeRegistry registry) {
            if (aNode is ASTControlNode) {
                var ac = (ASTControlNode) aNode;
                var ret = registry.GetControl (ac.name).Construct<ControlNode> ();
                ret.lineNumber = ac.lineNumber;

                foreach (var aChild in ac.children) {
                    ret.children.Add (CreateNodeFromAST (aChild, registry));
                }
                return ret;
            } else if (aNode is ASTDecoratorNode) {
                var ac = (ASTDecoratorNode) aNode;
                var ret = registry.GetDecorator (ac.name).Construct<DecoratorNode> ();
                ret.lineNumber = ac.lineNumber;

                ret.ReadArgs (ac.args.Select (it => it.objValue).ToList ());
                ret.target = CreateNodeFromAST (ac.target, registry);
                return ret;
            } else if (aNode is ASTActionNode) {
                var ac = (ASTActionNode) aNode;
                var ret = registry.GetAction (ac.name).Construct<ActionNode> ();
                ret.lineNumber = ac.lineNumber;

                var args = new Dictionary<string, object> ();
                foreach (var entry in ac.args) {
                    args.Add (entry.name, entry.arg.objValue);
                }
                ret.ReadArgs (new ActionNodeArgs (args));
                return ret;
            } else if (aNode is ASTConditionNode) {
                var ac = (ASTConditionNode) aNode;
                var ret = new ConditionNode (ac.condName, ac.arg.type, ac.op, ac.arg.floatValue);
                return ret;

            } else if (aNode is ASTSetConditionNode) {
                var ac = (ASTSetConditionNode) aNode;
                var ret = new SetConditionNode (ac.condName, ac.arg.objValue);
                return ret;

            } else {
                throw new Exception ("Unknown AST node type " + aNode);
            }
        }

        static T Construct<T> (this Type type) {
            return (T) (type.GetConstructor (new Type[0]).Invoke (new object[0]));
        }

        public static ASTNode Compile (string source) {
            ScriptParseContext ctx = new ScriptParseContext {
                lexer = new Lexer (new StringReader (source),
                new TokenDefinition[] {
                TD_SPACE,
                TD_COMMENT,
                TD_INT,
                TD_FLOAT,
                TD_BOOL,
                TD_STR,
                TD_ID,
                TD_LEFT_BRACE,
                TD_RIGHT_BRACE,
                TD_LEFT_PAREN,
                TD_RIGHT_PAREN,
                TD_COMMA,
                TD_AT,
                TD_QUESTION,
                TD_SEMI,
                TD_GTEQ,
                TD_LTEQ,
                TD_GT,
                TD_LT,
                TD_EQ,
                TD_NEQ,
                TD_ASSIGN,
                TD_COLON,
                TD_EXCLAIM
                }
                ),
            };

            var t1 = DateTime.Now;
            var ret = ParseNode (ctx);
            var t2 = DateTime.Now;
            Debug.Log("[BTScript] Compilation took " + (t2 - t1).Milliseconds + " ms.");
            return ret;
        }

        static ASTNode ParseNode (ScriptParseContext ctx) {
            var lexer = ctx.lexer;

            string head = _Expect (lexer, TK_ID, TK_COLON, TK_AT, TK_QUESTION, TK_EXCLAIM);
            object headToken = lexer.Token;

            if (headToken == TK_ID) { // Control node
                var nodeName = head;

                var node = ASTControlNode.Create (nodeName, lexer.LineNumber);

                _Expect (lexer, TK_LEFT_BRACE);
                while (true) { // parse child nodes
                    ASTNode n = ParseNode (ctx);
                    node.children.Add (n);

                    // Peek next lexeme
                    _Expect (lexer, TK_ID, TK_COLON, TK_AT, TK_QUESTION, TK_RIGHT_BRACE, TK_EXCLAIM);

                    if (lexer.Token == TK_RIGHT_BRACE)
                        break;

                    lexer.PushCurrent (); // Save last token for subsequent parsing                
                }

                return node;
            } else if (headToken == TK_COLON) { // Decorator node
                string nodeName = _Expect (lexer, TK_ID);

                var node = ASTDecoratorNode.Create (nodeName, lexer.LineNumber);

                _Expect (lexer, TK_LEFT_PAREN, TK_ID, TK_AT, TK_QUESTION, TK_COLON, TK_EXCLAIM);
                if (lexer.Token == TK_LEFT_PAREN) {
                    while (true) {
                        var look2 = _Expect (lexer, TK_RIGHT_PAREN, TK_INT, TK_FLOAT, TK_STR);
                        if (lexer.Token == TK_RIGHT_PAREN) {
                            break;
                        } else {
                            object arg;
                            if (lexer.Token == TK_INT) {
                                arg = int.Parse (look2);
                            } else if (lexer.Token == TK_FLOAT) {
                                arg = float.Parse (look2);
                            } else { // TK_STR
                                arg = look2.Substring (1, look2.Length - 2);
                            }

                            node.args.Add (new SerializableArg (arg));

                            _Expect (lexer, TK_COMMA, TK_RIGHT_PAREN);
                            if (lexer.Token == TK_RIGHT_PAREN) {
                                lexer.PushCurrent ();
                            }
                        }
                    }
                } else {
                    lexer.PushCurrent ();
                }

                node.target = ParseNode (ctx);

                return node;

            } else if (headToken == TK_AT) { // Action node
                string actionName = _Expect (lexer, TK_ID);
                var node = ASTActionNode.Create (actionName, lexer.LineNumber);

                _Expect (lexer, TK_LEFT_PAREN, TK_SEMI);
                if (lexer.Token == TK_SEMI) {
                    return node;
                } else {
                    while (true) {
                        string look1 = _Expect (lexer, TK_RIGHT_PAREN, TK_ID);
                        if (lexer.Token == TK_RIGHT_PAREN) {
                            break;
                        } else {
                            string argName = look1;

                            _Expect (lexer, TK_ASSIGN);

                            object arg = ParseArg (lexer);

                            var wrapped = new SerializableArgPair {
                                name = argName,
                                arg = new SerializableArg (arg)
                            };
                            node.args.Add (wrapped);

                            _Expect (lexer, TK_COMMA, TK_RIGHT_PAREN);
                            if (lexer.Token == TK_RIGHT_PAREN) {
                                lexer.PushCurrent ();
                            }
                        }
                    }

                    _Expect (lexer, TK_SEMI);
                    return node;
                }

            } else if (headToken == TK_EXCLAIM) {
                string argName = _Expect (lexer, TK_ID);
                _Expect (lexer, TK_ASSIGN);
                object arg = ParseArg (lexer);
                _Expect (lexer, TK_SEMI);

                var node = ASTSetConditionNode.Create (lexer.LineNumber);
                node.condName = argName;
                node.arg = new SerializableArg (arg);
                return node;

            } else { // TK_QUESTION, Condition node
                Debug.Assert (headToken == TK_QUESTION);

                string tk = _Expect (lexer, TK_EXCLAIM, TK_ID);

                string condName;

                if (lexer.Token != TK_EXCLAIM) {

                    condName = tk;

                    _Expect (lexer, TK_SEMI, TK_LTEQ, TK_LT, TK_GTEQ, TK_GT, TK_EQ, TK_NEQ);
                    if (lexer.Token == TK_SEMI) { // boolean condition node
                        var node = ASTConditionNode.Create (lexer.LineNumber);
                        node.condName = condName;
                        node.arg.objValue = true;
                        return node;

                    } else { // value condition node
                        CompareOp op = GetCompareOp (lexer.Token as TokenType);

                        string val = _Expect (lexer, TK_FLOAT, TK_INT);
                        var floatVal = float.Parse (val);

                        var node = ASTConditionNode.Create (lexer.LineNumber);
                        node.condName = condName;
                        node.arg.objValue = floatVal;
                        node.op = op;

                        _Expect (lexer, TK_SEMI);

                        return node;
                    }
                } else {
                    condName = _Expect (lexer, TK_ID);

                    if (lexer.Token == TK_ID) {
                        var conditionNode = ASTConditionNode.Create (lexer.LineNumber);
                        conditionNode.condName = condName;
                        conditionNode.arg.type = ConditionType.Bool;
                        conditionNode.op = CompareOp.NotEq;
                        _Expect (lexer, TK_SEMI);
                        return conditionNode;
                    } else {
                        throw _Error (lexer, "only bool varible can be place after '? !'");
                    }
                }
            }
        }

        static object ParseArg (Lexer lexer) {
            string rawval = _Expect (lexer, TK_FLOAT, TK_INT, TK_BOOL, TK_STR);
            if (lexer.Token == TK_INT) {
                return int.Parse (rawval);
            } else if (lexer.Token == TK_FLOAT) {
                return float.Parse (rawval);
            } else if (lexer.Token == TK_BOOL) {
                return bool.Parse (rawval);
            } else { // TK_STR
                return rawval.Substring (1, rawval.Length - 2);
            }
        }

        static CompareOp GetCompareOp (TokenType token) {
            if (token == TK_LTEQ) {
                return CompareOp.LessEq;
            } else if (token == TK_LT) {
                return CompareOp.Less;
            } else if (token == TK_GTEQ) {
                return CompareOp.GreaterEq;
            } else if (token == TK_GT) {
                return CompareOp.Greater;
            } else if (token == TK_EQ) {
                return CompareOp.Eq;
            } else if (token == TK_NEQ) {
                return CompareOp.NotEq;
            } else {
                throw new Exception ();
            }
        }

        static T CreateNodeInstance<T> (Type type, int lineNumber) where T : Node {
            var ctor = type.GetConstructor (new Type[0]);
            var node = ctor.Invoke (new object[0]) as T;
            node.lineNumber = lineNumber;
            return node;
        }

        static string _Expect (Lexer lexer, params TokenType[] types) {
            var nextContent = lexer.NextContent ();
            if (nextContent == null) {
                throw _ErrorEOF (lexer, types);
            } else {
                if (types.Contains (lexer.Token)) {
                    return lexer.TokenContents;
                } else {
                    throw _ErrorUnexpected (lexer, types);
                }
            }
        }

        static void _ExpectEOF (Lexer lexer) {
            if (lexer.Next ()) {
                _Error (lexer, "Expected EOF, found " + lexer.Token);
            }
        }

        static Exception _ErrorEOF (Lexer lexer, params TokenType[] expected) {
            throw _Error (lexer, string.Format ("Expected {0}, found EOF", _TokenTypeStr (expected)));
        }

        static Exception _ErrorUnexpected (Lexer lexer, params TokenType[] expected) {
            throw _Error (lexer, string.Format ("Expected {0}, found {1}:{2}", _TokenTypeStr (expected), lexer.Token, lexer.TokenContents));
        }

        static string _TokenTypeStr (TokenType[] expected) {
            string typeStr = "";
            for (int i = 0; i < expected.Length; ++i) {
                typeStr += expected[i];
                if (i != expected.Length - 1) {
                    typeStr += " or ";
                } else {
                    typeStr += " ";
                }
            }
            return typeStr;
        }

        static Exception _Error (Lexer lexer, string msg) {
            throw new Exception (string.Format ("{0}-{1}: {2}", lexer.LineNumber, lexer.Position, msg));
        }

    }

}