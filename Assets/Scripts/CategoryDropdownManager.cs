using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CategoryDropdownManager : MonoBehaviour
{
    [Header("API Config")]
    public string categoryApiUrl = "http://localhost:5035/api/ProductCategory";

    [Header("UI References")]
    public TMP_Dropdown categoryDropdown;
    public ProductManager productListManager; // Tham chi?u ??n script qu?n l? s?n ph?m

    // List n¨¤y d¨´ng ?? map t? Index c?a Dropdown sang ID th?t c?a Category
    private List<string> _categoryIds = new List<string>();

    void Start()
    {
        // 1. ??ng k? s? ki?n khi ng??i d¨´ng thay ??i l?a ch?n
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        // 2. T?i d? li?u
        StartCoroutine(FetchCategories());
    }

    IEnumerator FetchCategories()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(categoryApiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<CategoryResponse>(request.downloadHandler.text);
                if (response != null && response.data != null)
                {
                    PopulateDropdown(response.data);
                }
            }
            else
            {
                Debug.LogError("L?i l?y danh m?c: " + request.error);
            }
        }
    }

    void PopulateDropdown(List<CategoryItem> categories)
    {
        categoryDropdown.ClearOptions();
        _categoryIds.Clear();

        // --- OPTION 1: Th¨ºm l?a ch?n "T?t c?" ? ??u ---
        var options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("T?t c? s?n ph?m"));
        _categoryIds.Add(""); // ID r?ng ngh?a l¨¤ l?y t?t c?

        // --- OPTION 2: Th¨ºm c¨¢c category t? API ---
        foreach (var cat in categories)
        {
            options.Add(new TMP_Dropdown.OptionData(cat.name));
            _categoryIds.Add(cat.id); // L?u ID v¨¤o list song song
        }

        categoryDropdown.AddOptions(options);
    }

    // H¨¤m n¨¤y ch?y khi ng??i d¨´ng ch?n Dropdown
    public void OnCategoryChanged(int index)
    {
        // L?y ID t??ng ?ng v?i index ???c ch?n
        string selectedId = _categoryIds[index];

        Debug.Log($"?? ch?n: Index {index} - ID: {selectedId}");

        // G?i sang ProductManager ?? t?i l?i danh s¨¢ch s?n ph?m
        if (productListManager != null)
        {
            productListManager.FilterByCategory(selectedId);
        }
    }
}
