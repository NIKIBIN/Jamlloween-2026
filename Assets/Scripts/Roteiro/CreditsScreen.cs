using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// PLACEHOLDER credits: a black screen with text that scrolls up, then "R: play again / Esc: quit".
    /// Hidden until Show (wired to the last step of the story). The text lives in the TMP object in the scene.
    /// </summary>
    public sealed class CreditsScreen : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [Tooltip("The block of text that scrolls (anchored at the bottom centre, pivot at its top).")]
        [SerializeField] RectTransform scroll;
        [SerializeField] GameObject endPrompt;
        [SerializeField] LevelWalker walker;
        [SerializeField] AudioSource music;
        [SerializeField] float scrollSpeed = 60f;

        bool showing, ended;
        float start;

        void Awake()
        {
            if (canvas == null) throw new MissingReferenceException(nameof(canvas));
            if (scroll == null) throw new MissingReferenceException(nameof(scroll));
            if (endPrompt == null) throw new MissingReferenceException(nameof(endPrompt));
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (music == null) throw new MissingReferenceException(nameof(music));
            canvas.enabled = false;
            endPrompt.SetActive(false);
        }

        public bool Showing => showing;

        public void Show()
        {
            if (showing) return;
            showing = true;
            walker.enabled = false;
            canvas.enabled = true;
            start = 0f;
            scroll.anchoredPosition = new Vector2(0f, start);
            music.loop = true;
            music.Play();
        }

        void Update()
        {
            if (!showing) return;
            // The text rises from below the screen and stops centred.
            var rect = ((RectTransform)canvas.transform).rect;
            float centred = rect.height * 0.5f + scroll.rect.height * 0.5f;
            if (!ended)
            {
                start += scrollSpeed * Time.unscaledDeltaTime;
                scroll.anchoredPosition = new Vector2(0f, Mathf.Min(start, centred));
                if (start >= centred) { ended = true; endPrompt.SetActive(true); }
            }

            var k = Keyboard.current;
            if (k == null) return;
            if (ended && k.rKey.wasPressedThisFrame) SceneManager.LoadScene(gameObject.scene.name);
            if (k.escapeKey.wasPressedThisFrame)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
