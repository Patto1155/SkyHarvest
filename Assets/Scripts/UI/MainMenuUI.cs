using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;

namespace SkyHarvest.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private InputField? _seedInput;
        private Button? _continueButton;
        private System.Action? _onNewGame;
        private System.Action? _onContinue;

        public void Initialize(GameObject panel, InputField seedInput,
                               Button continueBtn, System.Action onNew, System.Action onContinue)
        {
            _panel          = panel;
            _seedInput      = seedInput;
            _continueButton = continueBtn;
            _onNewGame      = onNew;
            _onContinue     = onContinue;
        }

        public void Open()
        {
            IsOpen = true;
            _panel?.SetActive(true);
            if (_continueButton != null)
                _continueButton.interactable = SaveLoad.SaveManager.Instance?.HasSave() == true;
        }

        public void Close()
        {
            IsOpen = false;
            _panel?.SetActive(false);
        }

        public void OnNewGameClicked()
        {
            int seed = -1;
            if (_seedInput != null && !string.IsNullOrEmpty(_seedInput.text))
                int.TryParse(_seedInput.text, out seed);
            PlayerPrefs.SetInt("IslandSeed", seed);
            _onNewGame?.Invoke();
            Close();
        }

        public void OnContinueClicked()
        {
            _onContinue?.Invoke();
            Close();
        }
    }
}
