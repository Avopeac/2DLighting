using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class Occluder : MonoBehaviour
{
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
		c++;

		if (c > 20)
			return;

        Vector2[] path = collider.GetPath(pathIndex);
        int[] conflicts = GetConflictingVertices(ref path);

		//Do not proceed if there's no concave shape
        if (conflicts.Length == 0)
            return;

		int current = conflicts [0];
		int next = (current + 1) % path.Length;

        float dx = path[next].x - path[current].x;
        float dy = path[next].y - path[current].y;
        Vector2 normal = new Vector3(-dy, dx);

        Plane plane = new Plane(normal, path[next]);
        Vector2[][] splits = PolygonUtils.Split(plane, path, position);

		//Do not proceed if the split didn't result in two valid colliders
		if (splits [0].Length < 3 || splits [1].Length < 3)
			return;

		Debug.Log ("SLOW" + splits[0].Length + " " + splits[1].Length);
	
		PolygonCollider2D c1 = CreateSubCollider(splits[0]);
        PolygonCollider2D c2 = CreateSubCollider(splits[1]);

		//Keep going when it works
        SplitConcaveColliders(c1, 0);
        SplitConcaveColliders(c2, 0);



    }

    protected PolygonCollider2D CreateSubCollider(Vector2[] path, string name = "Collider")
    {
        GameObject child = new GameObject();
        child.transform.parent = this.transform;
		child.transform.position = this.transform.position;
		child.name = name;
		children.Add(child);

        PolygonCollider2D collider = child.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, path);

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
