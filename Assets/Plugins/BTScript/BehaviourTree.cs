using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTScript {

public class BehaviourTree : MonoBehaviour {

    public BehaviourScript source;

    public float lateStartMin, lateStartMax;

    [HideInInspector]
    public bool paused;

    public readonly Dictionary<string, object> blackboard = new Dictionary<string, object>();

    public Node rootNode { get; private set; }

    bool loaded = false;

    public void SetCondBool(string name, bool value) {
        SetCondVal(name, value);
    }

    public void SetCondFloat(string name, float value) {
        SetCondVal(name, value);        
    }

    public void SetCondInt(string name, int value) {
        SetCondVal(name, value);
    }

    public void SetCondObject(string name, object value) {
        Debug.Assert(value is float || value is int || value is bool || value is string);
        SetCondVal(name, value);
    }

    public int GetCondInt(string name) {
        return GetCondVal<int>(name);
    }

    public bool GetCondBool(string name) {
        if (!blackboard.ContainsKey(name))
            return false;
        object val = blackboard[name];
        if (val is bool) {
            return (bool) val;
        } else {
            throw new Exception("Type mismatch: " + val.GetType() + " " + val);
        }
    }

    public float GetCondFloat(string name) {
        if (!blackboard.ContainsKey(name))
            throw new Exception("No condition " + name + " found"); 

        object obj = blackboard[name];
        if (obj is int)
            return (int) obj;
        if (obj is float)
            return (float) obj;
        throw new Exception("Type mismatch: expected int or float, got " + obj.GetType().Name);
    }
    
    public object GetCondObject(string name) {
        return GetCondVal<object>(name);
    }

    public bool HasCond(string name) {
        return blackboard.ContainsKey(name);
    }

    T GetCondVal<T>(string name) {
        if (!blackboard.ContainsKey(name))
            throw new Exception("No condition " + name + " found"); 
        
        object obj = blackboard[name];
        if (obj is T) {
            return (T) obj;
        } else {
            throw new Exception("Type mismatch: expected " + typeof(T).Name + ", got " + obj.GetType().Name);
        }
    }

    void SetCondVal(string name, object value) {
        if (blackboard.ContainsKey(name))
            blackboard[name] = value;
        else
            blackboard.Add(name, value);
    }

    IEnumerator Start() {
        yield return new WaitForSeconds(lateStartMin + (lateStartMax - lateStartMin) * UnityEngine.Random.value);

        var registry = NodeRegistry.Instance;
        foreach (var provider in 
            GetComponents<NodeRegistryProvider>().OrderBy(it => it.priority)) {
            registry = provider.GetRegistry(registry);
        }

        var bts = Compiler.CreateNodeFromAST(source.root, registry);
        rootNode = bts;

        rootNode._Init(this);

        loaded = true;
    }

    void OnEnable() {
        StartCoroutine(Execute());
    }

    public void ResetEvaluate() {
        if (rootNode != null)
            rootNode.Reset();
    }

    IEnumerator Execute() {
        yield return new WaitUntil(() => loaded);
        yield return null;
        rootNode.Reset();

        while (enabled) {
            yield return null;
            if (!paused) {
                var result = rootNode.Evaluate();
                if (result != ExecState.Running) {
                    rootNode.Reset();
                }
            }
        }
    }

}

}