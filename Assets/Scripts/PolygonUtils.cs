using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PolygonUtils
{

    public const int SPLITS = 2;

    public static Vector2[][] Split(Plane plane, Vector2[] path)
    {
        List<Vector2> negative = new List<Vector2>();
        List<Vector2> positive = new List<Vector2>();

        int length = path.Length;
        Vector2 previous = path[length - 1];
        for (int i = 0; i < length; ++i)
        {
            Vector2 current = path[i];

            if (!plane.SameSide(current, previous))
            {
                Vector2 direction = current - previous;
                Ray ray = new Ray(current, direction);

                float distance;
                plane.Raycast(ray, out distance);

                Vector2 point = ray.GetPoint(distance);
                if (Mathf.Abs(distance) > 0.0f && Mathf.Abs(distance) < 1.0f)
                {
                    negative.Add(point);
                    positive.Add(point);
                }
            }

            if (plane.GetSide(current))
            {
                positive.Add(current);
            }
            else
            {
                negative.Add(current);
            }

            previous = current;
        }

        Vector2[][] splits = new Vector2[SPLITS][];
        splits[0] = positive.ToArray();
        splits[1] = negative.ToArray();

        return splits;
    }
}
