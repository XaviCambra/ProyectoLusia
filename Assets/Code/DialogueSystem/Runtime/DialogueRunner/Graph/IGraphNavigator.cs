using System;
using System.Collections.Generic;

public interface IGraphNavigator
{
    void Init(DialogueGraph graph);
    DialogueNodeData StartNode();
    DialogueNodeData NextFrom(DialogueNodeData node, string portName = "Next");
    IEnumerable<DialogueNodeData.ChoiceData> FilterChoices(DialogueNodeData node, Func<DialogueNodeData.ChoiceData, bool> predicate);
    string ResolveSpeaker(DialogueNodeData node);
    string ResolveBody(DialogueNodeData node);
    void RaiseEnterEvents(DialogueNodeData node);
}