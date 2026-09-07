using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App de ajustes del movil: volumen, notificaciones y un color de acento para
/// personalizar la apariencia. Solo hace de puente entre la UI y PhoneSettings
/// (la fuente de verdad persistente) — no guarda nada por su cuenta.
/// </summary>
public class SettingsApp : PhoneAppBase
{
    [Header("Volumen")]
    [Tooltip("Se aplica de verdad via AudioListener.volume.")]
    [SerializeField] private Slider masterVolumeSlider;
    [Tooltip("Guardado, listo para conectar a un AudioMixer cuando el proyecto tenga uno.")]
    [SerializeField] private Slider musicVolumeSlider;
    [Tooltip("Guardado, listo para conectar a un AudioMixer cuando el proyecto tenga uno.")]
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Notificaciones")]
    [Tooltip("Interruptor global de los iconos de no-leido/pendiente-responder del chat.")]
    [SerializeField] private Toggle notificationsToggle;

    [Header("Apariencia")]
    [Tooltip("Un boton por color de la paleta, en el mismo orden que accentColors.")]
    [SerializeField] private Button[] accentColorButtons;
    [SerializeField] private Color[] accentColors =
    {
        new(0.16f, 0.45f, 0.85f),
        new(0.80f, 0.25f, 0.25f),
        new(0.25f, 0.65f, 0.35f),
        new(0.75f, 0.55f, 0.10f),
    };
    [Tooltip("Elemento que recibe el color de acento elegido (p.ej. el fondo del movil).")]
    [SerializeField] private Image accentTarget;

    private void Awake()
    {
        if (masterVolumeSlider)  masterVolumeSlider.onValueChanged.AddListener(v => PhoneSettings.MasterVolume = v);
        if (musicVolumeSlider)   musicVolumeSlider.onValueChanged.AddListener(v => PhoneSettings.MusicVolume = v);
        if (sfxVolumeSlider)     sfxVolumeSlider.onValueChanged.AddListener(v => PhoneSettings.SfxVolume = v);
        if (notificationsToggle) notificationsToggle.onValueChanged.AddListener(v => PhoneSettings.NotificationsEnabled = v);

        for (int i = 0; i < accentColorButtons.Length; i++)
        {
            var index = i;
            accentColorButtons[i].onClick.AddListener(() => PhoneSettings.AccentColorIndex = index);

            // Pinta cada boton con su propio color de la paleta, para que sea un
            // selector visual (swatches) sin tener que colorearlos a mano en el Editor.
            if (index < accentColors.Length && accentColorButtons[i].targetGraphic)
                accentColorButtons[i].targetGraphic.color = accentColors[index];
        }

        PhoneSettings.OnChanged += ApplyAccent;
        ApplyAccent();
    }

    private void OnDestroy() => PhoneSettings.OnChanged -= ApplyAccent;

    /// <summary>Refresca los controles con los valores guardados. Llamar al abrir la app para no desincronizarse si algo cambio fuera de aqui.</summary>
    public override void OnOpen()
    {
        if (masterVolumeSlider)  masterVolumeSlider.SetValueWithoutNotify(PhoneSettings.MasterVolume);
        if (musicVolumeSlider)   musicVolumeSlider.SetValueWithoutNotify(PhoneSettings.MusicVolume);
        if (sfxVolumeSlider)     sfxVolumeSlider.SetValueWithoutNotify(PhoneSettings.SfxVolume);
        if (notificationsToggle) notificationsToggle.SetIsOnWithoutNotify(PhoneSettings.NotificationsEnabled);
    }

    private void ApplyAccent()
    {
        if (!accentTarget || accentColors.Length == 0) return;
        var index = Mathf.Clamp(PhoneSettings.AccentColorIndex, 0, accentColors.Length - 1);
        accentTarget.color = accentColors[index];
    }
}
