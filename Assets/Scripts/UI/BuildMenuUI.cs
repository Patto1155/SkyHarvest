using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SkyHarvest.Data;
using SkyHarvest.Building;

namespace SkyHarvest.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private BuildModeController? _buildCtrl;
        private List<StructureDef> _defs = new();
        private int _selectedIndex;
        private Text[]? _entryTexts;
        private Text? _costText;

        public void Initialize(GameObject panel, BuildModeController ctrl)
        {
            _panel     = panel;
            _buildCtrl = ctrl;
            _defs      = new List<StructureDef>(GameDatabase.AllStructures);
            _panel.SetActive(false);
        }

        public void SetDisplays(Text[] entries, Text costDisplay)
        {
            _entryTexts = entries;
            _costText   = costDisplay;
        }

        public void Open()
        {
            IsOpen = true;
            _panel?.SetActive(true);
            _selectedIndex = 0;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            _panel?.SetActive(false);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        private void Update()
        {
            if (!IsOpen) return;
            // Esc handling lives in Bootstrap.Update (centralized hotkeys).

            bool dirty = false;
            if (Input.GetKeyDown(KeyCode.UpArrow))   { _selectedIndex = (_selectedIndex - 1 + _defs.Count) % _defs.Count; dirty = true; }
            if (Input.GetKeyDown(KeyCode.DownArrow))  { _selectedIndex = (_selectedIndex + 1) % _defs.Count; dirty = true; }
            if (dirty) Refresh();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SelectCurrent();
            }
        }

        private void SelectCurrent()
        {
            if (_selectedIndex >= _defs.Count) return;
            _buildCtrl?.SetSelected(_defs[_selectedIndex]);
            Close();
        }

        /// <summary>First visible row: keeps the selection inside the fixed row window.</summary>
        public static int ScrollOffset(int selected, int total, int rows)
        {
            if (rows <= 0 || total <= rows) return 0;
            int maxOffset = total - rows;
            return System.Math.Clamp(selected - rows / 2, 0, maxOffset);
        }

        private void Refresh()
        {
            if (_entryTexts == null) return;
            int rows   = _entryTexts.Length;
            int offset = ScrollOffset(_selectedIndex, _defs.Count, rows);
            for (int i = 0; i < rows; i++)
            {
                int idx = offset + i;
                if (idx >= _defs.Count) { _entryTexts[i].text = ""; continue; }
                string prefix = idx == _selectedIndex ? "> " : "  ";
                string more = (i == 0 && offset > 0) ? " ▲" : (i == rows - 1 && offset + rows < _defs.Count) ? " ▼" : "";
                _entryTexts[i].text = prefix + _defs[idx].DisplayName + more;
            }

            if (_costText != null && _selectedIndex < _defs.Count)
            {
                var costs = _defs[_selectedIndex].BuildCosts;
                var sb = new System.Text.StringBuilder("Cost: ");
                if (costs != null)
                    foreach (var c in costs) sb.Append($"{c.Amount}× {c.ItemId}  ");
                _costText.text = sb.ToString().TrimEnd();
            }
        }
    }
}
