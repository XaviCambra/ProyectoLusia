public enum GameLanguage { EN, ESP};

public interface ILocalizationService
{
    GameLanguage CurrentLanguage { get; set; }

    // Carpeta dentro de Resources donde se encuentran los archivos de localización
    void Configure(string resourcesFolder);

    // Vuelve a cargar los CSV (por si cambias carpeta o idioma)
    void Reload();

    // Devuelve true si existe la clave en cualquier idioma
    bool Has(string textReference);

    // Intenta obtener el texto según CurrentLanguage con fallback a "en"
    bool TryGet(string textReference, out string localizedText);
}
