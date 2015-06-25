using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PolygonUtils
{

	public const int SPLITS = 2;

	public static Vector2[][] Split (Plane split, Vector2[] path, Vector2 debug)
	{
		List<Vector2> negative = new List<Vector2> ();
		List<Vector2> positive = new List<Vector2> ();
		int length = path.Length;

		//Set previous point in path
		Vector2 previous = path [length - 1];
		for (int i = 0; i < length; ++i) {

			//Set current point in path
			Vector2 current = path [i];

			bool sideCurrent = split.GetSide(current);
			bool sidePrevious = split.GetSide(previous);
	
			if (sideCurrent != sidePrevious) {

				//Create a ray from the two points on different sides of the plane
				Vector2 direction = current - previous;
				Ray ray = new Ray (current, direction);

				//See where the ray intersects the plane
				float distance;
				split.Raycast (ray, out distance);

				//Collect the point and add it to both shapes
				Vector2 point = ray.GetPoint (distance);
				
				negative.Add (point);
				positive.Add (point); 

			}

			if (sideCurrent) {
				positive.Add (current);
			} else {
				negative.Add (current);
			}

			previous = current;
		}

		Vector2[][] splits = new Vector2[SPLITS][];
		splits [0] = positive.ToArray ();
		splits [1] = negative.ToArray ();

		return splits;
	}

	public static Vector2 GetNormal (Vector2 start, Vector2 end)
	{
		float dx = end.x - start.x;
		float dy = end.y - start.y;

		return new Vector2 (-dy, dx);
	}
}
