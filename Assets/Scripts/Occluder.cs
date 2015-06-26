using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class Occluder : MonoBehaviour
{
    public const string SUB_COLLIDER_NAME = "Convex collider";

    private List<GameObject> children;
    private new PolygonCollider2D collider;
    private Vector2 position;

    private int c = 0;

    // Use this for initialization
    void Start()
    {
        children = new List<GameObject>();
        position = transform.position;

        collider = GetComponent<PolygonCollider2D>();
        SplitConcaveColliders(collider, 0);
    }

    protected void SplitConcaveColliders(PolygonCollider2D collider, int pathIndex)
    {

        //Probably a hole in the mesh
        if (collider.pathCount > 1)
            Debug.LogWarning("More than one path in collider.");

        //Get the conflicting vertex indices
        Vector2[] path = collider.GetPath(pathIndex);
        int[] conflicts = PolygonUtils.GetConcaveIndices(ref path);

        int current = 0;
        int next = 0;

        //Do not proceed if there's no concave shape
        if (conflicts.Length > 1)
        {
            current = conflicts[0];
            next = conflicts[1];
        }
        else if (conflicts.Length > 0)
        {
            current = conflicts[0];
            next = (conflicts[0] + 1) % path.Length;
        }
        else return;

        //Get normal to plane
        Vector2 normal = PolygonUtils.GetNormal(path[next], path[current]);

        //Offset the plane and split along it
        Plane plane = new Plane(normal, path[next]);
        PolygonUtils.SplitResult splits = PolygonUtils.Split(plane, ref path);

        //Do not proceed if the split didn't result in two valid colliders
        if (splits.First.Length < 3 || splits.Second.Length < 3)
            return;

        //Create new colliders with our results
        PolygonCollider2D c1 = CreateSubCollider(splits.First, SUB_COLLIDER_NAME + " " + c + " First");
        PolygonCollider2D c2 = CreateSubCollider(splits.Second, SUB_COLLIDER_NAME + " " + c + " Second");

        //Recursively create more if there are still concave shapes
        SplitConcaveColliders(c1, 0);
        SplitConcaveColliders(c2, 0);

        //Remove old concave colliders
        if (collider.transform.parent == this.transform)
            GameObject.Destroy(collider.gameObject);
        else if (collider.transform == this.transform)
            collider.enabled = false;
    }

    protected PolygonCollider2D CreateSubCollider(Vector2[] path, string name = "Collider")
    {
        //Create a new child with the same position
        GameObject child = new GameObject();
        child.transform.parent = this.transform;
        child.transform.position = this.transform.position;
        child.name = name;

        //Add a collider
        PolygonCollider2D collider = child.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, path);

        //Add to the list off sub-colliders
        children.Add(child);

        return collider;
    }

    void OnDisable()
    {
        //Destroy every sub-collider
        foreach (GameObject child in children)
        {
            GameObject.Destroy(child);
        }

        children.Clear();
    }
}
