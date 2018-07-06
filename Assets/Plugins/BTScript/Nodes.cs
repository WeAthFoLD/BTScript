using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTScript {

public enum ExecState { Running, Success, Fail }

public enum CompareOp { Less, LessEq, Greater, GreaterEq, Eq, NotEq, BoolUnary }

public enum ConditionType { Bool, Float, Int, String }

public class ActionNodeArgs {
    readonly Dictionary<string, object> args;

    public ActionNodeArgs(Dictionary<string, object> args) {
        this.args = args;
    }

    public float GetFloat(string s, float defaultValue) {
        if (!args.ContainsKey(s)) {
            return defaultValue;
        }
        object arg = args[s];
        if (arg is float || arg is int) {
            return arg is int ? (int) arg : (float) arg;
        } else {
            throw TypeMismatch("float or int", arg.GetType());
        }
    }

    public int GetInt(string s, int defaultValue) {
        if (!args.ContainsKey(s)) {
            return defaultValue;
        }
        object arg = args[s];
        if (arg is int) {
            return (int) arg;
        } else {
            throw TypeMismatch("int", arg.GetType());
        }
    }

    public bool GetBool(string s, bool defaultValue) {
        if (!args.ContainsKey(s)) {
            return defaultValue;
        }
        object arg = args[s];
        if (arg is bool) {
            return (bool) arg;
        } else {
            throw TypeMismatch("bool", arg.GetType());
        }
    }

    public string GetString(string s, string defaultValue) {
        if (!args.ContainsKey(s)) {
            return defaultValue;
        }
        object arg = args[s];
        if (arg is string) {
            return (string) arg;
        } else {
            throw TypeMismatch("string", arg.GetType());
        }
    }

    public object GetObject(string s, object defaultValue) {
        if (!args.ContainsKey(s)) {
            return defaultValue;
        }
        return args[s];
    }

    Exception TypeMismatch(string expected, Type actual) {
        throw new Exception(string.Format("Type mismatch: expected {0}, got {1}", expected, actual.Name));
    }
}

// --- Base nodes

public abstract class Node {

    public BehaviourTree tree { get; private set; }

    public int lineNumber; // The line number of node in source file

    public virtual void _Init(BehaviourTree tree) {
        this.tree = tree;
    }

    public abstract ExecState Evaluate();

    public virtual void Reset() {}

    public virtual Node[] GetActiveChildren() { return new Node[0]; }

}

public abstract class ControlNode : Node {

    public readonly List<Node> children = new List<Node>();

    public override void _Init(BehaviourTree script) {
        base._Init(script);
        foreach (var child in children) {
            child._Init(script);
        }
    }

    public override void Reset() {
        foreach (var child in children) {
            child.Reset();
        }
    }

}

public abstract class DecoratorNode : Node {

    public Node target;

    public override void _Init(BehaviourTree script) {
        base._Init(script);
        target._Init(script);
    }

    public override void Reset() {
        target.Reset();
    }

    public virtual void ReadArgs(List<object> args) {}

    public override Node[] GetActiveChildren() {
        return new Node[] { target };
    }

}

public abstract class ActionNode : Node {

    public virtual void ReadArgs(ActionNodeArgs args) {}

}

// -- Impl control nodes

public class PriorityNode : ControlNode {

    int execNodeIndex = -1;

    public override ExecState Evaluate() {
        if (execNodeIndex == -1) {
            execNodeIndex = 0;
        }

        while (execNodeIndex < children.Count) {
            var state = children[execNodeIndex].Evaluate();
            if (state == ExecState.Running) {
                return ExecState.Running;
            } else {
                if (state == ExecState.Success) {
                    return ExecState.Success;
                }
                // else: Fail, continue
            }

            ++execNodeIndex;
        }

        execNodeIndex = -1;
        return children.Count == 0 ? ExecState.Success : ExecState.Fail;
    }

    public override void Reset() {
        execNodeIndex = -1;
        base.Reset();
    }

    public override Node[] GetActiveChildren() {
        if (execNodeIndex == -1) {
            return new Node[0];
        } else {
            return new Node[] { children[execNodeIndex] };
        }
    }

}

public class SequenceNode : ControlNode {

    int execNodeIndex = -1;

    public override ExecState Evaluate() {
        if (execNodeIndex == -1) {
            execNodeIndex = 0;
        }

        while (execNodeIndex < children.Count) {
            var state = children[execNodeIndex].Evaluate();
            if (state == ExecState.Running) {
                return ExecState.Running;
            } else {
                if (state == ExecState.Fail) {
                    return ExecState.Fail;
                }
                // else: Success, continue
            }

            ++execNodeIndex;
        }

        execNodeIndex = -1;
        return ExecState.Success;
    }

    public override void Reset() {
        execNodeIndex = -1;
        base.Reset();
    }

    public override Node[] GetActiveChildren() {
        if (execNodeIndex == -1) {
            return new Node[0];
        } else {
            return new Node[] { children[execNodeIndex] };
        }
    }

}

public class RandomNode : ControlNode {

    readonly List<float> weights = new List<float>();
    float weightSum = 0;

    int executingIndex = -1;

    public override void _Init(BehaviourTree script) {
        base._Init(script);

        foreach (var node in children) {
            float weight;
            if (node is DecoratorWeightNode) {
                weight = (node as DecoratorWeightNode).weight;
            } else {
                weight = 1;
            }
            weights.Add(weight);
            weightSum += weight;
        }
    }

    public override ExecState Evaluate() {
        if (executingIndex == -1) {
            executingIndex = RandomNodeIndex();
        }

        var result = children[executingIndex].Evaluate();
        if (result != ExecState.Running) {
            executingIndex = -1;
        }
        return result;
    } 

    public override void Reset() {
        executingIndex = -1;
        base.Reset();
    }

    int RandomNodeIndex() {
        if (weights.Count == 0) {
            throw new Exception("No child node available");
        }

        float rand = UnityEngine.Random.value * weightSum;

        float sum = 0;
        for (int i = 0; i < weights.Count; ++i) {
            sum += weights[i];
            if (sum >= rand) {
                return i;
            }
        }
        return weights.Count - 1;
    }

    public override Node[] GetActiveChildren() {
        if (executingIndex == -1) {
            return new Node[0];
        } else {
            return new Node[] { children[executingIndex] };
        }
    }

}

public class ParallelAndNode : ControlNode {

    readonly BitArray executing = new BitArray(32);

    int executingCount;

    public override ExecState Evaluate() {
        if (executingCount == 0) {
            executing.Length = children.Count;
            executing.SetAll(true);
            executingCount = children.Count;
        }

        // string x = "";
        // for (int i = 0; i < executing.Length; ++i) {
        //     x += executing[i] ? '1' : '0';
        // }
        // Debug.Log("Mask: " + x);

        for (int i = 0; i < children.Count; ++i) {
            if (!executing[i]) continue;
            var result = children[i].Evaluate();
            if (result == ExecState.Fail) {
                return ExecState.Fail;
            } else if (result == ExecState.Success) {
                executing[i] = false;
                executingCount -= 1;
            } else { // ExecState.Running
                // DO NOTHING
            }
        }

        if (executingCount == 0) {
            return ExecState.Success;
        } else {
            return ExecState.Running;
        }
    }

    public override void Reset() {
        executingCount = 0;
        base.Reset();
    }

    public override Node[] GetActiveChildren() {
        Node[] nodes = new Node[executingCount];
        int n = 0;
        
        for (int i = 0; i < executing.Length; ++i) {
            if (executing[i]) {
                nodes[n++] = children[i];
            }
        }

        return nodes;
    }

}

public class ParallelOrNode : ControlNode {

    readonly BitArray executing = new BitArray(32);

    int executingCount;

    public override ExecState Evaluate() {
        if (executingCount == 0) {
            executing.Length = children.Count;
            executing.SetAll(true);
            executingCount = children.Count;
        }

        for (int i = 0; i < children.Count; ++i) {
            if (!executing[i]) continue;
            var result = children[i].Evaluate();
            if (result == ExecState.Success) {
                return ExecState.Success;
            } else if (result == ExecState.Fail) {
                executing[i] = false;
                executingCount -= 1;
            } else { // ExecState.Running
                // DO NOTHING
            }
        }

        if (executingCount == 0) {
            return ExecState.Fail;
        } else {
            return ExecState.Running;
        }
    }

    public override void Reset() {
        executingCount = 0;
        base.Reset();
    }

    public override Node[] GetActiveChildren() {
        Node[] nodes = new Node[executingCount];
        int n = 0;
        
        for (int i = 0; i < executing.Length; ++i) {
            if (executing[i]) {
                nodes[n++] = children[i];
            }
        }

        return nodes;
    }

}

public class ConditionNode : Node {

    public readonly string condName;
    public readonly ConditionType condType;
    public readonly CompareOp op;
    public readonly float compareVal;

    public ConditionNode(string condName, ConditionType condType, CompareOp op, float compareVal) {
        this.condName = condName;
        this.condType = condType;
        this.op = op;
        this.compareVal = compareVal;
    }

    public ConditionNode(string condName) : this(condName, ConditionType.Bool, CompareOp.BoolUnary, 0) {}

    public override ExecState Evaluate() {
        bool result;
        if (condType == ConditionType.Bool) {
            result = tree.GetCondBool(condName) != (op == CompareOp.NotEq);
        } else {
            float val = tree.GetCondFloat(condName);
            switch (op) {
                case CompareOp.Eq        : result = val == compareVal; break;
                case CompareOp.NotEq     : result = val != compareVal; break;
                case CompareOp.Less      : result = val <  compareVal; break;
                case CompareOp.LessEq    : result = val <= compareVal; break;
                case CompareOp.Greater   : result = val >  compareVal; break;
                case CompareOp.GreaterEq : result = val >= compareVal; break;
                default                  : throw new Exception("Unreachable");
            }
        }
        return result ? ExecState.Success : ExecState.Fail;
    }

}

public class DecoratorSuccessNode : DecoratorNode {

    public override ExecState Evaluate() {
        var result = target.Evaluate();
        return result == ExecState.Running ? ExecState.Running : ExecState.Success;
    }

}

public class DecoratorFailNode : DecoratorNode {

    public override ExecState Evaluate() {
        var result = target.Evaluate();
        return result == ExecState.Running ? ExecState.Running : ExecState.Fail;
    }

}

public class DecoratorNotNode : DecoratorNode {

    public override ExecState Evaluate() {
        var result = target.Evaluate();
        return result == ExecState.Running ? ExecState.Running : 
            (result == ExecState.Success ? ExecState.Fail : ExecState.Success);
    }

}

public class DecoratorWhileNode : DecoratorNode {

    public override ExecState Evaluate() {
        var result = target.Evaluate();
        if (result == ExecState.Success) {
            target.Reset();
        }
        return result == ExecState.Fail ? ExecState.Fail : ExecState.Running;
    }

}

public class DecoratorWeightNode : DecoratorNode {

    public float weight;

    public override void ReadArgs(List<object> args) {
        object arg = args[0];
        if (arg is int) {
            weight = (int) arg;
        } else if (arg is float) {
            weight = (float) arg;
        } else {
            throw new Exception("Type mismatch");
        }
    }

    public override ExecState Evaluate() {
        return target.Evaluate();
    }

}

public class DecoratorShuffleNode : DecoratorNode {

    public override void Reset() {
        base.Reset();

        var children = ((ControlNode) target).children;
        Shuffle(children);
    }

    public override ExecState Evaluate() {
        return target.Evaluate();
    }

    static void Shuffle<T>(List<T> list) {
        var rnd = new System.Random();
        for (int i = list.Count - 1; i >= 0; i--) {
            int pos = rnd.Next(i);
            var x = list[i - 1];
            list[i - 1] = list[pos];
            list[pos] = x;
        }
    }

}

public class LogNode : ActionNode {
    public string message;

    public override void ReadArgs(ActionNodeArgs args) {
        message = args.GetString("msg", "");
    }

    public override ExecState Evaluate() {
        Debug.Log(message);
        return ExecState.Success;
    }

}

public class WaitNode : ActionNode {

    public float duration;
    public bool unscaled;

    float elapsed;

    public override void ReadArgs(ActionNodeArgs args) {
        duration = args.GetFloat("duration", 0);
        unscaled = args.GetBool("unscaled", false);
    }

    public override ExecState Evaluate() {
        elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (elapsed >= duration) {
            return ExecState.Success;
        }
        return ExecState.Running;
    }

    public override void Reset() {
        elapsed = 0;
    }

}

public class AnimatorPlayNode : ActionNode {
    Animator animator;

    public string clipName;
    public int layer;
    public float normalizedTime;
    public float duration;
    public float speed;

    float t = -1;

    public override void ReadArgs(ActionNodeArgs args) {
        clipName = args.GetString("name", "");
        layer = args.GetInt("layer", -1);
        normalizedTime = args.GetFloat("normalizedTime", float.NegativeInfinity);
        duration = args.GetFloat("duration", -1);
        speed = args.GetFloat("speed", 1);
    }

    public override void _Init(BehaviourTree script) {
        base._Init(script);
        animator = script.GetComponent<Animator>();
    }

    public override ExecState Evaluate() {
        float nt;
        if (t == -1) {
            animator.speed = speed;
            animator.Play(clipName, layer, normalizedTime);
            t = 0;
            nt = normalizedTime;
        } else {
            if (duration == -1) {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.loop) {
                    nt = 0;
                } else {
                    if (stateInfo.IsName(clipName)) {
                        nt = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;                
                    } else {
                        nt = 0;
                    }
                }
            } else {
                nt = duration == 0 ? 1 : t / duration;
            }
        }

        t += Time.deltaTime;

        if (nt >= 1) {
            t = -1;
            return ExecState.Success;
        } else {
            return ExecState.Running;
        }
    }

    public override void Reset() {
        t = -1;
    }
 
}

    public class SendMessageNode : ActionNode {
        string msg;
        object arg;

        public override void ReadArgs(ActionNodeArgs args) {
            msg = args.GetString("name", "");
            arg = args.GetObject("arg", null);
        }


        public override ExecState Evaluate() {
            if (arg == null) {
                tree.SendMessage(msg, SendMessageOptions.DontRequireReceiver);
            } else {
                tree.SendMessage(msg, arg, SendMessageOptions.DontRequireReceiver);
            }
            return ExecState.Success;
        }
    }

    public class SetConditionNode : Node {
    
    string name;
    object value;

    public SetConditionNode(string _name, object _value) {
        name = _name;
        value = _value;
    }

    public override ExecState Evaluate() {
        tree.SetCondObject(name, value);
        return ExecState.Success;
    }

}


}