using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CsvLocalizationService : MonoBehaviour, ILocalizationService
{
    [Header("Carpeta en Resources (paramétrico)")]
    [SerializeField] private string resourcesFolder = "Localization"; // p.ej. Resources/Localization/*.csv

    [Header("Idioma actual")]
    [SerializeField] private GameLanguage current = GameLanguage.EN;

    // textRef -> (langKey -> text)
    private readonly Dictionary<string, Dictionary<string, string>> _table = new();
    // columnas detectadas (min: textReference, esp, en...)
    private readonly HashSet<string> _langs = new();

    public GameLanguage CurrentLanguage
    {
        get => current;
        set => current = value;
    }

    public void Configure(string folder)
    {
        resourcesFolder = folder;
        Reload();
    }

    public void Reload()
    {
        _table.Clear();
        _langs.Clear();

        var assets = Resources.LoadAll<TextAsset>(resourcesFolder);
        foreach (var ta in assets)
            LoadCsv(ta.text);
    }

    public bool Has(string key) => _table.ContainsKey(key);

    public bool TryGet(string key, out string value)
    {
        value = null;
        if (!_table.TryGetValue(key, out var row)) return false;

        // idioma seleccionado → fallback a "en"
        var want = LangKey(CurrentLanguage);
        if (row.TryGetValue(want, out var v) && !string.IsNullOrEmpty(v)) { value = v; return true; }
        if (row.TryGetValue("en", out var ve) && !string.IsNullOrEmpty(ve)) { value = ve; return true; }

        return false;
    }

    // --- Helpers ---

    private static string LangKey(GameLanguage g) => g == GameLanguage.ESP ? "esp" : "en";

    private void LoadCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;
        var lines = csv.Split('\n')
                       .Select(l => l.TrimEnd('\r'))
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .ToArray();
        if (lines.Length == 0) return;

        // Cabecera
        var header = SplitCsvLine(lines[0]).Select(h => h.Trim()).ToArray();
        var idxRef = System.Array.FindIndex(header, h => h.Equals("textReference"));
        if (idxRef < 0) return; // cabecera inválida

        var langCols = new List<(int idx, string key)>();
        for (int i = 0; i < header.Length; i++)
        {
            var h = header[i].Trim().ToLowerInvariant();
            if (i == idxRef) continue;
            _langs.Add(h);
            langCols.Add((i, h));
        }

        // Filas
        for (int li = 1; li < lines.Length; li++)
        {
            var cols = SplitCsvLine(lines[li]);
            if (cols.Count <= idxRef) continue;

            var key = cols[idxRef].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            if (!_table.TryGetValue(key, out var dict))
            {
                dict = new Dictionary<string, string>();
                _table[key] = dict;
            }

            foreach (var (idx, langKey) in langCols)
            {
                if (idx < cols.Count)
                    dict[langKey] = Unquote(cols[idx]).Trim();
            }
        }
    }

    // Soporte simple de CSV (coma o punto y coma) y comillas dobles
    private static List<string> SplitCsvLine(string line)
    {
        var sep = line.Contains(';') && !line.Contains(',') ? ';' : ',';
        var res = new List<string>();
        bool inQ = false;
        var cur = new System.Text.StringBuilder();

        foreach (var ch in line)
        {
            if (ch == '\"') { inQ = !inQ; cur.Append(ch); }
            else if (ch == sep && !inQ) { res.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        res.Add(cur.ToString());
        return res;
    }

    private static string Unquote(string s)
    {
        s = s?.Trim();
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length >= 2 && s[0] == '\"' && s[^1] == '\"')
            return s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
        return s;
    }

    // Carga inicial si está en escena
    private void Awake()
    {
        Reload();
    }
}
