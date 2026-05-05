using System.Threading.Tasks;
using TMPro;

public interface ITypewriterPresenter
{
    void Init(TextMeshProUGUI target);
    Task ShowAsync(string text, TypewriterProfile profileOrNull);
    /// <summary>
    /// Si hay escritura en curso, la completa y devuelve true; si no, false.
    /// </summary>
    bool FastForwardOrIgnore();
    void Cancel();
}
