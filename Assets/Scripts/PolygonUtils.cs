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
    public const float SQR_DIST_THRESHOLD = 0.05f;

    /// <summary>
    /// A shape is determined by a path that is traced clockwise or counter-clockwise. This shape is split given some plane of which to split along.
    /// </summary>
    /// <param name="split">The splitting plane. </param>
    /// <param name="path">The path that determines the shape. </param>
    /// <returns></returns>
    public static SplitResult Split(Plane split, ref Vector2[] path)
    {
        List<Vector2> negative = new List<Vector2>();
        List<Vector2> positive = new List<Vector2>();

        int length = path.Length;

        //Set previous point in path
        Vector2 previous = path[length - 1];
        for (int i = 0; i < length; ++i)
        {

            //Set current point in path
            Vector2 current = path[i];

            bool sideCurrent = split.GetSide(current);
            bool sidePrevious = split.GetSide(previous);

            if (sideCurrent != sidePrevious)
            {

                //Create a ray from the two points on different sides of the plane
                Vector2 direction = current - previous;
                direction.Normalize();
                Ray ray = new Ray(current, direction);

                //See where the ray intersects the plane
                float distance;
                split.Raycast(ray, out distance);

                //Collect the point and add it to both shapes
                Vector2 point = ray.GetPoint(distance);
                negative.Add(point);
                positive.Add(point);
            }

            //Check which side our current point is on and add it
            if (sideCurrent)
                positive.Add(current);
            else
                negative.Add(current);

            previous = current;
        }

        //Remove any duplicates in both shapes
        RemoveDuplicates(ref positive, SQR_DIST_THRESHOLD);
        RemoveDuplicates(ref negative, SQR_DIST_THRESHOLD);

        return new SplitResult(positive.ToArray(), negative.ToArray());
    }

    /// <summary>
    /// Remove duplicates in a list of points. Two points are considered the same if the distance between them is below some given threshold.
    /// </summary>
    /// <param name="path">A reference to the list of points. </param>
    /// <param name="threshold">Distance comparison. </param>
    private static void RemoveDuplicates(ref List<Vector2> path, float threshold)
    {
        if (path == null)
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
                    remove = true;

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
    public static int[] GetConcaveIndices(ref Vector2[] path)
    {

        Vector3 cross = Vector3.zero;
        List<int> negative = new List<int>();
        List<int> positive = new List<int>();

        int length = path.Length;

        Vector3 a, b;
        for (int i = 0; i < length; ++i)
        {
            int vertIndex = (i + 1) % length;

            //Two edges at a time
            a = path[(i + 2) % length] - path[vertIndex];
            b = path[vertIndex] - path[i];

            cross = Vector3.Cross(a, b);

            //Save the cross product between these edges
            if (cross.z < 0)
                negative.Add(vertIndex);
            else
                positive.Add(vertIndex);
        }

        //The one with least elements is the list with our conflicting points
        return negative.Count > positive.Count ? positive.ToArray() : negative.ToArray();
    }
}
