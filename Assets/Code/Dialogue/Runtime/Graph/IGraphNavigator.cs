using System;
using System.Collections.Generic;

public interface IGraphNavigator
{
    void Init(DialogueGraph graph);
    DialogueNodeData StartNode();
    DialogueNodeData NextFrom(DialogueNodeData node, string portName = "Next");
    string ResolveSpeaker(DialogueNodeData node);
    string ResolveBody(DialogueNodeData node);
    void RaiseEnterEvents(DialogueNodeData node);
}