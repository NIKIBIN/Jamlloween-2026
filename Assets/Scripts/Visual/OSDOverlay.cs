using System;
using UnityEngine;

namespace Circus
{
    /// <summary>VCR on-screen display: "PLAY ▶" top-left, running date/time bottom-right.</summary>
    public class OSDOverlay : MonoBehaviour
    {
        [SerializeField] int year = 1996, month = 10, day = 31;
        [SerializeField] int startHour = 23, startMinute = 47;
        [SerializeField] float playLabelSeconds = 4f;

        static readonly string[] Months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        DateTime start;

        void Awake() => start = new DateTime(year, month, day, startHour, startMinute, 0);

        void OnGUI()
        {
            if (GameState.I && GameState.I.IsGameOver) return;
            float s = UIStyle.Scale;
            float pad = 28 * s;
            var big = UIStyle.Big;
            var oldAnchor = big.alignment;

            // PLAY label: solid for the first seconds, then blinks slowly
            float tl = Time.timeSinceLevelLoad;
            bool showPlay = tl < playLabelSeconds || Mathf.Repeat(tl, 2f) < 1.4f;
            big.alignment = TextAnchor.UpperLeft;
            if (showPlay) UIStyle.OsdText(new Rect(pad, pad, 400 * s, 50 * s), "PLAY ▶", big);
            UIStyle.OsdText(new Rect(pad, pad + 46 * s, 400 * s, 30 * s), "SP", UIStyle.Label, 0.8f);

            var now = start.AddSeconds(Time.timeSinceLevelLoad);
            int h12 = now.Hour % 12 == 0 ? 12 : now.Hour % 12;
            string ampm = now.Hour < 12 ? "AM" : "PM";
            string date = $"{Months[now.Month - 1]}.{now.Day:00}.{now.Year}";
            string time = $"{h12,2}:{now.Minute:00}:{now.Second:00} {ampm}";
            big.alignment = TextAnchor.LowerRight;
            UIStyle.OsdText(new Rect(Screen.width - 700 * s - pad, Screen.height - 60 * s - pad, 700 * s, 60 * s), $"{date}  {time}", big, 0.95f);
            big.alignment = oldAnchor;
        }
    }
}
