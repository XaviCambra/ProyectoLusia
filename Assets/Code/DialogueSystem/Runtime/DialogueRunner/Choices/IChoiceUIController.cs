using System;
using System.Collections.Generic;

public interface IChoiceUIController
{
    void Init();
    void BeginNode(); // ← nuevo: limpiar inmediatamente al iniciar nodo
    bool Show(DialogueNodeData node, IEnumerable<DialogueNodeData.ChoiceData> visible, Action<string> onClick);
    void Hide();
    bool TryConsumeHotkey(out string chosenPort);
}