using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Static helper methods for manipulating polygonal shapes.
/// Author: Andreas Larsson
/// </summary>
public class PolygonUtils
{
    //Two points are considered the "same" if the distance is less than this threshold
    public const float SQR_DIST_THRESHOLD = 0.01f;

	//Three points are considered to be on the same line if the triangle area is less than this threshold
	public const float COLLINEARITY_THRESHOLD = 0.001f;
	
	/// <summary>
	/// A shape is determined by a path that is traced clockwise or counter-clockwise. This shape is split given some plane of which to split along.
	/// </summary>
	/// <param name="split">The splitting plane. </param>
	/// <param name="path">The path that determines the shape. </param>
	/// <returns></returns>
	public static SplitResult Split (Plane split, ref Vector2[] path)
	{
		List<Vector2> back = new List<Vector2>();
		List<Vector2> front = new List<Vector2>();
		
		int length = path.Length;
		
		//Set previous point in path
		Vector2 previous = path[length - 1];
		float prevDist = split.GetDistanceToPoint (previous);

		for (int i = 0; i < length; ++i) {
			Vector2 current = path[i];
			float currDist = split.GetDistanceToPoint(current);

			float pointDist;

			if (prevDist < 0) 
			{
				if(currDist < 0)
				{
					back.Add(current);
				} else {

					//Add intersection points
					Vector2 point = PlaneIntersection(split, previous, current, out pointDist);
					back.Add(point);
					front.Add(point);
					front.Add (current);
				}
			} else {
			
				if (currDist >= 0)
				{
					front.Add(current);
				}else {

					//Add intersection point
					Vector2 point = PlaneIntersection(split, previous, current, out pointDist);
					back.Add(point);
					back.Add(current);
					front.Add(point);
				}
			}

			previous = current; prevDist = currDist;

			RemoveDuplicates(ref front, SQR_DIST_THRESHOLD);
			RemoveDuplicates(ref back, SQR_DIST_THRESHOLD);
		}

		//RemoveZeroArea (ref front, COLLINEARITY_THRESHOLD);
		//RemoveZeroArea (ref back, COLLINEARITY_THRESHOLD);

		return new SplitResult(front.ToArray(), back.ToArray());
	}

    /// <summary>
    /// Remove duplicates in a list of points. Two points are considered the same if the distance between them is below some given threshold.
    /// </summary>
    /// <param name="path">A reference to the list of points. </param>
    /// <param name="threshold">Distance comparison. </param>
    private static void RemoveDuplicates(ref List<Vector2> path, float threshold)
    {
        if (path == null || path.Count == 0)
            return;

		//Loop the path
        int i = 1;
        while (i < path.Count)
        {
            int j = 0;
            bool remove = false;

            //Loop again until we find something to remove
            while (j < i && !remove)
            {
                Vector2 dist = path[i] - path[j];
                if (dist.sqrMagnitude < threshold)
				{
                    remove = true;
				}

                j++;
            }

            //Remove it, otherwise continue
            if (remove)
                path.RemoveAt(i);
            else
                i++;
        }
    }

    /// <summary>
    /// Gets the normal of the line between two points.
    /// </summary>
    /// <param name="start">The starting point. </param>
    /// <param name="end">The ending point. </param>
    /// <returns>The normal. </returns>
    public static Vector2 GetNormal(Vector2 start, Vector2 end)
    {
        float dx = end.x - start.x;
        float dy = end.y - start.y;

        Vector2 normal = new Vector2(-dy, dx);
        normal.Normalize();

        return normal;
    }

    /// <summary>
    /// The resulting paths from using the Split method are saved in this struct.
    /// </summary>
    public struct SplitResult
    {
        private Vector2[] split0, split1;

        public Vector2[] First
        {
            get { return this.split0; }
        }

        public Vector2[] Second
        {
            get { return this.split1; }
        }

        public SplitResult(Vector2[] split0, Vector2[] split1)
        {
            this.split0 = split0;
            this.split1 = split1;
        }

    }

    /// <summary>
    /// Given a path that determines a shape, returns the indices in the path that contribute to a concave shape.
    /// </summary>
    /// <param name="path">The path that determines the shape. </param>
    /// <returns>The indices in the path. </returns>
    public static int[] GetConcaveIndices(ref Vector2[] path, float collinearThreshold)
    {
		int length = path.Length;

        Vector3 cross = Vector3.zero;
        List<int> negative = new List<int>();
        List<int> positive = new List<int>();

        Vector3 ab, bc;
        for (int i = 0; i < length; ++i)
        {
            int vertIndex = (i + 1) % length;

			Vector3 a = path[(i + 2) % length];
			Vector3 b = path[vertIndex];
			Vector3 c = path[i];

			if (PolygonUtils.TriangleArea(a,b,c) < collinearThreshold)
				continue;

            //Two edges at a time
            ab = path[(i + 2) % length] - path[vertIndex];
            bc = path[vertIndex] - path[i];

            cross = Vector3.Cross(ab, bc);

            //Save the cross product between these edges
            if (cross.z < 0)
                negative.Add(vertIndex);
            else
                positive.Add(vertIndex);
        }

        //The one with least elements is the list with our conflicting points
        return negative.Count > positive.Count ? positive.ToArray() : negative.ToArray();
    }

	/// <summary>
	/// Returns a point where a line intersects the given plane.
	/// </summary>
	/// <returns>The intersection point.</returns>
	/// <param name="plane">Plane.</param>
	/// <param name="start">Start point.</param>
	/// <param name="end">End point.</param>
	public static Vector2 PlaneIntersection(Plane plane, Vector2 start, Vector2 end, out float distance)
	{
		//Create a new ray from start to end
		Ray ray = new Ray(start, end - start);

		//How far along the ray do we intersect the plane?
		plane.Raycast (ray, out distance);

		//Return that point
		return ray.GetPoint(distance);
	}

	/// <summary>
	/// Determines if the triangle that spans between the points a b and c has zero area (collinear).
	/// </summary>
	/// <returns><c>true</c> if zero area; otherwise, <c>false</c>.</returns>
	/// <param name="a">The first point.</param>
	/// <param name="b">The second point.</param>
	/// <param name="c">The third point.</param>
	public static bool IsZeroArea(Vector3 a, Vector3 b, Vector3 c)
	{
		return TriangleArea(a,b,c) < COLLINEARITY_THRESHOLD;
	}

	/// <summary>
	/// Area of a triangle given three points. Useful for telling if three points are collinear (on the same line).
	/// </summary>
	/// <returns>The area. </returns>
	/// <param name="a">First point. </param>
	/// <param name="b">Second point. </param>
	/// <param name="c">Third point. </param>
	public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 ab = a - b;
		Vector3 bc = b - c;
		Vector3 ac = a - c;

		float d0 = ab.magnitude;
		float d1 = bc.magnitude;
		float d2 = ac.magnitude;

		//Using Heron's formula
		float s = (d0 + d1 + d2) * 0.5f;
		return Mathf.Sqrt (s * (s - d0) * (s - d1) * (s - d2)); 
	}
}
