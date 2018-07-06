using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTScript.Internal;
using System.Linq;
using System.Text;

namespace BTScript.Internal {

    public class ASTControlNode : ASTNode {
        public static ASTControlNode Create(string n, int l) {
            var ret = ScriptableObject.CreateInstance<ASTControlNode>();
            ret.name = n;
            ret.lineNumber = l;
            return ret;
        }

        public List<ASTNode> children = new List<ASTNode>();

        public override void Print(int indent, StringBuilder s) {
            s.AppendLine(Prefix(indent) + "| [Control] " + name);
            foreach (var child in children) {
                child.Print(indent + 1, s);
            }
        }
    }

}