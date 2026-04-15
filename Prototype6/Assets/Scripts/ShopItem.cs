using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public AugmentData augment;
    public int price;
    [HideInInspector] public bool purchased;   // set at runtime from PlayerPrefs
}
