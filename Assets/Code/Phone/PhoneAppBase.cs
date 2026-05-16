using UnityEngine;

/// <summary>
/// Base para todas las apps del móvil. Gestiona la visibilidad del panel;
/// las subclases solo sobreescriben <see cref="OnOpen"/> y <see cref="OnClose"/>.
/// </summary>
public abstract class PhoneAppBase : MonoBehaviour, IPhoneApp
{
    [SerializeField] private string     appName = "App";
    [SerializeField] private Sprite     appIcon;
    [SerializeField] private GameObject panel;

    public string AppName => appName;
    public Sprite AppIcon => appIcon;

    internal void Show() { if (panel) panel.SetActive(true); }
    internal void Hide() { if (panel) panel.SetActive(false); }

    public virtual void OnOpen()  { }
    public virtual void OnClose() { }
}
