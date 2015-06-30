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

	//Keeps children with shadow geometry
	private const int SHADOW_CAPACITY = 10;
	private List<GameObject> geometries;

    // Use this for initialization
    void Start()
    {
        position = new Vector2(transform.position.x, transform.position.y);
		geometries = new List<GameObject> (SHADOW_CAPACITY);

		for (int i = 0; i < SHADOW_CAPACITY; ++i) {
			geometries.Add(CreateShadowChild("Shadow child" + i));
		}
    }

	private GameObject CreateShadowChild(string name)
	{
		GameObject obj = new GameObject(name);
		obj.transform.parent = transform;
		obj.transform.position = transform.position;
		obj.SetActive(false);
		
		MeshFilter filter = obj.AddComponent<MeshFilter>();
		obj.AddComponent<MeshRenderer>();

		Mesh mesh = new Mesh ();
		mesh.MarkDynamic ();

		filter.sharedMesh = mesh;

		return obj;
	}

    void Update()
    {

        //Update 2D position
        position.Set(transform.position.x, transform.position.y);

        //Update occluders
        if (timer >= updateFrequency)
        {
            CreateShadowGeometries(GetColliders());
			timer = 0;
        }

        timer += Time.deltaTime;
    }

    private void CreateShadowGeometries(Collider2D[] colliders)
    {
		int i = 0;
		while(colliders.Length > geometries.Count) {
			geometries.Add(CreateShadowChild("Extended shadow child " + i));
		}

		i = 0;
        foreach (Collider2D collider in colliders)
        {
			GameObject obj = geometries[i];
			obj.transform.position = collider.transform.position;
			obj.SetActive(true);
			MeshFilter filter = obj.GetComponent<MeshFilter>();
			Mesh mesh = filter.sharedMesh;
			CreateShadowGeometry(ref mesh, collider as PolygonCollider2D);
			filter.sharedMesh = mesh;

			i++;
        }
    }

    private void CreateShadowGeometry(ref Mesh mesh, PolygonCollider2D collider)
    {
        Vector2 position = collider.transform.position;
        Vector2[] path = collider.GetPath(0);

        float[] angles = GetEdgeAngles(position, ref path);
        LinkedList<int> boundaries = GetBoundaryIndices(ref angles);

        if (boundaries.Count > 1)
        {
			//Convex shapes always have 2 boundary points
			int first = boundaries.First.Value;
            int last = boundaries.Last.Value;

			//Find the vertices between boundary points
			int between = Mathf.Abs(last - first);
			between = 0;

			int vertCount = 2 * (between + 2);
			Vector3[] vertices = new Vector3[vertCount];

			//Add first boundary points
			Vector2 pos = position + path[first];
			Vector2 dir = pos - this.position;

			//Hit any other colliders?
			//RaycastHit2D hit = Physics2D.Raycast(pos, dir, projectionRange);
			float distance = projectionRange;

			//if (hit)
			//	distance = hit.distance;

			vertices[0] = pos;
			vertices[1] = pos + distance * dir;

			/*int j = last < first ? last : first;
			int count = 0;
			for (int i = j; i < j + between; ++i)
			{
				pos = position + path[i];

				//On shape
				if (count % 2 == 0)
				{
					vertices[count + 1] = pos;
				} else 
				{
					dir = GetLightRayToPosition(pos, Color.green);
					vertices[count + 1] = pos + projectionRange * dir;
				}

				count++;
			}*/

			//Add last boundary points
			pos = position + path[last];
			dir = pos - this.position;

			//Hit any other colliders?
			//hit = Physics2D.Raycast(pos, dir, projectionRange);
			//distance = projectionRange;
			
			//if (hit)
			//	distance = hit.distance;

			vertices[vertCount - 2] = pos;
			vertices[vertCount - 1] = pos + distance * dir;

			int[] indices = new int[(int) (1.5f * vertCount)];
			for(int i = 0; i < vertCount - 3; i += 3)
			{
				indices[i * 2 + 0] = i + 0;
				indices[i * 2 + 1] = i + 1;
				indices[i * 2 + 2] = i + 2;
			}

			mesh.vertices = vertices;
			mesh.triangles = indices;
        }
    }

	/// <summary>
	/// Gets the boundary indices.
	/// </summary>
	/// <returns>The boundary indices.</returns>
	/// <param name="angles">Angles.</param>
    protected LinkedList<int> GetBoundaryIndices(ref float[] angles)
    {
        LinkedList<int> indices = new LinkedList<int>();
        int length = angles.Length;
        int previous = length - 1;
        
        for (int i = 0; i < length; ++i)
        {
            if (angles[i] < 0 && angles[previous] > 0 || angles[i] > 0 && angles[previous] < 0)
			{
                indices.AddLast(previous);
			}

            previous = i;
        }

        return indices;
    }

	/// <summary>
	/// Gets the edge angles.
	/// </summary>
	/// <returns>The edge angles.</returns>
	/// <param name="position">Position.</param>
	/// <param name="path">Path.</param>
    protected float[] GetEdgeAngles(Vector2 position, ref Vector2[] path)
    {
        int length = path.Length;
        float[] angles = new float[length];

        //Set previous point
        Vector2 previous = path[length - 1];
        for (int i = 0; i < length; ++i)
        {   
            //Find current edge normal
            Vector2 edgeNormal = PolygonUtils.GetNormal(previous, path[i]);
            previous = path[i];

            //Cast a ray from the light to this point
            Vector2 ray = GetLightRayToPosition(position + path[i], Color.clear);
            ray.Normalize();

            //Determine if edge is facing light or not, save the angles.
            float ndotl = Vector2.Dot(ray, edgeNormal);
            angles[i] = ndotl;
        }

        return angles;
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
