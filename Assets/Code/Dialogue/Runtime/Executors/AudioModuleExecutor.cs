using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="AudioModule"/>.
/// Reproduce un AudioClip a través de un AudioSource gestionado internamente.
/// Si <see cref="AudioModule.waitForCompletion"/> está activo, la Task espera a que el clip termine.
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioModuleExecutor : MonoBehaviour, IModuleExecutor
{
    [SerializeField] private AudioSource audioSource;

    public Type ModuleType => typeof(AudioModule);

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning("[AudioModuleExecutor] No hay AudioSource asignado ni en el GameObject.");
    }

    public void Initialize(DialogueGraph graph) { }
    public void OnNodeBegin() { }
    public void ResetAll() => Cancel();

    public void Cancel()
    {
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool TryFastForward() => false;

    public async Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (AudioModule)module;

        if (audioSource == null || m.clip == null) return;

        audioSource.volume = m.volume;
        audioSource.PlayOneShot(m.clip);

        if (m.waitForCompletion)
        {
            float elapsed = 0f;
            float duration = m.clip.length;

            while (elapsed < duration && !ctx.Token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }
    }
}
