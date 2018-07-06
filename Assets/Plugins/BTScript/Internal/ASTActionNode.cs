using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTScript.Internal;
using System.Linq;
using System.Text;

namespace BTScript.Internal {

    public class ASTActionNode : ASTNode {
        public static ASTActionNode Create(string n, int l) {
             var ret = ScriptableObject.CreateInstance<ASTActionNode>();
            ret.name = n;
            ret.lineNumber = l;
            return ret;
        }

        public List<SerializableArgPair> args = new List<SerializableArgPair>();

        public override void Print(int indent, StringBuilder s) {
            s.AppendLine(Prefix(indent) + "| [Action] " + name);
        }
    }

}