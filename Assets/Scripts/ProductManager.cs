using UnityEngine;
using UnityEngine.Networking;


public class ProductManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    UnityWebRequest request = UnityWebRequest.Get("https://example.com/api/products");
    
    // Update is called once per frame
    void Update()
    {
        
    }
    
}
