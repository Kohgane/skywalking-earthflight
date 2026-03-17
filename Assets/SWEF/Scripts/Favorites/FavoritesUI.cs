using UnityEngine;
using UnityEngine.UI;
using SWEF.Core;
using SWEF.Flight;
using SWEF.Teleport;

namespace SWEF.Favorites
{
    /// <summary>
    /// UI panel with paging for favorites list.
    /// Save current location, teleport to favorite, delete, prev/next page.
    /// </summary>
    public class FavoritesUI : MonoBehaviour
    {
        [SerializeField] private FavoriteManager favorites;
        [SerializeField] private TeleportController teleport;
        [SerializeField] private AltitudeController altitudeSource;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button saveButton;

        [Header("List (up to 5 per page)")]
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private Text[] itemTexts;
        [SerializeField] private Button[] deleteButtons;

        [Header("Paging")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageText;

        private const int ItemsPerPage = 5;
        private int _currentPage;

        private void Awake()
        {
            if (favorites == null) favorites = FindFirstObjectByType<FavoriteManager>();
            if (teleport == null) teleport = FindFirstObjectByType<TeleportController>();
            if (altitudeSource == null) altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (toggleButton != null) toggleButton.onClick.AddListener(TogglePanel);
            if (saveButton != null) saveButton.onClick.AddListener(SaveCurrent);
            if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
            if (nextButton != null) nextButton.onClick.AddListener(NextPage);

            for (int i = 0; i < itemButtons.Length; i++)
            {
                int idx = i;
                if (itemButtons[i] != null)
                    itemButtons[i].onClick.AddListener(() => OnItemSelected(idx));
            }

            for (int i = 0; i < deleteButtons.Length; i++)
            {
                int idx = i;
                if (deleteButtons[i] != null)
                    deleteButtons[i].onClick.AddListener(() => OnDeleteItem(idx));
            }

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (favorites != null)
                favorites.OnFavoritesChanged += RefreshList;
        }

        private void OnDisable()
        {
            if (favorites != null)
                favorites.OnFavoritesChanged -= RefreshList;
        }

        private void TogglePanel()
        {
            if (panelRoot == null) return;
            bool show = !panelRoot.activeSelf;
            panelRoot.SetActive(show);
            if (show)
            {
                _currentPage = 0;
                RefreshList();
            }
        }

        private void SaveCurrent()
        {
            if (favorites == null || !SWEFSession.HasFix) return;

            double lat = SWEFSession.Lat;
            double lon = SWEFSession.Lon;
            float alt = altitudeSource != null ? altitudeSource.CurrentAltitudeMeters : 0f;
            string name = $"Fav @ ({lat:F4}, {lon:F4})";

            favorites.Add(name, lat, lon, alt);
        }

        private void RefreshList()
        {
            if (favorites == null) return;

            var list = favorites.Favorites;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)list.Count / ItemsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            int startIdx = _currentPage * ItemsPerPage;

            for (int i = 0; i < ItemsPerPage; i++)
            {
                int dataIdx = startIdx + i;
                bool hasData = dataIdx < list.Count;

                if (i < itemButtons.Length && itemButtons[i] != null)
                    itemButtons[i].gameObject.SetActive(hasData);

                if (i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].gameObject.SetActive(hasData);

                if (hasData && i < itemTexts.Length && itemTexts[i] != null)
                {
                    var fav = list[dataIdx];
                    itemTexts[i].text = $"{fav.name}\n({fav.lat:F4}, {fav.lon:F4}) alt:{fav.altitude:F0}m";
                }
            }

            if (prevButton != null) prevButton.interactable = _currentPage > 0;
            if (nextButton != null) nextButton.interactable = _currentPage < totalPages - 1;
            if (pageText != null) pageText.text = $"{_currentPage + 1} / {totalPages}";
        }

        private void OnItemSelected(int slotIndex)
        {
            if (favorites == null || teleport == null) return;
            int dataIdx = _currentPage * ItemsPerPage + slotIndex;
            if (dataIdx >= favorites.Favorites.Count) return;

            var fav = favorites.Favorites[dataIdx];
            teleport.TeleportTo(fav.lat, fav.lon, fav.name);
        }

        private void OnDeleteItem(int slotIndex)
        {
            if (favorites == null) return;
            int dataIdx = _currentPage * ItemsPerPage + slotIndex;
            favorites.RemoveAt(dataIdx);
        }

        private void PrevPage()
        {
            _currentPage = Mathf.Max(0, _currentPage - 1);
            RefreshList();
        }

        private void NextPage()
        {
            _currentPage++;
            RefreshList();
        }
    }
}