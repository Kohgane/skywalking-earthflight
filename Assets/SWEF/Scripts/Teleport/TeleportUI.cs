using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Teleport
{
    /// <summary>
    /// UI panel for place search + teleport.
    /// Wire in Inspector: search input, search button, result texts, teleport panel.
    /// </summary>
    public class TeleportUI : MonoBehaviour
    {
        [SerializeField] private TeleportController teleport;

        [Header("Search")]
        [SerializeField] private InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private Text statusText;

        [Header("Results (up to 5)")]
        [SerializeField] private Button[] resultButtons;
        [SerializeField] private Text[] resultTexts;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button toggleButton;

        private PlaceResult[] _lastResults;

        private void Awake()
        {
            if (teleport == null)
                teleport = FindFirstObjectByType<TeleportController>();

            if (searchButton != null)
                searchButton.onClick.AddListener(OnSearch);

            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);

            for (int i = 0; i < resultButtons.Length; i++)
            {
                int idx = i;
                if (resultButtons[i] != null)
                    resultButtons[i].onClick.AddListener(() => OnResultSelected(idx));
            }

            ClearResults();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (teleport != null)
            {
                teleport.OnSearchResults += HandleResults;
                teleport.OnSearchError += HandleError;
                teleport.OnTeleportStarted += OnTeleportStarted;
                teleport.OnTeleportCompleted += OnTeleportCompleted;
            }
        }

        private void OnDisable()
        {
            if (teleport != null)
            {
                teleport.OnSearchResults -= HandleResults;
                teleport.OnSearchError -= HandleError;
                teleport.OnTeleportStarted -= OnTeleportStarted;
                teleport.OnTeleportCompleted -= HandleTeleportCompleted;
            }
        }

        private void OnSearch()
        {
            if (teleport == null || searchInput == null) return;
            string query = searchInput.text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            if (statusText != null)
                statusText.text = "Searching...";
            ClearResults();
            teleport.Search(query);
        }

        private void HandleResults(PlaceResult[] results)
        {
            _lastResults = results;
            if (statusText != null)
                statusText.text = $"{results.Length} result(s)";

            for (int i = 0; i < resultButtons.Length; i++)
            {
                if (i < results.Length)
                {
                    if (resultButtons[i] != null) resultButtons[i].gameObject.SetActive(true);
                    if (i < resultTexts.Length && resultTexts[i] != null)
                        resultTexts[i].text = $"{results[i].name}\n{results[i].address}";
                }
                else
                {
                    if (resultButtons[i] != null) resultButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandleError(string error)
        {
            if (statusText != null)
                statusText.text = $"Error: {error}";
        }

        private void OnResultSelected(int index)
        {
            if (_lastResults == null || index >= _lastResults.Length) return;
            var place = _lastResults[index];
            teleport.TeleportTo(place.lat, place.lon, place.name);
        }

        private void OnTeleportStarted()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnTeleportCompleted()
        {
            if (statusText != null) statusText.text = "Teleport complete!";
        }

        private void TogglePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(!panelRoot.activeSelf);
        }

        private void ClearResults()
        {
            _lastResults = null;
            for (int i = 0; i < resultButtons.Length; i++)
            {
                if (resultButtons[i] != null) resultButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
