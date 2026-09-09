// "While you were away" panel — the first thing a returning player sees (spec 2026-09-09 §3).
// Pauses the clock while open so the report matches what's on the island.
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Sim;

namespace SkyHarvest.UI
{
    public class WelcomeBackUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private Text? _title;
        private Text? _body;

        public void Initialize(GameObject panel, Text title, Text body, Button collect)
        {
            _panel = panel; _title = title; _body = body;
            collect.onClick.AddListener(Close);
            _panel.SetActive(false);
        }

        public void Show(OfflineReport report)
        {
            if (_panel == null) return;
            if (_title != null) _title.text = $"Away {OfflineReport.FormatDuration(report.ElapsedSeconds)}";
            if (_body  != null) _body.text  = BuildBody(report);
            IsOpen = true;
            GameManager.Instance?.Pause();
            _panel.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _panel?.SetActive(false);
            GameManager.Instance?.Resume();
        }

        public static string BuildBody(OfflineReport r)
        {
            var sb = new StringBuilder();

            if (r.Harvested.Count == 0 && r.Replanted == 0 && r.BatchesCompleted == 0)
                sb.AppendLine("The island waited quietly.");

            foreach (var kv in r.Harvested)
            {
                string name = GameDatabase.GetItem(kv.Key)?.DisplayName ?? kv.Key;
                sb.AppendLine($"+{kv.Value} {name}");
            }
            if (r.Replanted > 0)        sb.AppendLine($"{r.Replanted} plots replanted");
            if (r.BatchesCompleted > 0) sb.AppendLine($"{r.BatchesCompleted} workshop batches ready");

            if (r.StorageFullAt >= 0f) sb.AppendLine($"Storage filled after {OfflineReport.FormatDuration(r.StorageFullAt)}");
            if (r.WaterEmptyAt  >= 0f) sb.AppendLine($"Water ran dry after {OfflineReport.FormatDuration(r.WaterEmptyAt)}");
            if (r.PowerEmptyAt  >= 0f) sb.AppendLine($"Power ran out after {OfflineReport.FormatDuration(r.PowerEmptyAt)}");
            if (r.CappedByOfflineLimit) sb.AppendLine($"(offline limit {OfflineReport.FormatDuration(Constants.OfflineCapSeconds)} reached)");

            return sb.ToString().TrimEnd();
        }
    }
}
