
using UnityEngine;

public enum LeafState
{
    Resting,
    Moving,
    Airborne,
    BeingCollected,
    Collected
}

[System.Serializable]
public struct LeafData
{
    public int id;
    public int variantIndex;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public Vector3 velocity;
    public Color color;

    public LeafState state;
}
