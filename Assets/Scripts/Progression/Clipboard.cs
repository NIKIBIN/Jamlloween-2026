using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Prancheta diegética. Fica escondida abaixo da visão; apertando a tecla (liga/desliga) ela sobe só um pouco (aparece a
    /// borda de cima) e o jogador olha para baixo para ler. Lista todos os objetivos: concluídos riscados com [X],
    /// o atual em destaque e os futuros esmaecidos. O modelo é feito de primitivas com o texto em TextMeshPro 3D.
    /// Ela acompanha a posição dos olhos e só o giro horizontal (yaw), então inclinar a cabeça para baixo a revela.
    /// </summary>
    public class Clipboard : MonoBehaviour
    {
        [SerializeField] Key key = Key.Tab;
        [SerializeField] float raiseSeconds = 0.25f;

        [Header("Posição (relativa aos olhos, girando só com o yaw)")]
        [Tooltip("Escondida: fora do campo de visão.")]
        [SerializeField] Vector3 hiddenOffset = new Vector3(-0.17f, -0.75f, 0.35f);
        [Tooltip("Levantada: só a borda de cima entra na visão. Suba o Y para ver mais sem olhar para baixo.")]
        [SerializeField] Vector3 raisedOffset = new Vector3(-0.17f, -0.27f, 0.42f);

        [Header("Materiais (opcionais; sem eles usa um Unlit simples)")]
        [SerializeField] Material boardMaterial;
        [SerializeField] Material paperMaterial;
        [SerializeField] Material clipMaterial;

        [Header("Texto")]
        [SerializeField] string title = "TAREFAS";
        [SerializeField] Color inkColor = new Color(0.13f, 0.11f, 0.09f);
        [SerializeField] Color currentColor = new Color(0.6f, 0.07f, 0.05f);
        [Tooltip("Opacidade (0 a 255, em hexa no TMP) dos objetivos futuros.")]
        [SerializeField, Range(0, 255)] int futureAlpha = 100;

        const float W = 0.26f, H = 0.34f;

        Transform model;
        TextMeshPro text;
        Camera cam;
        float raise;
        int playingMinigames;
        bool open;
        int shownIndex = -2;
        bool shownFinished;

        void Awake() => Build();

        void OnEnable()
        {
            Minigame.AnyStarted += OnMinigameStarted;
            Minigame.AnyExited += OnMinigameExited;
        }

        void OnDisable()
        {
            Minigame.AnyStarted -= OnMinigameStarted;
            Minigame.AnyExited -= OnMinigameExited;
        }

        void OnDestroy()
        {
            if (model != null) Destroy(model.gameObject);
        }

        void OnMinigameStarted(Minigame _) { playingMinigames++; open = false; }
        void OnMinigameExited(Minigame _) => playingMinigames = Mathf.Max(0, playingMinigames - 1);

        void LateUpdate()
        {
            if (model == null) Build();   // o modelo some se a cena recarrega ou se o Awake não rodou
            var k = Keyboard.current;
            if (k != null && playingMinigames == 0 && k[key].wasPressedThisFrame) open = !open;
            raise = Mathf.MoveTowards(raise, open && playingMinigames == 0 ? 1f : 0f, Time.deltaTime / Mathf.Max(raiseSeconds, 0.01f));

            if (raise <= 0f) { if (model.gameObject.activeSelf) model.gameObject.SetActive(false); return; }
            if (cam == null) cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Clipboard] sem Camera.main (tag MainCamera) para posicionar a prancheta.", this); return; }
            if (!model.gameObject.activeSelf) model.gameObject.SetActive(true);

            RefreshText();

            var yaw = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            Vector3 eye = cam.transform.position;
            Vector3 offset = Vector3.Lerp(hiddenOffset, raisedOffset, Mathf.SmoothStep(0f, 1f, raise));
            Vector3 pos = eye + yaw * offset;
            // A face (-Z) olha para os olhos, então a prancheta fica inclinada para quem está olhando de cima.
            model.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - eye, Vector3.up));
        }

        // ------------------------------------------------------------------ texto

        void RefreshText()
        {
            var d = ProgressionDirector.Instance;
            if (d == null) return;
            if (shownIndex == d.CurrentIndex && shownFinished == d.Finished) return;
            shownIndex = d.CurrentIndex;
            shownFinished = d.Finished;

            string faded = $"<alpha=#{futureAlpha:X2}>";
            var sb = new StringBuilder();
            sb.Append("<align=center><b>").Append(title).Append("</b></align>\n\n");
            for (int i = 0; i < d.Steps.Count; i++)
            {
                var step = d.Steps[i];
                if (step == null || string.IsNullOrEmpty(step.taskText)) continue;
                if (i < d.CurrentIndex || d.Finished)
                    sb.Append("<s>[X] ").Append(step.taskText).Append("</s>\n");
                else if (i == d.CurrentIndex)
                    sb.Append("<b><color=#").Append(ColorUtility.ToHtmlStringRGB(currentColor)).Append(">[  ] ").Append(step.taskText).Append("</color></b>\n");
                else
                    sb.Append(faded).Append("[  ] ").Append(step.taskText).Append("<alpha=#FF>\n");
            }
            text.text = sb.ToString();
        }

        // ------------------------------------------------------------------ modelo

        void Build()
        {
            model = new GameObject("ClipboardModel").transform;
            model.gameObject.SetActive(false);

            Part("Board", new Vector3(0f, 0f, 0f), new Vector3(W, H, 0.012f), boardMaterial, new Color(0.30f, 0.19f, 0.10f));
            Part("Paper", new Vector3(0f, -0.01f, -0.0075f), new Vector3(W - 0.03f, H - 0.07f, 0.003f), paperMaterial, new Color(0.93f, 0.89f, 0.77f));
            Part("Clip", new Vector3(0f, H * 0.5f - 0.012f, -0.012f), new Vector3(0.09f, 0.035f, 0.016f), clipMaterial, new Color(0.62f, 0.62f, 0.58f));

            var go = new GameObject("Text", typeof(TextMeshPro));
            go.transform.SetParent(model, false);
            go.transform.localPosition = new Vector3(0f, -0.012f, -0.0105f);
            go.transform.localScale = Vector3.one * 0.001f;   // 1 unidade do retângulo = 1 mm
            text = go.GetComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2((W - 0.06f) * 1000f, (H - 0.1f) * 1000f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 300f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = inkColor;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
        }

        void Part(string name, Vector3 localPos, Vector3 scale, Material mat, Color fallback)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());   // não pode ser mirada nem barrar nada
            go.transform.SetParent(model, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sharedMaterial = mat != null ? mat : MakeUnlit(fallback);
        }

        static Material MakeUnlit(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
