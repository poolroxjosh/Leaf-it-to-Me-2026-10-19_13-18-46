
using UnityEngine;

[System.Serializable]
public class LeafVariant
{
    public string name = "Maple Leaf";

    public GameObject prefab;

    [Min(0f)]
    public float spawnWeight = 1f;

    public Vector2 sizeMultiplier = new Vector2(0.8f, 1.2f);

    [Header("Mesh Orientation")]
    public Vector3 meshRotationOffset = Vector3.zero;

    public Gradient colors = new Gradient();
}
