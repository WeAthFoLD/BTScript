using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BTScript;
using System;
using System.Text;

namespace BTScript.Internal {

    [Serializable]
    public struct SerializableArg {
        public ConditionType type;
        public int intValue;
        public float floatValue;
        public string strValue;

        public bool boolValue {
            get {
                return intValue != 0;
            }
            set {
                intValue = value ? 1 : 0;
            }
        }

        public object objValue {
            get {
                switch (type) {
                    case ConditionType.Bool: return boolValue;
                    case ConditionType.Int: return intValue;
                    case ConditionType.Float: return floatValue;
                    case ConditionType.String: return strValue;
                }
                return intValue;
            }
            set {
                if (value is int) {
                    type = ConditionType.Int;
                    intValue = (int) value;
                } else if (value is bool) {
                    type = ConditionType.Bool;
                    boolValue = (bool) value;
                } else if (value is float) {
                    type = ConditionType.Float;
                    floatValue = (float) value;
                } else {
                    type = ConditionType.String;
                    strValue = (string) value;
                }
            }
        }

        public SerializableArg(object obj) : this() {
            objValue = obj;
        }
    }

    [Serializable]
    public struct SerializableArgPair {
        public string name;
        public SerializableArg arg;
        
    }

    public abstract class ASTNode : ScriptableObject {
        public int lineNumber;

        public abstract void Print(int indent, StringBuilder s);

        protected string Prefix(int indent) {
            return new string('-', indent * 2);
        }
    }


}