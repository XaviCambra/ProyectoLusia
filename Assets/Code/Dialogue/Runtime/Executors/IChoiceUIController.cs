using System;

public interface IChoiceUIController
{
    void Init();
    void BeginNode();
    bool Show(ChoiceModule module, string nodeGuid, Action<string> onClick);
    void Hide();
    bool TryConsumeHotkey(out string chosenPort);
}
