using UnityEngine;

/// <summary>
/// Primitivo de esfregar. Dois usos:
///  - Limpar (seal desligado): uma camada de piche (decal) cuja opacidade cai conforme a distancia que a estopa
///    percorre sobre a area com o botao do mouse pressionado. A camada some quando a distancia e atingida.
///  - Vedar (seal ligado): um vazamento de gas. O decal e a fita, que aparece conforme a distancia percorrida
///    com o rolo de fita e fica no lugar depois de vedado.
/// Nao ha pintura por pixel: a camada inteira muda junto. Vai-e-vem conta como distancia.
///
/// Fica como filho direto do painel (a posicao local e o centro da area). Reutilizavel em qualquer
/// minigame: quem controla a ferramenta chama Contains() e Scrub().
/// </summary>
public class ZatanScrubArea : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Tooltip("Camada de piche (ou a fita, no modo vedar). Precisa de um material transparente com _BaseColor.")]
    [SerializeField] Renderer decal;
    [Tooltip("Modo vedar: a area e um vazamento de gas e o decal e a fita, que aparece conforme o jogador passa o rolo.")]
    [SerializeField] bool seal;
    [Tooltip("Opcional. Reflexo claro sobre o piche (da o aspecto brilhante). Acompanha a opacidade do piche.")]
    [SerializeField] Renderer shine;
    [Tooltip("Raio (m) da area em que a estopa esfrega, no plano do painel.")]
    [SerializeField] float radius = 0.1f;
    [Tooltip("Distancia (m) que a estopa precisa percorrer sobre a area para limpar tudo.")]
    [SerializeField] float requiredDistance = 1f;
    [Tooltip("Comeca coberta de piche. Desmarque para areas que so ficam sujas depois (juntas que vazam).")]
    [SerializeField] bool startActive = true;
    [Tooltip("Tempo (s) que o vazamento leva para crescer ate o tamanho total ao ser ativado.")]
    [SerializeField] float growSeconds = 0.9f;
    [Tooltip("Tamanho inicial do vazamento (fracao do tamanho final).")]
    [SerializeField, Range(0.05f, 1f)] float startScale = 0.3f;

    Material decalMat, shineMat;
    Color baseColor = new Color(0.03f, 0.02f, 0.02f, 1f);
    Vector3 decalScale = Vector3.one, shineScale = Vector3.one;
    float scrubbed, grow = 1f;

    public bool Active { get; private set; }
    public bool IsClean { get; private set; }
    /// <summary>0 = coberto, 1 = limpo.</summary>
    public float Progress => IsClean ? 1f : Mathf.Clamp01(scrubbed / Mathf.Max(requiredDistance, 0.001f));
    /// <summary>1 = piche inteiro, 0 = limpo. Util para atenuar o som do vazamento.</summary>
    public float Coverage => Active ? 1f - Progress : 0f;
    public float RequiredDistance { get => requiredDistance; set => requiredDistance = Mathf.Max(0.001f, value); }
    /// <summary>True: vazamento de gas, fechado com fita. False: piche, limpo com a estopa.</summary>
    public bool Seal => seal;

    void Awake()
    {
        if (decal != null)
        {
            decalScale = decal.transform.localScale;
            if (decal.sharedMaterial != null && decal.sharedMaterial.HasProperty(BaseColorId))
                baseColor = decal.sharedMaterial.GetColor(BaseColorId);
        }
        if (shine != null) shineScale = shine.transform.localScale;
        if (startActive) Activate(false); else Hide();
    }

    void Update()
    {
        if (!Active || IsClean || grow >= 1f) return;
        grow = Mathf.Min(1f, grow + Time.deltaTime / Mathf.Max(growSeconds, 0.01f));
        ApplyVisual();
    }

    /// <summary>Cobre a area de piche (de novo, se ja estava limpa). animate = o vazamento cresce aos poucos.</summary>
    public void Activate(bool animate)
    {
        Active = true;
        IsClean = false;
        scrubbed = 0f;
        grow = animate ? 0f : 1f;
        if (decal != null) decal.enabled = true;
        if (shine != null) shine.enabled = true;
        ApplyVisual();
    }

    void Hide()
    {
        Active = false;
        if (decal != null) decal.enabled = false;
        if (shine != null) shine.enabled = false;
    }

    /// <summary>True se o ponto (no espaco local do pai, onde a area vive) esta sobre a area.</summary>
    public bool Contains(Vector3 parentLocalPoint, float margin = 0f)
    {
        Vector3 d = parentLocalPoint - transform.localPosition;
        d.z = 0f;
        return d.magnitude <= radius + margin;
    }

    /// <summary>Soma distancia esfregada. Devolve true no frame em que a area fica limpa.</summary>
    public bool Scrub(float distance)
    {
        if (!Active || IsClean || distance <= 0f) return false;
        scrubbed += distance;
        if (scrubbed >= requiredDistance)
        {
            IsClean = true;
            if (seal) ApplyVisual();   // a fita fica no lugar, inteira
            else
            {
                if (decal != null) decal.enabled = false;
                if (shine != null) shine.enabled = false;
            }
            return true;
        }
        ApplyVisual();
        return false;
    }

    void ApplyVisual()
    {
        if (decal == null) return;
        // Instancias de material proprias: MaterialPropertyBlock faz o decal sumir com o GPU Resident Drawer.
        if (decalMat == null) decalMat = decal.material;
        if (shine != null && shineMat == null) shineMat = shine.material;

        if (seal)
        {
            // A fita aparece aos poucos e fica opaca no fim. Sem crescimento: o vazamento e o gas, nao a mancha.
            float k = Progress;
            var tape = baseColor;
            tape.a = baseColor.a * Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, k));
            decalMat.SetColor(BaseColorId, tape);
            decal.transform.localScale = new Vector3(decalScale.x, decalScale.y * Mathf.Lerp(0.35f, 1f, k), decalScale.z);
            if (shineMat != null) shineMat.SetColor(BaseColorId, new Color(1f, 1f, 1f, 0.35f * k));
            return;
        }

        // O piche segura grosso ate perto de 35% e so depois clareia.
        float t = Mathf.Clamp01((Progress - 0.35f) / 0.65f);
        float a = 1f - t * t * (3f - 2f * t);
        var c = baseColor;
        c.a = baseColor.a * a;
        decalMat.SetColor(BaseColorId, c);
        if (shineMat != null) shineMat.SetColor(BaseColorId, new Color(1f, 1f, 1f, 0.55f * a));
        float s = Mathf.Lerp(startScale, 1f, Mathf.SmoothStep(0f, 1f, grow));
        decal.transform.localScale = decalScale * s;
        if (shine != null) shine.transform.localScale = shineScale * s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
        Gizmos.DrawWireSphere(transform.localPosition, radius);
    }
}
