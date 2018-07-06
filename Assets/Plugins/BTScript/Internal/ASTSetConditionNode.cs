using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTScript.Internal;
using System.Linq;
using System.Text;

namespace BTScript.Internal {

    public class ASTSetConditionNode : ASTNode {
        public static ASTSetConditionNode Create(int l) {
            var ret = CreateInstance<ASTSetConditionNode>();
            ret.name = "!";
            ret.lineNumber = l;
            return ret;
        }

        public string condName;
        public SerializableArg arg;

        public override void Print(int indent, StringBuilder s) {
            s.AppendLine(Prefix(indent) + "| [!SetCondition] " + condName);
        }
    }

}