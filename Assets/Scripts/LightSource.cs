using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightSource : MonoBehaviour
{
    //Light source data, projection range should be determined by screen space in future
    public float radius = 10.0f;
    public float projectionRange = 10.0f;
    private Vector2 position;

    //Enable debuggers 
    public bool debugger = false;

    //For updating visible occluders
    public float updateFrequency = 2.0f;
    private float timer = 0;
    private Collider2D[] colliders;


    // Use this for initialization
    void Start()
    {
        position = new Vector2(transform.position.x, transform.position.y);
    }

    void Update()
    {

        //Update 2D position
        position.Set(transform.position.x, transform.position.y);

        //Update occluders
        if (timer >= updateFrequency)
        {
            timer = 0;
            colliders = GetColliders();
            CreateShadowGeometries(colliders);
        }

        timer += Time.deltaTime;
    }

    private IList<Mesh> CreateShadowGeometries(Collider2D[] colliders)
    {
        IList<Mesh> shadows = new List<Mesh>();

        foreach(Collider2D collider in colliders)
        {
            if (collider.gameObject.GetComponent<Occluder>() != null)
                shadows.Add(CreateShadowGeometry(collider as PolygonCollider2D));
        }

        return shadows;
    }

    private Mesh CreateShadowGeometry(PolygonCollider2D collider)
    {
		Vector2 position = collider.transform.position;
		Vector2[] path = GetWorldPathPoints(ref collider, 0);
		float[] angles = GetEdgeAngles(position, ref path);
	
		LinkedList<int> boundaries = GetBoundaryIndices(ref angles);

		//Collider is concave, no support
		if (boundaries.Count == 2)
		{
			int first = boundaries.First.Value;
			int last = boundaries.Last.Value;

		}

        return null;
    }

	protected Vector2[] GetWorldPathPoints(ref PolygonCollider2D collider, int pathIndex)
	{
		Vector2[] path = collider.GetPath(0);

		//Support for scaling and rotation, preferrably this is not done every frame in the future
		for(int i = 0; i < path.Length; ++i)
		{
			path[i] = collider.transform.rotation * path[i]; 
			path[i].Scale(collider.transform.localScale);
		}

		return path;
	}

	protected LinkedList<int> GetBoundaryIndices(ref float[] angles)
	{
		int length = angles.Length, index = 0;
		LinkedList<int> indices = new LinkedList<int>();
		for (int i = 1; i < length + 1; ++i)
		{
			//Wrap around
			index = i % length;

			//Need to check both ways
			if (angles[index] <= 0 && angles[i-1] > 0 || angles[index] > 0 && angles[i-1] <= 0)
				indices.AddLast (i-1);
		}

		return indices;
	}

	protected float[] GetEdgeAngles(Vector2 position, ref Vector2[] path)
	{
		int length = path.Length, index = 0;
		float[] angles = new float[length];

		for (int i = 1; i < length + 1; ++i)
		{
			//Wrap around
			index = i % length;
			
			//Find current edge normal
			Vector2 edgeNormal = GetEdgeNormal(path[i-1], path[index]);
			
			//Cast a ray from the light to this point
			Vector2 ray = GetLightRayToPosition(position + path[index], Color.cyan).normalized;
			
			//Determine if edge is facing light or not, save the angles.
			float ndotl = Vector2.Dot(ray, edgeNormal);
			angles[index] = ndotl;
		}

		return angles;
	}

	protected Vector2 GetEdgeNormal(Vector2 v1, Vector2 v2)
	{
		//Find normal to edge
		float nx = v2.y - v1.y;
		float ny = v2.x - v1.x;
		
		//Create the edge normal
		return new Vector2(nx, -ny).normalized;
	}

    protected Vector2 GetLightRayToPosition(Vector2 position, Color color)
    {
        if (debugger)
        {
            Debug.DrawRay(this.position, position - this.position, color, 0.25f, false);
        }

        return position - this.position;
    }

    protected Collider2D[] GetColliders()
    {
        return Physics2D.OverlapCircleAll(position, radius);
    }
}
