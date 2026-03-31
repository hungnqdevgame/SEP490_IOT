using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] 
    private GameObject colorPanel;
    [SerializeField]
    private GameObject informationPanel;
    [SerializeField]
    private Button colorButton;
    [SerializeField]
    private Button informationButton;
    [SerializeField]
    private Button showButton;
    [SerializeField]
    private TextMeshProUGUI showText;
    [SerializeField]
    bool isHide = false;

    void Start()
    {
        colorPanel.SetActive(false);
        informationPanel.SetActive(true);
        informationButton.gameObject.SetActive(true);
        colorButton.gameObject.SetActive(true);
        
        informationButton.Select();
        showText.text = "Ẩn";
        isHide = false;
    }

    public void ShowColorPanel()
    {
        colorPanel.SetActive(true);
        informationPanel.SetActive(false);
      
    }

    public void ShowInformationPanel()
    {
        colorPanel.SetActive(false);
        informationPanel.SetActive(true);
    }

    public void HidePanels()
    {
        colorPanel.SetActive(false);
        if (isHide == false)
        {         
            informationButton.gameObject.SetActive(false);
            colorButton.gameObject.SetActive(false);
            informationPanel.SetActive(false);
            showText.text = "Hiện";
            isHide = true;
        }
        else 
        {
            informationPanel.SetActive(true);
            informationButton.gameObject.SetActive(true);
            colorButton.gameObject.SetActive(true);
            informationButton.Select();
            showText.text = "Ẩn";
            isHide = false;
        }
    }


}
