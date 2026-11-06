using System.Collections.Generic;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Core;
using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// The game's opening: a black screen where the story fades in one line at a time, block after block, then
    /// "press any key". The key fades the black away, brings the sound up and starts the story (ProgressionDirector.Begin,
    /// with the director's waitForBegin on). Until then the player can't move or interact and the world is silent.
    /// Drawn with IMGUI in front of the HUD, in the same VCR text style. A key while the text is still coming jumps to the end.
    /// </summary>
    public sealed class IntroScreen : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [SerializeField] LevelWalker walker;
        [SerializeField] PlayerInteractor interactor;

        [Header("Texto")]
        [Tooltip("Cada bloco aparece sozinho na tela; cada linha (separada por Enter) entra em fade, uma de cada vez. Vazio = sem texto: só a tela preta e o aviso de tecla.")]
        [SerializeField, TextArea(3, 8)] string[] blocks = new string[0];
        [SerializeField] string prompt = "Pressione qualquer tecla para continuar";

        [Header("Tempos (segundos)")]
        [SerializeField, Min(0f)] float startDelay = 1.2f;
        [Tooltip("Duração do fade de cada linha.")]
        [SerializeField, Min(0.05f)] float lineFadeIn = 1.4f;
        [Tooltip("Do começo de uma linha ao começo da próxima.")]
        [SerializeField, Min(0.05f)] float lineInterval = 2.2f;
        [Tooltip("Quanto o bloco fica inteiro na tela antes de sumir.")]
        [SerializeField, Min(0f)] float blockHold = 2.5f;
        [SerializeField, Min(0.05f)] float blockFadeOut = 1.2f;
        [Tooltip("Entre o fim da última linha e o aviso de tecla.")]
        [SerializeField, Min(0f)] float promptDelay = 0.8f;
        [Tooltip("O preto some aos poucos depois da tecla.")]
        [SerializeField, Min(0.05f)] float outroSeconds = 2.5f;
        [Tooltip("O som do mundo volta aos poucos depois da tecla.")]
        [SerializeField, Min(0.05f)] float audioFadeSeconds = 3f;

        struct Line { public int block; public string text; public float start; }

        readonly List<Line> lines = new List<Line>();
        float[] blockFadeStart;
        float clock, promptTime, outroStart = -1f;
        bool done;

        void Awake()
        {
            if (director == null) throw new MissingReferenceException(nameof(director));
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (interactor == null) throw new MissingReferenceException(nameof(interactor));

            BuildTimeline();
            walker.enabled = false;
            interactor.enabled = false;
            AudioListener.volume = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void BuildTimeline()
        {
            blockFadeStart = new float[blocks.Length];
            float t = startDelay;
            promptTime = startDelay;   // sem texto: só a tela preta e o aviso de tecla
            for (int b = 0; b < blocks.Length; b++)
            {
                string[] split = blocks[b].Split('\n');
                float last = t;
                foreach (string s in split)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    lines.Add(new Line { block = b, text = s.Trim(), start = t });
                    last = t;
                    t += lineInterval;
                }
                float shown = last + lineFadeIn;
                bool final = b == blocks.Length - 1;
                // The last block stays with the prompt; the others leave before the next one starts.
                blockFadeStart[b] = final ? float.PositiveInfinity : shown + blockHold;
                if (final) promptTime = shown + promptDelay;
                else t = blockFadeStart[b] + blockFadeOut + 0.5f;
            }
        }

        void Update()
        {
            if (done) return;
            clock += Time.unscaledDeltaTime;

            if (outroStart < 0f)
            {
                if (!AnyKeyPressed()) return;
                if (clock < promptTime) { SkipToPrompt(); return; }
                BeginOutro();
                return;
            }

            float k = Mathf.Clamp01((clock - outroStart) / audioFadeSeconds);
            AudioListener.volume = k;
            if (clock - outroStart >= Mathf.Max(outroSeconds, audioFadeSeconds))
            {
                AudioListener.volume = 1f;
                done = true;
                enabled = false;
            }
        }

        /// <summary>A key before the end: everything earlier is skipped and the last block shows whole, with the prompt.</summary>
        void SkipToPrompt()
        {
            int last = blocks.Length - 1;
            if (last < 0) { clock = promptTime; return; }
            float lastStart = float.NegativeInfinity;
            foreach (Line l in lines) if (l.block == last) lastStart = Mathf.Max(lastStart, l.start);
            float now = lastStart + lineFadeIn;
            for (int i = 0; i < lines.Count; i++)
            {
                Line l = lines[i];
                l.start = l.block == last ? now - lineFadeIn : float.NegativeInfinity;
                lines[i] = l;
            }
            for (int b = 0; b < last; b++) blockFadeStart[b] = float.NegativeInfinity;
            clock = now;
            promptTime = now;
        }

        void BeginOutro()
        {
            outroStart = clock;
            walker.enabled = true;
            interactor.enabled = true;
            director.Begin();
        }

        static bool AnyKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            Mouse mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;
            Gamepad pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }

        void OnGUI()
        {
            if (done) return;
            GUI.depth = -1000;   // in front of the HUD and the clipboard

            float w = Screen.width, h = Screen.height;
            float outro = outroStart < 0f ? 0f : clock - outroStart;
            float curtain = 1f - Mathf.SmoothStep(0f, 1f, outro / outroSeconds);
            float textOut = 1f - Mathf.Clamp01(outro / 0.8f);
            UIStyle.Fill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, curtain));

            GUIStyle style = UIStyle.Center;
            float width = w * 0.7f, spacing = 14f * UIStyle.Scale;
            int current = CurrentBlock();
            if (current >= 0)
            {
                float total = 0f;
                foreach (Line l in lines) if (l.block == current) total += style.CalcHeight(new GUIContent(l.text), width) + spacing;
                float y = h * 0.45f - total * 0.5f;
                float blockAlpha = 1f - Mathf.Clamp01((clock - blockFadeStart[current]) / blockFadeOut);
                foreach (Line l in lines)
                {
                    if (l.block != current) continue;
                    float lh = style.CalcHeight(new GUIContent(l.text), width);
                    float a = Mathf.SmoothStep(0f, 1f, (clock - l.start) / lineFadeIn) * blockAlpha * textOut;
                    if (a > 0.001f) UIStyle.OsdText(new Rect((w - width) * 0.5f, y, width, lh), l.text, style, a);
                    y += lh + spacing;
                }
            }

            if (clock >= promptTime)
            {
                float appear = Mathf.Clamp01((clock - promptTime) / 0.8f);
                float pulse = 0.7f + 0.3f * Mathf.Cos((clock - promptTime) * 2.4f);   // starts bright, never fades below 40%
                float a = appear * pulse * textOut;
                float ph = 40f * UIStyle.Scale;
                if (a > 0.001f) UIStyle.OsdText(new Rect(0f, h * 0.85f - ph * 0.5f, w, ph), prompt, UIStyle.Prompt, a);
            }
        }

        /// <summary>The block on screen now: the latest one whose first line has started.</summary>
        int CurrentBlock()
        {
            int current = -1;
            foreach (Line l in lines) if (clock >= l.start) current = Mathf.Max(current, l.block);
            return current;
        }
    }
}
