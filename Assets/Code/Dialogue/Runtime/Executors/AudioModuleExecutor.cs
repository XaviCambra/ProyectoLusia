using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="AudioModule"/>.
/// Reproduce un AudioClip a través de un AudioSource gestionado internamente.
/// Si <see cref="AudioModule.waitForCompletion"/> está activo, la Task espera a que el clip termine.
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private AudioSource audioSource;

    public override Type ModuleType => typeof(AudioModule);

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

    }

    public override void Cancel()
    {
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();
    }

    public override void ResetAll() => Cancel();

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (AudioModule)module;

        if (audioSource == null || m.clip == null) return null;

        audioSource.volume = m.volume;
        audioSource.PlayOneShot(m.clip);

        if (m.waitForCompletion)
        {
            float elapsed  = 0f;
            float duration = m.clip.length;

            while (elapsed < duration && !ctx.Token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

        return null;
    }
}
