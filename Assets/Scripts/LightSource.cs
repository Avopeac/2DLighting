using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[AddComponentMenu("Patchwork/2D Point Light")]
/// <summary>
/// A light source detects occluders and create shadow geometry.
/// </summary>
public class LightSource : MonoBehaviour
{
    [Header("General settings: ")]
	public Material lightMaterial;
    public float updateFrequency = 2.0f;
    public bool isStatic = false;

    [Header("Shadow settings: ")]
    public int shadowCapacity = 50;
    public float shadowProjectionRange = 100.0f;

    [Header("Light settings: ")]
    public float radius = 10.0f;
	public float intensity = 1.0f;
	public Color inner = Color.white;
	public Color outer = Color.yellow;

    //For updating visible occluders
    private Vector2 position;
    private Collider2D[] visibleOccluders;
    private float timer = 0;

    //Keeps children with shadow geometry
    private List<Mesh> geometries;
    private int activeCounter = 0;

	//The light mesh, can be switched out
	private CustomPointLight customLight;
	private Mesh customLightMesh;

    // Use this for initialization
    void Start()
    {
		//Save 2D position
        position = new Vector2(transform.position.x, transform.position.y);

		//Create new collections
        visibleOccluders = new Collider2D[shadowCapacity];
        geometries = new List<Mesh>(shadowCapacity);
        
        //Add expected amount of geometries to the pool
        for (int i = 0; i < shadowCapacity; ++i) {
			Mesh mesh = new Mesh();
			mesh.MarkDynamic();
			geometries.Add (mesh);
		}

		//Create a new light mesh
		customLight = new CustomPointLight ();
		customLightMesh = customLight.CreateLightMesh (inner, outer, intensity, radius, 32);
		CreateLightChild ();
    }

	/// <summary>
	/// Creates the light child.
	/// </summary>
	void CreateLightChild()
	{
		//Create the object that will hold the visible colored light
		GameObject obj = new GameObject ();
		MeshFilter filter = obj.AddComponent<MeshFilter> ();
		MeshRenderer renderer = obj.AddComponent<MeshRenderer> ();

		//Set as child to this light source
		obj.transform.parent = this.transform;
		obj.transform.position = this.transform.position;
		obj.hideFlags = HideFlags.HideInHierarchy;

		filter.mesh = customLightMesh;

		//Copy the material to be able to set pass once
		Material materialCopy = new Material (lightMaterial);
		materialCopy.SetPass (2);
		renderer.material = materialCopy;
	}

	void OnRenderObject()
	{
		//Render each light
		lightMaterial.SetPass(0);
		Graphics.DrawMeshNow(customLightMesh, position, transform.rotation, 0);

		//Then render all shadow geometry
		for (int i = 0; i < activeCounter; ++i) {
			lightMaterial.SetPass(1);

			Graphics.DrawMeshNow(geometries[i], Matrix4x4.identity);
		}
	}

    void Update()
    {
       
        //Update 2D position
        position.Set(transform.position.x, transform.position.y);

        //Update occluders
        if (timer >= updateFrequency)
        {
            //Find occluders
            int count = Physics2D.OverlapCircleNonAlloc(position, radius, visibleOccluders);
            CreateShadowGeometries(count, ref visibleOccluders);
            timer = 0;
        }

        timer += Time.deltaTime;

        //Very ugly way to make them static
        if (isStatic)
        {
            timer -= Time.deltaTime + 1.0f;
        }
    }

    /// <summary>
    /// Create multiple shadow geometries with some given occluders.
    /// </summary>
    /// <param name="count">The amount of colliders. </param>
    /// <param name="colliders">The current collider results. </param>
    private void CreateShadowGeometries(int count, ref Collider2D[] colliders)
    {
        //Deactivate unused shadow geometry
		while (activeCounter > count) {
			activeCounter--;
		}

        //Activate the geometry
        for (int i = 0; i < count; ++i)
        {
			if (count < activeCounter)
            	activeCounter--;	
			else if (count > activeCounter)
				activeCounter++;

			//Get free mesh
			Mesh mesh = geometries[i];
			mesh.Clear();

            //Send old mesh object and the visible occluder
            PolygonCollider2D collider = visibleOccluders[i] as PolygonCollider2D;
            CreateShadowGeometry(ref mesh, ref collider);

            //Update mesh
            geometries[i] = mesh;
        }
    }

    /// <summary>
    /// Creates the shadow geometry mesh from collider data and our light source.
    /// </summary>
    /// <param name="mesh">The mesh to be updated. </param>
    /// <param name="collider">The collider which occludes the light. </param>
    private void CreateShadowGeometry(ref Mesh mesh, ref PolygonCollider2D collider)
    {

        Vector2 position = collider.transform.position;
        Vector2[] path = collider.GetPath(0);

        float[] angles = GetEdgeAngles(position, ref path);
        int[] boundaries = GetBoundaryIndices(ref angles);

        //We have double the amount of vertices since we project the shadow outward
        int vertCount = 2 * boundaries.Length;
        Vector3[] vertices = new Vector3[vertCount];

        int index = 0;
        foreach (int i in boundaries)
        {
            Vector2 pos = position + path[i];
            Vector2 dir = pos - this.position;

            vertices[index++] = pos;
            vertices[index++] = pos + shadowProjectionRange * dir;
        }

        index = 0;
        //Euler-Poincare gives us 3/2 times vertex count indices, each triangle has 3 indices
        int[] indices = new int[(int)(1.5f * vertCount)];
        for (int i = 0; i < indices.Length; i += 3)
        {
            //The winding is different for every other triangle
            if (index % 2 == 0)
            {
                indices[i + 0] = index + 0;
                indices[i + 1] = index + 1;
                indices[i + 2] = index + 2;

                index += 1;
            }
            else
            {
                indices[i + 0] = index + 1;
                indices[i + 1] = index + 2;
                indices[i + 2] = index + 0;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
    }

    /// <summary>
    /// Gets the start and end indices for projecting a shadow.
    /// </summary>
    /// <param name="angles">The edge angles of a path. The amount of angles should be the same length as the path. </param>
    /// <returns>The boundary indices. </returns>
    protected int[] GetBoundaryIndices(ref float[] angles)
    {
    
        //We know there's always 2 points to project shadow from
        int[] boundaries = new int[2];
        int count = 0;

        int length = angles.Length;
        int previous = length - 1;
        //Find indices where edge angles go from positive to negative or vice versa 
        for (int i = 0; i < length; ++i)
        {
            if (angles[i] < 0 && angles[previous] > 0 || angles[i] > 0 && angles[previous] < 0)
            {
                boundaries[count++] = previous;
            }

            previous = i;
        }

        return boundaries;
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
            Vector2 ray = position + path[i] - this.position;
            ray.Normalize();

            //Determine if edge is facing light or not, save the angles.
            float ndotl = Vector2.Dot(ray, edgeNormal);
            angles[i] = ndotl;
        }

        return angles;
    }
}
