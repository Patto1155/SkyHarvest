using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SkyHarvest.Core;
using SkyHarvest.Workshop;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class WorkshopUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private WorkshopBase? _currentWorkshop;
        private PlayerInventoryComponent? _playerInv;

        private Text? _titleText;
        private Text? _progressText;
        private Slider? _progressBar;
        private Button? _startButton;
        private Button? _collectButton;
        private Text? _recipeListText;
        private List<RecipeDef> _recipes = new();
        private int _selectedRecipeIndex;

        public void Initialize(GameObject panel, PlayerInventoryComponent inv)
        {
            _panel = panel;
            _playerInv = inv;
            _panel.SetActive(false);
        }

        public void SetWidgets(Text title, Text progress, Slider bar, Button start, Button collect, Text recipeList)
        {
            _titleText     = title;
            _progressText  = progress;
            _progressBar   = bar;
            _startButton   = start;
            _collectButton = collect;
            _recipeListText = recipeList;
            _startButton?.onClick.AddListener(OnStartClicked);
            _collectButton?.onClick.AddListener(OnCollectClicked);
        }

        public void Open(WorkshopBase workshop)
        {
            _currentWorkshop = workshop;
            IsOpen = true;
            _panel?.SetActive(true);
            _recipes = new List<RecipeDef>(GameDatabase.GetRecipesFor(workshop.GetWorkshopType()));
            _selectedRecipeIndex = 0;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            _currentWorkshop = null;
            _panel?.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || _currentWorkshop == null) return;
            if (_progressBar != null)
                _progressBar.value = _currentWorkshop.Progress;
            if (_progressText != null)
                _progressText.text = _currentWorkshop.IsProcessing
                    ? $"{_currentWorkshop.Progress * 100f:F0}%"
                    : _currentWorkshop.IsComplete ? "Done!" : "Idle";
            if (_collectButton != null)
                _collectButton.interactable = _currentWorkshop.IsComplete;
            if (_startButton != null)
                _startButton.interactable = !_currentWorkshop.IsProcessing;
        }

        private void Refresh()
        {
            if (_currentWorkshop == null) return;
            if (_titleText != null) _titleText.text = _currentWorkshop.gameObject.name;

            if (_recipeListText != null && _recipes.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _recipes.Count; i++)
                {
                    string prefix = i == _selectedRecipeIndex ? "> " : "  ";
                    sb.AppendLine($"{prefix}{_recipes[i].DisplayName}");
                }
                _recipeListText.text = sb.ToString();
            }
        }

        private void OnStartClicked()
        {
            if (_currentWorkshop == null || _playerInv == null) return;
            if (_selectedRecipeIndex >= _recipes.Count) return;
            _currentWorkshop.StartRecipe(_recipes[_selectedRecipeIndex], _playerInv.Inventory);
        }

        private void OnCollectClicked()
        {
            if (_currentWorkshop == null || _playerInv == null) return;
            _currentWorkshop.CollectOutput(_playerInv.Inventory);
        }

        private void OnGUI() { } // reserve for key-navigation within panel
    }
}
