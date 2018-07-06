using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BTScript {

public interface NodeRegistryProvider {

    int priority { get; }

    NodeRegistry GetRegistry(NodeRegistry prev);

}

}