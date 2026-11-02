using UnityEngine;

namespace Circus
{
    /// <summary>Caught: a burst of static, then a VCR blue screen ("NO SIGNAL"), then a rewind before the restart.</summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] float staticTime = 0.7f;
        [SerializeField] float rewindAt = 2.2f;
        [SerializeField] Color vcrBlue = new Color(0.05f, 0.12f, 0.75f);
        [SerializeField] int noiseWidth = 160, noiseHeight = 90;

        Texture2D noise;
        Color32[] px;
        bool active;
        float startTime;

        void OnEnable() => EventBus.PlayerCaught += OnCaught;
        void OnDisable() => EventBus.PlayerCaught -= OnCaught;

        void OnCaught() { active = true; startTime = Time.unscaledTime; }

        void Update()
        {
            if (!active) return;
            float t = Time.unscaledTime - startTime;
            if (t < staticTime || t > rewindAt) RefreshNoise(t > rewindAt ? 0.35f : 1f);
            VHSGlobals.ExtraInterference = t < staticTime ? 1f : 0.5f;
        }

        void RefreshNoise(float strength)
        {
            if (noise == null)
            {
                noise = new Texture2D(noiseWidth, noiseHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                px = new Color32[noiseWidth * noiseHeight];
            }
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)(Random.value * 255 * strength);
                px[i] = new Color32(v, v, v, 255);
            }
            noise.SetPixels32(px);
            noise.Apply(false);
        }

        void OnGUI()
        {
            if (!active) return;
            if (noise == null) RefreshNoise(1f);
            GUI.depth = -50;
            float t = Time.unscaledTime - startTime;
            var full = new Rect(0, 0, Screen.width, Screen.height);
            if (t < staticTime)
            {
                GUI.DrawTexture(full, noise);
                return;
            }
            UIStyle.Fill(full, vcrBlue);
            if (t > rewindAt)
            {
                var old = GUI.color; GUI.color = new Color(1, 1, 1, 0.25f);
                GUI.DrawTexture(full, noise); GUI.color = old;
                UIStyle.OsdText(new Rect(40 * UIStyle.Scale, 30 * UIStyle.Scale, 600, 60), "◄◄ REW", UIStyle.Big);
            }
            UIStyle.OsdText(new Rect(0, Screen.height * 0.38f, Screen.width, 60 * UIStyle.Scale), "NO SIGNAL", UIStyle.Big);
            UIStyle.OsdText(new Rect(0, Screen.height * 0.50f, Screen.width, 40 * UIStyle.Scale), "— FITA CORTADA —", UIStyle.Center);
        }
    }
}
