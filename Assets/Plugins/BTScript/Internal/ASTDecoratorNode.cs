
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTScript.Internal;
using System.Linq;
using System.Text;

namespace BTScript.Internal {

    public class ASTDecoratorNode : ASTNode {
        public static ASTDecoratorNode Create(string n, int l) {
             var ret = ScriptableObject.CreateInstance<ASTDecoratorNode>();
            ret.name = n;
            ret.lineNumber = l;
            return ret;
        }

        public List<SerializableArg> args = new List<SerializableArg>();
        public ASTNode target;

        public override void Print(int indent, StringBuilder s) {
            s.AppendLine(Prefix(indent) + "| [Decorator] " + name);
            target.Print(indent + 1, s);
        }
    }

}