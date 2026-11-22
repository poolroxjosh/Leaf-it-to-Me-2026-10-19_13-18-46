using UnityEngine;
using System.Collections.Generic;

public class GameSaveController : MonoBehaviour
{
    public Transform player;
    public LeafManager leafManager;

    private void Start()
    {
        Debug.Log("GameSaveController Start");
        leafManager.OnBulkSpawnCompleted += LoadGame;
    }

    public void SaveGame()
    {
        Debug.Log("Save Button Pressed");

        SaveData data = new SaveData();

        data.playerX = player.position.x;
        data.playerY = player.position.y;
        data.playerZ = player.position.z;

        for (int i = 0; i < leafManager.leaves.Count; i++)
        {
            if (leafManager.leaves[i].state ==
                LeafState.Collected)
            {
                data.collectedLeafIds.Add(i);
            }
        }

        SaveManager.Save(data);

        Debug.Log(
            $"Saved {data.collectedLeafIds.Count} collected leaves");
    }

    public void LoadGame()
    {
        SaveData data = SaveManager.Load();

        if (data == null)
            return;

        player.position = new Vector3(
            data.playerX,
            data.playerY,
            data.playerZ
        );

        HashSet<int> collectedIds =
            new HashSet<int>(data.collectedLeafIds);

        for (int i = 0; i < leafManager.leaves.Count; i++)
        {
            if (!collectedIds.Contains(i))
                continue;

            LeafData leaf = leafManager.leaves[i];

            leaf.state = LeafState.Collected;

            leafManager.leaves[i] = leaf;
        }
    }
}