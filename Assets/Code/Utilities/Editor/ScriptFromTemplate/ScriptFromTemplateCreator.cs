using UnityEditor;
using UnityEngine;

public class ScriptFromTemplateCreator : MonoBehaviour
{
    private const string pathToTemplate_00 = "Assets/Code/Utilities/Editor/ScriptFromTemplate/Templates/AbilityTemplate.txt";

    [MenuItem(itemName: "Assets/Create/Script from Template/Ability", isValidateFunction: false, priority = 1)]
    public static void CreateScriptFromTemplate_00()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(pathToTemplate_00, "new Ability.cs");
    }

}