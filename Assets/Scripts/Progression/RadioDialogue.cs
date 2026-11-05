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
        [SerializeField] float secondsPerCharacter = 0.055f;
        [SerializeField] float minSeconds = 2f;
        [Tooltip("Pausa entre uma fala e a próxima.")]
        [SerializeField] float gapSeconds = 0.5f;

        readonly Queue<RadioLine> queue = new Queue<RadioLine>();
        RadioLine playing;
        float remaining, gap;

        public bool IsBusy => playing != null || queue.Count > 0;
        /// <summary>Disparado quando a última fala termina e a fila fica vazia.</summary>
        public event Action QueueEmptied;

        void Awake()
        {
            if (source != null) return;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
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
        }

        void Update()
        {
            if (playing != null)
            {
                remaining -= Time.deltaTime;
                if (remaining > 0f) return;
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
            else remaining = Mathf.Max(minSeconds, (playing.text?.Length ?? 0) * secondsPerCharacter);
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
