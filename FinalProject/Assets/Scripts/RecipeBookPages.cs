using UnityEngine;
using TMPro;

[System.Serializable]
public class RecipePage
{
    [Tooltip("Title shown at the top of this recipe page.")]
    public string title;

    [TextArea(4, 12)]
    [Tooltip("Main body text for this recipe page.")]
    public string body;
}

[DisallowMultipleComponent]
public class RecipeBookPages : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Text element that displays the recipe title.")]
    public TextMeshProUGUI titleText;

    [Tooltip("Text element that displays the recipe body.")]
    public TextMeshProUGUI bodyText;

    [Tooltip("Text element that displays the current page index, for example '1 / 4'.")]
    public TextMeshProUGUI pageIndicatorText;

    [Header("Pages")]
    [Tooltip("List of pages that make up the recipe book.")]
    public RecipePage[] pages;

    private int _currentIndex;

    private void Awake()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[RecipeBookPages] No pages assigned. The book will display an empty state.");
        }

        if (titleText == null)
        {
            Debug.LogWarning("[RecipeBookPages] titleText reference is not assigned.");
        }

        if (bodyText == null)
        {
            Debug.LogWarning("[RecipeBookPages] bodyText reference is not assigned.");
        }

        if (pageIndicatorText == null)
        {
            Debug.LogWarning("[RecipeBookPages] pageIndicatorText reference is not assigned.");
        }
    }

    private void OnEnable()
    {
        // Reset to a valid page index whenever the book opens
        if (pages != null && pages.Length > 0)
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, pages.Length - 1);
        }
        else
        {
            _currentIndex = 0;
        }

        RefreshPage();
        Debug.Log($"[RecipeBookPages] Book opened. Current page index = {_currentIndex}.");
    }

    /// <summary>
    /// Advances to the next page, wrapping around at the end.
    /// Intended to be called by UI buttons.
    /// </summary>
    public void NextPage()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[RecipeBookPages] NextPage called but there are no pages configured.");
            return;
        }

        _currentIndex = (_currentIndex + 1) % pages.Length;
        Debug.Log($"[RecipeBookPages] NextPage. New index = {_currentIndex}.");
        RefreshPage();
    }

    /// <summary>
    /// Moves to the previous page, wrapping around to the end if needed.
    /// Intended to be called by UI buttons.
    /// </summary>
    public void PreviousPage()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[RecipeBookPages] PreviousPage called but there are no pages configured.");
            return;
        }

        _currentIndex = (_currentIndex - 1 + pages.Length) % pages.Length;
        Debug.Log($"[RecipeBookPages] PreviousPage. New index = {_currentIndex}.");
        RefreshPage();
    }

    /// <summary>
    /// Updates all UI text elements based on the current page index.
    /// Handles the empty state gracefully if there are no pages.
    /// </summary>
    private void RefreshPage()
    {
        if (pages == null || pages.Length == 0)
        {
            if (titleText != null) titleText.text = "No Recipes";
            if (bodyText != null) bodyText.text = string.Empty;
            if (pageIndicatorText != null) pageIndicatorText.text = string.Empty;

            Debug.Log("[RecipeBookPages] RefreshPage called with no pages. Showing empty state.");
            return;
        }

        // Clamp again in case pages changed at runtime
        _currentIndex = Mathf.Clamp(_currentIndex, 0, pages.Length - 1);
        RecipePage page = pages[_currentIndex];

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(page.title) ? "Untitled Recipe" : page.title;
        }

        if (bodyText != null)
        {
            bodyText.text = page.body ?? string.Empty;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{_currentIndex + 1} / {pages.Length}";
        }

        Debug.Log($"[RecipeBookPages] Showing page {_currentIndex + 1} of {pages.Length}: '{page.title}'.");
    }
}
