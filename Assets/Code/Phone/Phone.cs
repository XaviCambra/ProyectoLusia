using UnityEngine;

/// <summary>
/// Singleton persistente que gestiona la apertura/cierre del móvil y
/// la navegación entre apps. Descubre las apps automáticamente via
/// <see cref="GetComponentsInChildren{PhoneAppBase}"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class Phone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject  phoneRoot;
    [SerializeField] private GameObject  homeScreen;
    [SerializeField] private Transform   appIconParent;
    [SerializeField] private AppIconButton iconPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    public static Phone Instance { get; private set; }

    private PhoneAppBase[] _apps;
    private PhoneAppBase   _activeApp;
    private bool           _isOpen;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _apps = GetComponentsInChildren<PhoneAppBase>(true);

        foreach (var app in _apps)
        {
            app.Hide();
            var btn = Instantiate(iconPrefab, appIconParent);
            btn.Set(app.AppName, app.AppIcon, () => OpenApp(app));
        }

        phoneRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    // -----------------------------------------------------------------------

    public void Toggle()          => SetOpen(!_isOpen);
    public void Open()            => SetOpen(true);
    public void Close()           => SetOpen(false);

    public void GoHome()
    {
        CloseActiveApp();
        homeScreen.SetActive(true);
    }

    public void OpenApp(PhoneAppBase app)
    {
        CloseActiveApp();
        homeScreen.SetActive(false);
        _activeApp = app;
        app.Show();
        app.OnOpen();
    }

    // -----------------------------------------------------------------------

    private void SetOpen(bool open)
    {
        _isOpen = open;
        phoneRoot.SetActive(open);
        if (!open)
        {
            CloseActiveApp();
            homeScreen.SetActive(true);
        }
    }

    private void CloseActiveApp()
    {
        if (_activeApp == null) return;
        _activeApp.OnClose();
        _activeApp.Hide();
        _activeApp = null;
    }
}
