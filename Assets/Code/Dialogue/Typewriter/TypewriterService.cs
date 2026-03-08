using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TypewriterService
{
    public async Task RunAsync(
        string text,
        TypewriterProfile profile,
        TMP_Text target = null,
        Action<string> onTextUpdate = null,
        CancellationToken ct = default)
    {
        if (profile == null)
        {
            Debug.LogWarning("[TypewriterService] No se asigno ningun perfil de configuracion.");
            return;
        }

        var sb = new StringBuilder(text.Length);
        if (target) target.text = "";

        int i = 0;
        while (i < text.Length)
        {
            ct.ThrowIfCancellationRequested();

            // Soporte RichText: salta la etiqueta sin delay
            if (profile.respectRichText && text[i] == '<')
            {
                int close = text.IndexOf('>', i + 1);
                if (close != -1)
                {
                    sb.Append(text, i, close - i + 1);
                    target?.SetText(sb);
                    i = close + 1;
                    continue;
                }
            }

            // Puntos suspensivos: trata "..." como un solo token
            if (IsEllipsis(text, i))
            {
                sb.Append("...");
                target?.SetText(sb);
                onTextUpdate?.Invoke(sb.ToString());
                i += 3;
                await Delay(profile, profile.ellipsisPct, ct);
                continue;
            }

            char c = text[i];
            sb.Append(c);
            target?.SetText(sb);
            onTextUpdate?.Invoke(sb.ToString());

            float pct = GetDelayMultiplier(c, profile);
            await Delay(profile, pct, ct);
            i++;
        }
    }

    private static bool IsEllipsis(string s, int i)
        => i + 2 < s.Length && s[i] == '.' && s[i + 1] == '.' && s[i + 2] == '.';

    private static float GetDelayMultiplier(char c, TypewriterProfile p)
    {
        switch (c)
        {
            case ',':                   return p.commaPct;
            case '.':
            case '?':
            case '!':                   return p.periodPct;
            case ':':
            case ';':                   return p.colonPct;
            case '"':
            case '\u00AB': // «
            case '\u00BB': // »
            case '\u00BF': // ¿
            case '\u00A1': // ¡
            case '(':
            case ')':
            case '[':
            case ']':
            case '{':
            case '}':                   return p.bracketPct;
            case ' ':
            case '\t':
            case '\n':
            case '\r':                  return p.whitespacePct;
            default:                    return 1f;
        }
    }

    private static async Task Delay(TypewriterProfile p, float pct, CancellationToken ct)
    {
        float t = Mathf.Max(0.001f, p.secondsPerChar * pct);
        await Task.Delay(TimeSpan.FromSeconds(t), ct);
    }
}
