using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayPanel : MonoBehaviour
{
    [SerializeField] private StandardSlider playerHealthBar;
    [SerializeField] private StandardSlider playerVitalityBar;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private SmallPopup interactInfo;
    [SerializeField] private SmallPopup popupInfo;
    [SerializeField] private TextMeshProUGUI goldText;

    public void InitPanel()
    {
        var instances = GetComponentsInChildren<IDefaultUi>();
        foreach (var _ in instances) _.InitUi();
    }
    // private void Start()
    // {
    //     interactInfo.
    // }

    public void UpdateUi()
    {
        if (PlayerHealth.Instance is not null)
        {
            playerHealthBar.UpdateValue(PlayerHealth.Instance.CurHP, PlayerHealth.Instance.MaxHP);
            playerVitalityBar.UpdateValue(PlayerHealth.Instance.CurVitality, PlayerHealth.Instance.MaxVitality);
            goldText.text = PlayerHealth.Instance.Gold.ToString("F0") + "G";
        }
    }

    public void ToggleInventory() => inventoryManager.gameObject.SetActive(!inventoryManager.gameObject.activeSelf);
    public void GetItem2Inventory(ItemScriptableObject itemData) => inventoryManager.SetItem(itemData);

    public void ShowInteractInfo(string description) => interactInfo.EnableInfo(description);
    public void HideInteractInfo() => interactInfo.DisableInfo();
    public void PopupInfo(string description) => popupInfo.PopupInfo(description);
}
