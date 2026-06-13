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

        private void Refresh()
        {
            if (_entryTexts == null) return;
            for (int i = 0; i < _entryTexts.Length && i < _defs.Count; i++)
            {
                string prefix = i == _selectedIndex ? "> " : "  ";
                _entryTexts[i].text = prefix + _defs[i].DisplayName;
            }
            for (int i = _defs.Count; i < _entryTexts.Length; i++)
                _entryTexts[i].text = "";

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
