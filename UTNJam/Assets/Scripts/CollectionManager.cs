using UnityEngine;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance;

    private HashSet<string> unlockedItems = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnlockItem(string itemID)
    {
        if (!unlockedItems.Contains(itemID))
        {
            unlockedItems.Add(itemID);

            Debug.Log("Nuevo objeto desbloqueado: " + itemID);
        }
    }

    public bool IsUnlocked(string itemID)
    {
        return unlockedItems.Contains(itemID);
    }
}