using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Prancheta diegética. Fica escondida abaixo da visão; apertando a tecla (liga/desliga) ela sobe só um pouco (aparece a
    /// borda de cima) e o jogador olha para baixo para ler. Lista as tarefas que o diretor revelou: concluídas riscadas com [X],
    /// as atuais em destaque e as futuras esmaecidas. O modelo é feito de primitivas com o texto em TextMeshPro 3D.
    /// Ela acompanha a posição dos olhos e só o giro horizontal (yaw), então inclinar a cabeça para baixo a revela.
    /// </summary>
    public class Clipboard : MonoBehaviour
    {
        [SerializeField] Key key = Key.Tab;
        [Tooltip("Tecla de reserva. Também liga/desliga. Serve para saber se o Tab chega ao jogo no seu ambiente.")]
        [SerializeField] Key altKey = Key.C;
        [SerializeField] float raiseSeconds = 0.25f;

        [Header("Posição (por ângulo em relação à vista, só com o yaw)")]
        [Tooltip("Fração da prancheta visível no canto da tela olhando reto para a frente. O resto aparece ao olhar para baixo.")]
        [SerializeField, Range(0f, 1f)] float visibleFraction = 0.3f;
        [Tooltip("Onde fica o centro da prancheta na largura da tela: 0 = borda esquerda, 0.5 = centro.")]
        [SerializeField, Range(0f, 0.5f)] float anchorX = 0.2f;
        [Tooltip("Distância dos olhos até a prancheta (m).")]
        [SerializeField] float distance = 0.65f;

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
        bool open, wasDown;
        bool warnedNoKeyboard, reportedOpen;
        int shownVersion = -1;

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
            if (k == null)
            {
                if (!warnedNoKeyboard) { warnedNoKeyboard = true; Debug.LogWarning("[Clipboard] Keyboard.current é nulo: o jogo não está recebendo teclado.", this); }
            }
            else
            {
                // Borda própria (solta -> aperta). Não depende de wasPressedThisFrame.
                bool tab = k[key].isPressed, alt = k[altKey].isPressed;
                bool down = tab || alt;
                if (down && !wasDown)
                {
                    string which = tab ? key.ToString() : altKey.ToString();
                    if (playingMinigames > 0)
                        Debug.Log($"[Clipboard] {which} ignorado: dentro de um minigame ({playingMinigames}).", this);
                    else
                    {
                        open = !open;
                        Debug.Log($"[Clipboard] {(open ? "aberta" : "fechada")} ({which})", this);
                    }
                }
                wasDown = down;
            }
            raise = Mathf.MoveTowards(raise, open && playingMinigames == 0 ? 1f : 0f, Time.deltaTime / Mathf.Max(raiseSeconds, 0.01f));

            if (raise <= 0f) { reportedOpen = false; if (model.gameObject.activeSelf) model.gameObject.SetActive(false); return; }
            if (cam == null) cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Clipboard] sem Camera.main (tag MainCamera) para posicionar a prancheta.", this); return; }
            if (!model.gameObject.activeSelf) model.gameObject.SetActive(true);

            RefreshText();

            // Posição por ângulo: o canto de baixo da tela fica em vHalf abaixo do eixo da vista. A prancheta cobre
            // ±span em torno do seu centro; o centro é posto para mostrar só visibleFraction dela olhando reto.
            float vHalf = cam.fieldOfView * 0.5f;
            float spanY = Mathf.Atan2(H * 0.5f, distance) * Mathf.Rad2Deg;
            float raisedPitch = vHalf + spanY * (1f - 2f * visibleFraction);
            float hiddenPitch = vHalf + spanY + 3f;   // fora da tela, por baixo
            float pitch = Mathf.Lerp(hiddenPitch, raisedPitch, Mathf.SmoothStep(0f, 1f, raise));
            float side = Mathf.Atan((2f * anchorX - 1f) * Mathf.Tan(vHalf * Mathf.Deg2Rad) * cam.aspect) * Mathf.Rad2Deg;
            side = FitSide(side, raisedPitch, Mathf.Tan(vHalf * Mathf.Deg2Rad) * cam.aspect);

            var yaw = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            Vector3 eye = cam.transform.position;
            Vector3 pos = eye + yaw * (Quaternion.Euler(pitch, side, 0f) * Vector3.forward * distance);
            // A face (-Z) olha para os olhos, então a prancheta fica inclinada para quem está olhando de cima.
            model.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - eye, Vector3.up));
            if (!reportedOpen)
            {
                reportedOpen = true;
                Vector3 vp = cam.WorldToViewportPoint(pos);
                Debug.Log($"[Clipboard] modelo em {pos} | camera '{cam.name}' fov={cam.fieldOfView:F0} aspect={cam.aspect:F2} | viewport=({vp.x:F2},{vp.y:F2}) z={vp.z:F2}", this);
            }
        }

        /// <summary>Gira para a direita até a borda esquerda da prancheta entrar na tela (olhando reto). A perspectiva a alarga perto do canto.</summary>
        float FitSide(float side, float pitch, float tanHalfH)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 p = Quaternion.Euler(pitch, side, 0f) * Vector3.forward * distance;
                var rot = Quaternion.LookRotation(p, Vector3.up);
                float ndcX = 0f;
                foreach (float sy in new[] { -1f, 1f })   // as duas quinas da esquerda
                {
                    Vector3 corner = p + rot * new Vector3(-W * 0.5f, sy * H * 0.5f, 0f);
                    ndcX = Mathf.Min(ndcX, corner.x / Mathf.Max(corner.z, 0.01f) / tanHalfH);
                }
                if (ndcX >= -0.97f) break;
                side += 1.5f;
            }
            return side;
        }

        // ------------------------------------------------------------------ texto

        void RefreshText()
        {
            var d = ProgressionDirector.Instance;
            if (d == null || shownVersion == d.TasksVersion) return;
            shownVersion = d.TasksVersion;

            string faded = $"<alpha=#{futureAlpha:X2}>";
            var sb = new StringBuilder();
            sb.Append("<align=center><b>").Append(title).Append("</b></align>\n\n");
            for (int i = 0; i < d.Tasks.Count; i++)
            {
                var task = d.Tasks[i];
                var state = d.GetTaskState(i);
                if (state == TaskState.Hidden || string.IsNullOrEmpty(task.text)) continue;
                sb.Append(task.indent ? "<indent=9%>" : "<indent=0>");
                switch (state)
                {
                    case TaskState.Done:
                        sb.Append("<s>[X] ").Append(task.text).Append("</s>");
                        break;
                    case TaskState.Current:
                        sb.Append("<b><color=#").Append(ColorUtility.ToHtmlStringRGB(currentColor)).Append(">[  ] ").Append(task.text).Append("</color></b>");
                        break;
                    default:
                        sb.Append(faded).Append("[  ] ").Append(task.text).Append("<alpha=#FF>");
                        break;
                }
                sb.Append('\n');
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
