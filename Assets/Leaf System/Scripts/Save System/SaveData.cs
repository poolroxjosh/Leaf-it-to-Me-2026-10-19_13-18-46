using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public float playerX;
    public float playerY;
    public float playerZ;

    public List<int> collectedLeafIds = new();
}