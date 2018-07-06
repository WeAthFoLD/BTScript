

using System;
using System.Collections.Generic;

namespace BTScript {

public class NodeRegistry {

    public static readonly NodeRegistry Instance = new NodeRegistry();

    static NodeRegistry() {
        Instance.AddControl<PriorityNode>("Prio");
        Instance.AddControl<SequenceNode>("Seq");
        Instance.AddControl<RandomNode>("Rand");
        Instance.AddControl<ParallelAndNode>("ParallelAnd");
        Instance.AddControl<ParallelOrNode>("ParallelOr");

        Instance.AddDecorator<DecoratorSuccessNode>("success");
        Instance.AddDecorator<DecoratorFailNode>("fail");
        Instance.AddDecorator<DecoratorNotNode>("not");
        Instance.AddDecorator<DecoratorWhileNode>("while");
        Instance.AddDecorator<DecoratorWeightNode>("weight");
        Instance.AddDecorator<DecoratorShuffleNode>("shuffle");

        Instance.AddAction<WaitNode>("wait");
        Instance.AddAction<LogNode>("log");
        Instance.AddAction<AnimatorPlayNode>("playAnim");
        Instance.AddAction<SendMessageNode>("msg");
    }

    readonly NodeRegistry parent;

    Dictionary<string, Type> controlNodes = new Dictionary<string, Type>();
    Dictionary<string, Type> decoratorNodes = new Dictionary<string, Type>();
    Dictionary<string, Type> actionNodes = new Dictionary<string, Type>();

    NodeRegistry(NodeRegistry parent = null) {
        this.parent = parent;
    }

    public NodeRegistry Derive() {
        return new NodeRegistry(this);
    }

    public void AddDecorator<T>(string name) where T: DecoratorNode {
        decoratorNodes.Add(name, typeof(T));
    }

    public void AddControl<T>(string name) where T: ControlNode {
        controlNodes.Add(name, typeof(T));
    }

    public void AddAction<T>(string name) where T: ActionNode {
        actionNodes.Add(name, typeof(T));
    }

    public Type GetControl(string name) {
        return Get(self => self.controlNodes, name, "control");
    }

    public Type GetDecorator(string name) {
        return Get(self => self.decoratorNodes, name, "decorator");
    }

    public Type GetAction(string name) {
        return Get(self => self.actionNodes, name, "action");
    }

    T Get<T>(Func<NodeRegistry, Dictionary<string, T>> dictMapper, string name, string type) {
        var dict = dictMapper(this);

        T ret;
        dict.TryGetValue(name, out ret);

        if (ret == null && parent != null) {
            return parent.Get(dictMapper, name, type);
        }

        if (ret == null) {
            throw new Exception("No " + type + " node named " + name + " found.");
        }

        return ret;
    }

}

}