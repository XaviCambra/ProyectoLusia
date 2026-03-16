using System;
using UnityEngine;

/// <summary>
/// Componente ligero que dispara un evento cada vez que el RectTransform
/// cambia de tamaño. Colocarlo en el Content del ScrollRect permite que
/// otros sistemas (ej. ChatUI) reaccionen sin polling ni coroutines.
/// </summary>
public class ContentSizeListener : MonoBehaviour
{
    public event Action OnSizeChanged;

    private void OnRectTransformDimensionsChange()
    {
        OnSizeChanged?.Invoke();
    }
}
