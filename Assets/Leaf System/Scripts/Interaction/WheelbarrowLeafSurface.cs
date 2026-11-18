
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WheelbarrowLeafSurface : MonoBehaviour
{
    [Header("Source Mesh")]
    [Tooltip("The MeshFilter containing the original Blender leaf pile.")]
    public MeshFilter sourceMeshFilter;

    [Header("Appearance")]
    [Min(0.01f)]
    public float textureTiling = 3f;

    [Tooltip("Small vertical offset to avoid flickering.")]
    public float surfaceOffset = 0.002f;

    [Header("Debug")]
    public bool logWarnings = false;

    private MeshFilter surfaceFilter;
    private MeshRenderer surfaceRenderer;
    private Mesh surfaceMesh;

    private Mesh sourceMesh;
    private Transform sourceTransform;
    private Renderer sourceRenderer;

    private Vector3[] sourceVertices;
    private int[] sourceTriangles;

    private float currentFill = -1f;

    private readonly List<Vector3> intersections =
        new List<Vector3>();

    private readonly List<Vector3> outline =
        new List<Vector3>();

    private const float Epsilon = 0.0001f;

    private void Awake()
    {
        surfaceFilter = GetComponent<MeshFilter>();
        surfaceRenderer = GetComponent<MeshRenderer>();

        surfaceMesh = new Mesh();
        surfaceMesh.name = "Generated Leaf Pile Top";
        surfaceMesh.MarkDynamic();

        surfaceFilter.mesh = surfaceMesh;
        surfaceRenderer.enabled = false;

        if (sourceMeshFilter == null ||
            sourceMeshFilter.sharedMesh == null)
        {
            Debug.LogError(
                "WheelbarrowLeafSurface: Assign the original " +
                "LeafPile MeshFilter in the Inspector.",
                this
            );
            enabled = false;
            return;
        }

        sourceMesh = sourceMeshFilter.sharedMesh;
        sourceTransform = sourceMeshFilter.transform;
        sourceRenderer =
            sourceMeshFilter.GetComponent<Renderer>();

        if (!sourceMesh.isReadable)
        {
            Debug.LogError(
                "WheelbarrowLeafSurface: Enable Read/Write " +
                "on the imported LeafPile FBX.",
                this
            );
            enabled = false;
            return;
        }

        sourceVertices = sourceMesh.vertices;
        sourceTriangles = sourceMesh.triangles;
    }

    public void SetFill(float fill)
    {
        fill = Mathf.Clamp01(fill);

        if (!enabled || surfaceMesh == null)
            return;

        if (Mathf.Abs(fill - currentFill) < 0.0001f)
            return;

        currentFill = fill;

        // Empty: no pile.
        // Full: original Blender top is already present.
        if (fill <= Epsilon || fill >= 0.9999f)
        {
            surfaceRenderer.enabled = false;
            return;
        }

        GenerateSurface(fill);
    }

    private void GenerateSurface(float fill)
    {
        if (sourceRenderer == null)
            return;

        // Match the exact world-space height range
        // used by WheelbarrowLeafFill's shader.
        Bounds bounds = sourceRenderer.bounds;

        float sliceY = Mathf.Lerp(
            bounds.min.y,
            bounds.max.y,
            fill
        );

        intersections.Clear();

        // Find where each original triangle crosses
        // the horizontal fill plane.
        for (int i = 0; i < sourceTriangles.Length; i += 3)
        {
            Vector3 a = sourceTransform.TransformPoint(
                sourceVertices[sourceTriangles[i]]
            );

            Vector3 b = sourceTransform.TransformPoint(
                sourceVertices[sourceTriangles[i + 1]]
            );

            Vector3 c = sourceTransform.TransformPoint(
                sourceVertices[sourceTriangles[i + 2]]
            );

            AddIntersection(a, b, sliceY);
            AddIntersection(b, c, sliceY);
            AddIntersection(c, a, sliceY);
        }

        if (intersections.Count < 3)
        {
            surfaceRenderer.enabled = false;

            if (logWarnings)
                Debug.LogWarning(
                    "Leaf pile slice has fewer than 3 points.",
                    this
                );

            return;
        }

        BuildConvexOutline();

        if (outline.Count < 3)
        {
            surfaceRenderer.enabled = false;
            return;
        }

        BuildCapMesh(sliceY);
    }

    private void AddIntersection(
        Vector3 a,
        Vector3 b,
        float sliceY)
    {
        float da = a.y - sliceY;
        float db = b.y - sliceY;

        // Only consider edges that cross the plane.
        if ((da < -Epsilon && db < -Epsilon) ||
            (da > Epsilon && db > Epsilon))
            return;

        // Ignore edges lying entirely on the plane.
        if (Mathf.Abs(da) <= Epsilon &&
            Mathf.Abs(db) <= Epsilon)
            return;

        float denominator = b.y - a.y;

        if (Mathf.Abs(denominator) < Epsilon)
            return;

        float t = (sliceY - a.y) / denominator;

        if (t < 0f || t > 1f)
            return;

        Vector3 point = Vector3.Lerp(a, b, t);

        // Avoid duplicate points shared by triangles.
        foreach (Vector3 existing in intersections)
        {
            Vector2 difference = new Vector2(
                existing.x - point.x,
                existing.z - point.z
            );

            if (difference.sqrMagnitude <
                Epsilon * Epsilon)
                return;
        }

        intersections.Add(point);
    }

    private void BuildConvexOutline()
    {
        outline.Clear();

        // Sort points in world X/Z space.
        intersections.Sort((a, b) =>
        {
            int x = a.x.CompareTo(b.x);

            return x != 0
                ? x
                : a.z.CompareTo(b.z);
        });

        // Monotone-chain convex hull.
        List<Vector3> hull = new List<Vector3>();

        foreach (Vector3 point in intersections)
        {
            while (hull.Count >= 2 &&
                CrossXZ(
                    hull[hull.Count - 2],
                    hull[hull.Count - 1],
                    point
                ) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        int lowerCount = hull.Count;

        for (int i = intersections.Count - 2; i >= 0; i--)
        {
            Vector3 point = intersections[i];

            while (hull.Count > lowerCount &&
                CrossXZ(
                    hull[hull.Count - 2],
                    hull[hull.Count - 1],
                    point
                ) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        if (hull.Count > 1)
            hull.RemoveAt(hull.Count - 1);

        outline.AddRange(hull);
    }

    private static float CrossXZ(
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) -
               (b.z - a.z) * (c.x - a.x);
    }

    private void BuildCapMesh(float sliceY)
    {
        int count = outline.Count;

        Vector3 center = Vector3.zero;

        foreach (Vector3 point in outline)
            center += point;

        center /= count;
        center.y = sliceY + surfaceOffset;

        Vector3[] vertices = new Vector3[count + 1];
        Vector2[] uvs = new Vector2[count + 1];
        int[] triangles = new int[count * 3];

        // Convert world positions into the
        // generated surface object's local space.
        vertices[0] =
            transform.InverseTransformPoint(center);

        uvs[0] = new Vector2(
            center.x * textureTiling,
            center.z * textureTiling
        );

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPoint = outline[i];
            worldPoint.y = sliceY + surfaceOffset;

            vertices[i + 1] =
                transform.InverseTransformPoint(worldPoint);

            uvs[i + 1] = new Vector2(
                worldPoint.x * textureTiling,
                worldPoint.z * textureTiling
            );
        }

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            // Upward-facing triangles.
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        surfaceMesh.Clear();

        surfaceMesh.vertices = vertices;
        surfaceMesh.uv = uvs;
        surfaceMesh.triangles = triangles;

        surfaceMesh.RecalculateNormals();
        surfaceMesh.RecalculateBounds();

        surfaceRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (surfaceMesh != null)
            Destroy(surfaceMesh);
    }
}
