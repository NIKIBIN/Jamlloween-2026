using System.Collections.Generic;
using Jamlloween.Flashlight;
using TMPro;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// The small HUD text in the bottom-left corner: "Pressione TAB para ver os objetivos", there once the player has the
    /// clipboard (hidden while the clipboard is up or in a minigame). When the tasks change it says so for a few seconds, and it teaches the controls
    /// once, when they become useful: moving at the start, F when the flashlight is picked up, G when the gas can is.
    /// The texts are TextMeshPro labels on the HUD canvas (its CanvasScaler keeps them right at any screen size), stacked
    /// by a VerticalLayoutGroup that grows upward from the corner. A hidden label leaves no gap.
    /// </summary>
    public sealed class ObjectivesHint : MonoBehaviour
    {
        [SerializeField] Clipboard clipboard;
        [Tooltip("Optional: shows the F tip when the player picks it up.")]
        [SerializeField] PlayerFlashlight flashlight;
        [Tooltip("Optional: shows the G tip when the player picks up the gas can.")]
        [SerializeField] GasCanCarrier gasCan;

        [Header("Labels (children of the HUD canvas)")]
        [Tooltip("Fades the whole hint out while the clipboard is up or in a minigame.")]
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text objectivesLabel;
        [SerializeField] TMP_Text updatedLabel;
        [SerializeField] TMP_Text tipLabel;

        [Header("Texts")]
        [SerializeField] string objectivesText = "Pressione {0} para ver os objetivos";
        [SerializeField] string updatedText = "Objetivos atualizados";
        [SerializeField] string startTip = "WASD anda   SHIFT corre   E usa";
        [SerializeField] string flashlightTip = "F liga e desliga a lanterna";
        [SerializeField] string gasCanTip = "G larga o galão";
        [Tooltip("Seconds each tip and the 'updated' notice stay on screen.")]
        [SerializeField, Min(1f)] float tipSeconds = 6f;
        [SerializeField, Range(0f, 1f)] float objectivesAlpha = 0.75f;

        readonly Queue<string> tips = new Queue<string>();
        float tipLeft, updatedLeft;
        int shownVersion = -1, minigames;
        bool gasTaught;

        void Awake()
        {
            if (clipboard == null) clipboard = FindFirstObjectByType<Clipboard>();
            if (clipboard == null) throw new MissingReferenceException(nameof(clipboard));
            if (group == null) throw new MissingReferenceException(nameof(group));
            if (objectivesLabel == null) throw new MissingReferenceException(nameof(objectivesLabel));
            if (updatedLabel == null) throw new MissingReferenceException(nameof(updatedLabel));
            if (tipLabel == null) throw new MissingReferenceException(nameof(tipLabel));
            objectivesLabel.text = string.Format(objectivesText, clipboard.OpenKey.ToString().ToUpperInvariant());
            updatedLabel.text = updatedText;
            if (!string.IsNullOrEmpty(startTip)) tips.Enqueue(startTip);
        }

        void OnEnable()
        {
            Minigame.AnyStarted += OnStarted;
            Minigame.AnyExited += OnExited;
            if (flashlight != null) flashlight.Acquired += OnFlashlight;
        }

        void OnDisable()
        {
            Minigame.AnyStarted -= OnStarted;
            Minigame.AnyExited -= OnExited;
            if (flashlight != null) flashlight.Acquired -= OnFlashlight;
        }

        void OnStarted(Minigame _) => minigames++;
        void OnExited(Minigame _) => minigames = Mathf.Max(0, minigames - 1);
        void OnFlashlight() { if (!string.IsNullOrEmpty(flashlightTip)) tips.Enqueue(flashlightTip); }

        void Update()
        {
            var d = ProgressionDirector.Instance;
            if (d != null && d.TasksVersion != shownVersion)
            {
                if (shownVersion >= 0) updatedLeft = tipSeconds;   // not for the first list, at the start
                shownVersion = d.TasksVersion;
            }
            if (clipboard.IsOpen) updatedLeft = 0f;                 // seen
            else if (updatedLeft > 0f) updatedLeft -= Time.deltaTime;

            if (!gasTaught && gasCan != null && gasCan.IsCarrying)
            {
                gasTaught = true;
                if (!string.IsNullOrEmpty(gasCanTip)) tips.Enqueue(gasCanTip);
            }

            if (tipLeft > 0f) tipLeft -= Time.deltaTime;
            else if (tips.Count > 0) { tipLabel.text = tips.Dequeue(); tipLeft = tipSeconds; }

            group.alpha = clipboard.IsOpen || minigames > 0 ? 0f : 1f;
            // Sem a prancheta (ainda na mesa do trailer), a tecla não faz nada: o aviso só aparece depois de pegá-la.
            if (objectivesLabel.gameObject.activeSelf != clipboard.IsOwned) objectivesLabel.gameObject.SetActive(clipboard.IsOwned);
            objectivesLabel.alpha = objectivesAlpha;
            Show(updatedLabel, updatedLeft > 0f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 4f)));
            Show(tipLabel, tipLeft > 0f, Mathf.Clamp01(tipLeft));
        }

        static void Show(TMP_Text label, bool on, float alpha)
        {
            if (label.gameObject.activeSelf != on) label.gameObject.SetActive(on);
            if (on) label.alpha = alpha;
        }
    }
}
