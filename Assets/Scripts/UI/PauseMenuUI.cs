using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;

namespace SkyHarvest.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;

        public void Initialize(GameObject panel) { _panel = panel; _panel.SetActive(false); }

        public void Open()
        {
            IsOpen = true;
            GameManager.Instance?.Pause();
            _panel?.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            GameManager.Instance?.Resume();
            _panel?.SetActive(false);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void OnSaveClicked()
        {
            SaveLoad.SaveManager.Instance?.Save();
        }

        public void OnSaveAndQuitClicked()
        {
            SaveLoad.SaveManager.Instance?.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

        public void OnResumeClicked() => Close();
    }
}
