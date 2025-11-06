using System;
using UnityEngine;

public class ProgressConditionListener : MonoBehaviour
{
    private const string MethodName = "PreferenciasNero";

    [SerializeField]
    private bool NeroIsGay = false;

    private void OnEnable()
    {
        // Registra: sin usar el "arg"
        ProgressConditionInvoker.Register(MethodName, (arg) =>
        {
            Debug.LogWarning("ME LLAMAN MARICO Y QUEEEEEEEE!?");
            //Debug.Log($"[ProgressConditionListener] '{MethodName}' invocado. arg={arg ?? "null"}");
            return NeroIsGay;
        });
    }

    private void OnDisable()
    {
        ProgressConditionInvoker.Unregister(MethodName);
    }
}
