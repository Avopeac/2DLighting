using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class Occluder : MonoBehaviour
{
    private List<GameObject> children;
    private new PolygonCollider2D collider;

    // Use this for initialization
    void Start()
    {

        children = new List<GameObject>();

        collider = GetComponent<PolygonCollider2D>();
        SplitConcaveColliders(collider, 0);
    }

    protected void SplitConcaveColliders(PolygonCollider2D collider, int pathIndex)
    {
        Vector2[] path = collider.GetPath(pathIndex);
        int[] conflicts = GetConflictingVertices(ref path);

        Debug.Log(conflicts.Length);

        if (conflicts.Length == 0)
            return;

        float x = path[(conflicts[0] + 1) % path.Length].x - path[conflicts[0]].x;
        float y = path[(conflicts[0] + 1) % path.Length].y - path[conflicts[0]].y;

        Vector2 normal = new Vector3(y, -x);

        Plane plane = new Plane(normal, path[conflicts[0]]);
        Vector2[][] splits = PolygonUtils.Split(plane, path);

        PolygonCollider2D c1 = CreateSubCollider(splits[0]);
        PolygonCollider2D c2 = CreateSubCollider(splits[1]);

        SplitConcaveColliders(c1, 0);
        SplitConcaveColliders(c2, 0);
    }

    protected PolygonCollider2D CreateSubCollider(Vector2[] path)
    {
        GameObject child = new GameObject();
        child.transform.parent = this.transform;
        PolygonCollider2D collider = child.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, path);
        children.Add(child);

        return collider;
    }

    void OnDisable()
    {
        foreach (GameObject child in children)
        {
            GameObject.Destroy(child);
        }

        children.Clear();
    }

    protected int[] GetConflictingVertices(ref Vector2[] path)
    {

        Vector3 cross = Vector3.zero;
        List<int> negative = new List<int>();
        List<int> positive = new List<int>();

        int length = path.Length;

        Vector3 a, b;
        for (int i = 0; i < length; ++i)
        {
            a = path[(i + 2) % length] - path[(i + 1) % length];
            b = path[(i + 1) % length] - path[i];

            cross = Vector3.Cross(a, b);

            if (cross.z < 0)
            {
                negative.Add(i);
            }
            else
            {
                positive.Add(i);
            }
        }

        return negative.Count > positive.Count ? positive.ToArray() : negative.ToArray();
    }
}
