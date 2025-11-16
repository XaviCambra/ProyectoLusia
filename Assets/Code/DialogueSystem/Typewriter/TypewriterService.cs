using System;
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
            Debug.LogWarning("[TypewriterService] No se asignó ningún perfil de configuración.");
            return;
        }

        string buffer = "";
        if (target) target.text = "";

        int i = 0;
        while (i < text.Length)
        {
            ct.ThrowIfCancellationRequested();

            // Soporte RichText
            if (profile.respectRichText && text[i] == '<')
            {
                int close = text.IndexOf('>', i + 1);
                if (close != -1)
                {
                    string tag = text.Substring(i, close - i + 1);
                    buffer += tag;
                    target?.SetText(buffer);
                    i = close + 1;
                    continue;
                }
            }

            // Detectar puntos suspensivos
            if (IsEllipsis(text, i))
            {
                buffer += "...";
                target?.SetText(buffer);
                onTextUpdate?.Invoke(buffer);
                i += 3;
                await Delay(profile, profile.ellipsisPct, ct);
                continue;
            }

            char c = text[i];
            buffer += c;
            target?.SetText(buffer);
            onTextUpdate?.Invoke(buffer);

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
            case ',': return p.commaPct;
            case '.': return p.periodPct;
            case '?': return p.questionPct;
            case '!': return p.exclaimPct;
            case ';': return p.semicolonPct;
            case ':': return p.colonPct;
            case '"':
            case '«':
            case '»':
            case '“':
            case '”':
            case '‘':
            case '’': return p.quotePct;
            case '(':
            case ')':
            case '[':
            case ']':
            case '{':
            case '}': return p.parenPct;
            case ' ':
            case '\t':
            case '\n':
            case '\r': return p.minimalWhitespaceDelay ? 1f : 1.2f;
            default: return 1f;
        }
    }

    private static async Task Delay(TypewriterProfile p, float pct, CancellationToken ct)
    {
        float t = Mathf.Max(0.001f, p.secondsPerChar * pct * p.globalSpeed);
        await Task.Delay(TimeSpan.FromSeconds(t), ct);
    }
}
