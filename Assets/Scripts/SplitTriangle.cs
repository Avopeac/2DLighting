using UnityEngine;
using System.Collections;

public class SplitTriangle : MonoBehaviour {

	public GameObject p1;
	public GameObject p2;

	void OnGUI()
	{
		if (GUI.Button(Rect.MinMaxRect(0,0,100,100), "Split"))
		{

			Plane plane = GetPlane();
			Vector2[][] splits = PolygonUtils.Split(plane, GetComponent<PolygonCollider2D>().GetPath(0), Vector2.zero);

			Debug.Log(splits[0].Length);
			Debug.Log(splits[1].Length);

			CreateSubCollider(splits[0]);
			CreateSubCollider(splits[1]);
		}
	}

	protected PolygonCollider2D CreateSubCollider(Vector2[] path, string name = "Collider")
	{
		GameObject child = new GameObject();
		child.transform.parent = this.transform;
		child.transform.position = this.transform.position;
		child.name = name;
		
		PolygonCollider2D collider = child.AddComponent<PolygonCollider2D>();
		collider.SetPath(0, path);
		
		return collider;
	}

	Plane GetPlane()
	{
		Vector2 normal = PolygonUtils.GetNormal (p1.transform.position, p2.transform.position);
		return new Plane (normal, p1.transform.position);
	}
}
