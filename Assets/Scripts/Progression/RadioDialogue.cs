using Jamlloween.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Diálogo do rádio: fila de falas com legenda e áudio opcional, disparadas pelo diretor.
    /// Separado do minigame do rádio. As falas tocam sozinhas, uma depois da outra.
    /// </summary>
    public class RadioDialogue : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [Tooltip("Tempo de leitura por caractere quando a fala não tem áudio.")]
        [SerializeField] float secondsPerCharacter = 0.085f;
        [Tooltip("Tempo fixo somado a toda fala sem áudio, para o jogador achar a legenda antes de ler.")]
        [SerializeField] float baseSeconds = 1.5f;
        [SerializeField] float minSeconds = 4f;
        [Tooltip("Pausa entre uma fala e a próxima.")]
        [SerializeField] float gapSeconds = 0.5f;

        [Header("Rádio: chiado e voz (falas com personagem; pensamentos do jogador ficam sem)")]
        [Tooltip("Chiado em loop: forte na abertura e no fim da fala, baixinho por baixo da voz.")]
        [SerializeField] AudioClip staticLoop;
        [Tooltip("Murmúrio do patrão em loop enquanto a legenda está na tela (começa num ponto aleatório a cada fala).")]
        [SerializeField] AudioClip bossVoice;
        [Tooltip("Clique do botão de falar, no começo e no fim da fala.")]
        [SerializeField] AudioClip keyClick;
        [SerializeField, Range(0f, 1f)] float staticVolume = 0.55f;
        [Tooltip("Chiado de fundo enquanto o patrão fala.")]
        [SerializeField, Range(0f, 1f)] float staticBedVolume = 0.12f;
        [SerializeField, Range(0f, 1f)] float voiceVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] float clickVolume = 0.6f;
        [Tooltip("Segundos de chiado antes da voz entrar.")]
        [SerializeField, Min(0f)] float burstSeconds = 0.35f;
        [Tooltip("Segundos de chiado depois da voz sair, no fim da fala.")]
        [SerializeField, Min(0f)] float tailSeconds = 0.3f;
        [SerializeField, Min(0.01f)] float voiceFadeSeconds = 0.15f;

        readonly Queue<RadioLine> queue = new Queue<RadioLine>();
        RadioLine playing;
        float remaining, gap;
        AudioSource staticSource, voiceSource;
        bool radioFx, murmur;
        float lineElapsed, lineTotal;

        public bool IsBusy => playing != null || queue.Count > 0;
        /// <summary>Disparado quando a última fala termina e a fila fica vazia.</summary>
        public event Action QueueEmptied;

        void Awake()
        {
            if (staticLoop == null) throw new MissingReferenceException(nameof(staticLoop));
            if (bossVoice == null) throw new MissingReferenceException(nameof(bossVoice));
            if (keyClick == null) throw new MissingReferenceException(nameof(keyClick));
            staticSource = MakeLoop(staticLoop);
            voiceSource = MakeLoop(bossVoice);
            if (source != null) return;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        AudioSource MakeLoop(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.loop = true;
            s.clip = clip;
            s.volume = 0f;
            return s;
        }

        public void Enqueue(RadioLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.text) && line.audio == null) return;
            queue.Enqueue(line);
        }

        public void Clear()
        {
            queue.Clear();
            playing = null;
            gap = 0f;
            if (source != null) source.Stop();
            StopRadioFx(false);
        }

        void Update()
        {
            if (playing != null)
            {
                remaining -= Time.deltaTime;
                UpdateRadioFx();
                if (remaining > 0f) return;
                StopRadioFx(true);
                playing = null;
                if (queue.Count == 0) QueueEmptied?.Invoke();
                else gap = gapSeconds;
                return;
            }

            if (queue.Count == 0) return;
            if (gap > 0f) { gap -= Time.deltaTime; return; }

            playing = queue.Dequeue();
            if (playing.audio != null)
            {
                source.PlayOneShot(playing.audio, playing.volume);
                remaining = playing.audio.length;
            }
            else if (playing.seconds > 0f) remaining = playing.seconds;
            else remaining = Mathf.Max(minSeconds, baseSeconds + (playing.text?.Length ?? 0) * secondsPerCharacter);
            StartRadioFx();
        }

        // ------------------------------------------------------------------ chiado e voz

        /// <summary>A line with a speaker comes over the radio: click, static, the boss's murmur, static, click.</summary>
        void StartRadioFx()
        {
            radioFx = !string.IsNullOrEmpty(playing.speaker);
            if (!radioFx) return;
            murmur = playing.audio == null;   // a line with its own recording doesn't need the murmur
            lineElapsed = 0f;
            lineTotal = remaining;
            source.PlayOneShot(keyClick, clickVolume);
            staticSource.time = UnityEngine.Random.Range(0f, staticLoop.length * 0.9f);
            staticSource.volume = staticVolume;
            staticSource.Play();
            if (!murmur) return;
            voiceSource.time = UnityEngine.Random.Range(0f, bossVoice.length * 0.9f);
            voiceSource.volume = 0f;
            voiceSource.Play();
        }

        void UpdateRadioFx()
        {
            if (!radioFx) return;
            lineElapsed += Time.deltaTime;
            float t = lineElapsed, end = lineTotal - tailSeconds;
            staticSource.volume = t < burstSeconds ? staticVolume : t > end ? staticVolume * 0.9f : staticBedVolume;
            if (!murmur) return;
            float fadeIn = Mathf.Clamp01((t - burstSeconds) / voiceFadeSeconds);
            float fadeOut = Mathf.Clamp01((end - t) / voiceFadeSeconds);
            voiceSource.volume = voiceVolume * fadeIn * fadeOut;
        }

        void StopRadioFx(bool click)
        {
            if (!radioFx) return;
            radioFx = false;
            staticSource.Stop();
            voiceSource.Stop();
            if (click) source.PlayOneShot(keyClick, clickVolume);
        }

        void OnGUI()
        {
            if (playing == null || string.IsNullOrEmpty(playing.text)) return;
            float w = Screen.width, h = Screen.height;
            string text = string.IsNullOrEmpty(playing.speaker) ? playing.text : $"<b>{playing.speaker}:</b> {playing.text}";
            // A caixa cresce para cima com o texto: a base fica fixa e falas longas não vazam do fundo.
            float width = w * 0.7f;
            float minHeight = 70 * UIStyle.Scale;
            float height = Mathf.Max(minHeight, UIStyle.Center.CalcHeight(new GUIContent(text), width) + 8 * UIStyle.Scale);
            float bottom = h * 0.80f + minHeight;
            var r = new Rect(w * 0.15f, bottom - height, width, height);
            UIStyle.Fill(new Rect(r.x - 10, r.y - 4, r.width + 20, r.height + 8), new Color(0f, 0f, 0f, 0.55f));
            UIStyle.OsdText(r, text, UIStyle.Center);
        }
    }
}
