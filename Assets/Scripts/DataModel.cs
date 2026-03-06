using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProductRoot
{
    public bool success;
    public string message;
    public ProductData data;
}

[Serializable]
public class ProductData
{
    public List<ProductItem> items;
    public int totalCount;
    public int pageNumber;
    public int pageSize;     // (Tùy chọn) Thêm cho đủ bộ với JSON
    public int totalPages;   // <- QUAN TRỌNG: Thêm dòng này để chốt trang cuối
}

[Serializable]
public class ProductItem
{
    public string id;
    public string name;
    public string sku;
    public double price;
    public string description;
    public string imageUrl;
    public string model3DUrl;
    public List<ColorItem> colors;

    public string productCategoryId;
    public string brand;
    public string material;
    public string ageRange;
}

[Serializable]
public class ColorItem
{
    public string id;
    public string sku;
    public string model3DUrl;
    public string imageUrl;
    public double price;
}

[Serializable]
public class CategoryResponse
{
    public bool success;
    public string message;
    public List<CategoryItem> data; // JSON trả về mảng [] trong field data
}

[Serializable]
public class CategoryItem
{
    public string id;
    public string code;
    public string name;
}

[System.Serializable]
public class ModelPlaylistItem
{
    public string modelUrl;       // Đường dẫn đến file robot hoặc .glb trên Render
    public string assetName;      // Tên Prefab (ví dụ: KyleRobot)
    public float displayDuration; // Thời gian hiển thị (giây)
}

// THÊM 2 CLASS NÀY ĐỂ ĐỌC API CATEGORY ĐƠN LẺ
[System.Serializable]
public class SingleCategoryResponse
{
    public bool success;
    public string message;
    public CategoryDetail data;
}

[System.Serializable]
public class CategoryDetail
{
    public string id;
    public string name;
    public string code;
    public string description;
}

[System.Serializable]
public class ProductColorApiResponse
{
    public bool success;
    public string message;
    public ProductColorData data;
}

[System.Serializable]
public class ProductColorData
{
    public string productId;
    public string productSku;
    public string productName;
    public double price;
    public string description;
    public string model3DUrl; // "robot"
    public string variantSku; // "S-000005-W"
    public string colorName;
}