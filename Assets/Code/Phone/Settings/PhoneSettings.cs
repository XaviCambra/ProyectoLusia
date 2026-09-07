using System;
using UnityEngine;

/// <summary>
/// Ajustes del movil, persistentes entre partidas via PlayerPrefs (a diferencia
/// del estado de sesion del chat, esto SI debe sobrevivir a cerrar el juego).
/// Fuente unica de verdad: SettingsApp solo lee/escribe aqui, nunca guarda nada
/// por su cuenta. El volumen maestro se aplica de verdad (AudioListener.volume);
/// musica y SFX quedan listos para conectarse a un AudioMixer el dia que exista
/// uno en el proyecto (ver comentario en MusicVolume/SfxVolume).
/// </summary>
public static class PhoneSettings
{
    private const string KeyMasterVolume     = "phone.audio.master";
    private const string KeyMusicVolume      = "phone.audio.music";
    private const string KeySfxVolume        = "phone.audio.sfx";
    private const string KeyNotifications    = "phone.notifications.enabled";
    private const string KeyAccentColorIndex = "phone.appearance.accentIndex";

    /// <summary>Se dispara con cualquier cambio de ajuste, para que la UI que lo muestre se refresque.</summary>
    public static event Action OnChanged;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
        set
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMasterVolume, value);
            AudioListener.volume = value;
            OnChanged?.Invoke();
        }
    }

    // Music/SFX no tienen todavia un AudioMixer que los separe de verdad (el
    // proyecto solo tiene AudioSources sueltas). Se guardan y se notifican igual,
    // listos para que cuando exista un AudioMixer con grupos Music/SFX, SettingsApp
    // le pase estos valores via AudioMixer.SetFloat (en decibelios, no lineal).
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KeyMusicVolume, 1f);
        set { PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value)); OnChanged?.Invoke(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(KeySfxVolume, 1f);
        set { PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value)); OnChanged?.Invoke(); }
    }

    /// <summary>Interruptor global de los iconos de notificacion del chat (ver ChatConversation.Notification).</summary>
    public static bool NotificationsEnabled
    {
        get => PlayerPrefs.GetInt(KeyNotifications, 1) == 1;
        set { PlayerPrefs.SetInt(KeyNotifications, value ? 1 : 0); OnChanged?.Invoke(); }
    }

    /// <summary>Indice del color de acento del movil (paleta fija en SettingsApp). 0 = por defecto.</summary>
    public static int AccentColorIndex
    {
        get => PlayerPrefs.GetInt(KeyAccentColorIndex, 0);
        set { PlayerPrefs.SetInt(KeyAccentColorIndex, value); OnChanged?.Invoke(); }
    }

    /// <summary>Aplica los ajustes persistidos que tienen efecto global inmediato (volumen). Llamar una vez al arrancar.</summary>
    public static void ApplyOnLaunch()
    {
        AudioListener.volume = MasterVolume;
    }
}
