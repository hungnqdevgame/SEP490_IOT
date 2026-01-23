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
