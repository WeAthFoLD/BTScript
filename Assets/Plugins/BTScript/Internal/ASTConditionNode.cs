using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTScript.Internal;
using System.Linq;
using System.Text;

namespace BTScript.Internal {

    public class ASTConditionNode : ASTNode {
        public static ASTConditionNode Create(int l) {
            var ret = CreateInstance<ASTConditionNode>();
            ret.name = "?";
            ret.lineNumber = l;
            return ret;
        }

        public string condName;
        public CompareOp op;
        public SerializableArg arg;

        public override void Print(int indent, StringBuilder s) {
            s.AppendLine(Prefix(indent) + "| [?Condition] " + condName + " " + arg.type);
        }
    }

}