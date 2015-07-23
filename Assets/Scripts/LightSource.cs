using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Patchwork/2D Point Light")]
/// <summary>
/// A light source detects occluders and create shadow geometry.
/// </summary>
public class LightSource : MonoBehaviour
{
	private const string LIGHT_CAMERA_NAME = "Light Camera";

    [Header("Configuration settings: ")]
	public LightSystem lightSystem;
	public Material shadowMaterial;
	public Material lightMaterial;
	public int lightLayer;
	public int subdivisions = 32;
	public int mipMapLevel = 2;

    [Header("Shadow settings: ")]
    public int shadowCapacity = 50;
    public float shadowProjectionRange = 100.0f;

    [Header("Light settings: ")]
	public Texture lightCookie;
    public float radius = 10.0f;
	public Color inner = Color.white;
	public Color outer = Color.yellow;

	//Declaring some things that will be used frequently
    private Vector2 position;
	private List<int> indices;
	private List<Edge> edges;
	private static MaterialPropertyBlock properties;

	//For updating visible occluders
	private int activeCounter = 0;
	private List<Mesh> geometries;
	private Collider2D[] visibleOccluders;
	
	public RenderTexture LightMap { get; private set; }
	private Camera lightCamera;
	private Mesh customLightMesh;

    // Use this for initialization
    void Start()
    {

		if (lightSystem == null)
			Debug.LogError ("Light system cannot be set to null. ");


		//Save 2D position
        position = new Vector2(transform.position.x, transform.position.y);
		indices = new List<int> ();
		edges = new List<Edge> ();
	
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
		CustomPointLight light = new CustomPointLight ();
		customLightMesh = light.CreateLightMesh (radius, subdivisions);

		//Screen size texture for this light, we use mipmaps to downsample and get some blur
		LightMap = new RenderTexture (128 * (Screen.width / Screen.height), 128, 0);
		LightMap.generateMips = true;
		LightMap.useMipMap = true;
		LightMap.mipMapBias = 2;

		//Create child objects for a visible mesh and a camera to isolate single light sources
		CreateCameraChild ();

		//Single material with different properties
		if (properties == null)
			properties = new MaterialPropertyBlock ();

		properties.AddColor ("_Inner", inner);
		properties.AddColor ("_Outer", outer);
		properties.AddTexture ("_MainTex", lightCookie);
    }

	/// <summary>
	/// Creates the camera child. Each light needs to be rendered into a texture along with its shadow geometry.
	/// </summary>
	private void CreateCameraChild()
	{
		Camera mainCamera = lightSystem.GetComponent<Camera> ();

		//Create new child game object
		GameObject obj = new GameObject (LIGHT_CAMERA_NAME);
		obj.transform.parent = transform;
		obj.transform.position = mainCamera.transform.position;
		obj.hideFlags = HideFlags.HideInHierarchy;

		//Create the camera to capture light source and shadows
		Camera cam = obj.AddComponent<Camera> ();
		cam.CopyFrom (mainCamera);
		cam.backgroundColor = Color.clear;	
		cam.cullingMask = 0;
		cam.cullingMask = 1 << lightLayer;
		cam.targetTexture = LightMap;

		lightCamera = cam;
	}
	
	void OnRenderObject()
	{
		//Render one light
        properties.Clear();
		properties.SetColor ("_Inner", inner);
		properties.SetColor ("_Outer", outer);
		properties.SetTexture ("_MainTex", lightCookie);
        //lightMaterial.SetColor("_Inner", inner);
        //lightMaterial.SetColor("_Outer", outer);
        //lightMaterial.SetTexture("_MainTex", lightCookie);
		Graphics.DrawMesh (customLightMesh, position, transform.rotation, lightMaterial, lightLayer, lightCamera, 0, properties);

		//Then render all shadow geometry
		for (int i = 0; i < activeCounter; ++i) {
			Graphics.DrawMesh(geometries[i], Matrix4x4.identity, shadowMaterial, lightLayer, lightCamera);
		}
	}

    void Update()
    {
        //Update 2D position
        position.Set(transform.position.x, transform.position.y);

		//Follow main camera
		lightCamera.transform.position = lightSystem.GetComponent<Camera>().transform.position;

        //Find occluders
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, visibleOccluders);
        CreateShadowGeometries(count, ref visibleOccluders);
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

            PolygonCollider2D collider = visibleOccluders[i] as PolygonCollider2D;
			Bounds bounds = collider.bounds;
			bounds.Expand(0.2f);

			if (bounds.Contains(this.position))
				return;

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
		int[] indices = GetBackVertices (position, ref path);

		//We have double the amount of vertices since we project the shadow outward
		int vertCount = 2 * indices.Length;

		//Not enough vertices
		if (vertCount < 2)
			return;

		Vector3[] vertices = new Vector3[vertCount];
		int[] triangles = new int[3 * (vertCount - 2)];

		int index = 0;
        foreach (int i in indices)
        {
            Vector2 pos = position + path[i];
            Vector2 dir = pos - this.position;

			//Calculate the range of the shadow
			float range = shadowProjectionRange - dir.magnitude;
			if (range < 0)
				range = 0;

			//The point on the path and the projected position
            vertices[index++] = pos;
            vertices[index++] = pos + range * dir;
        }

        index = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            //The winding is different for every other triangle
            if (index % 2 == 0)
            {
                triangles[i + 0] = index + 2;
                triangles[i + 1] = index + 1;
                triangles[i + 2] = index + 0;
            }
            else
            {
                triangles[i + 0] = index + 1;
                triangles[i + 1] = index + 0;
                triangles[i + 2] = index + 2;
            }

			index++;
        }

		//Set new mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

	/// <summary>
	/// Gets the back vertices.
	/// </summary>
	/// <returns>The back vertices.</returns>
	/// <param name="center">Center.</param>
	/// <param name="offsets">Offsets.</param>
	private int[] GetBackVertices(Vector2 center, ref Vector2[] offsets)
	{
		edges.Clear();
		//Projection vector is to eliminate "point walking" of the shadow geometry
		Vector2 prev, curr, normal, dir, proj = this.position + (position - this.position) * radius;
		int length = offsets.Length, i = length - 1;
		//Get world position of the last collider point
		prev = center + offsets [length - 1];
		for (int j = 0; j < length; ++j) {
			//Get world position of the current collider point
			curr = center + offsets[j];
			normal = PolygonUtils.GetNormal(prev, curr);
			dir = prev - proj;

			//Add edge if the edge normal and direction to from the light to a edge point isn't visible
			//Could check if visible to make shadows that overlaps the object
			if (Vector2.Dot (dir.normalized, normal.normalized) > 0)
			{ 
				edges.Add(new Edge(i, j));
			}

			prev = curr;
			i = j;
		}

		//Sort edge index pairs (1 2) (3 0) (0 1) => (0 1) (1 2) (3 0)
		edges.Sort (delegate(Edge x, Edge y) {
			if (x.FirstIndex == y.SecondIndex) return 1;
			else if (x.SecondIndex == y.FirstIndex) return -1;
			else return 0;
		});

		indices.Clear ();
		//Add unique indices
		foreach (Edge e in edges) {
			
			if(!indices.Contains(e.FirstIndex))
				indices.Add(e.FirstIndex);
			
			if(!indices.Contains(e.SecondIndex))
				indices.Add(e.SecondIndex);
		}

		return indices.ToArray();
	}

	//Keeps two indices for a collider edge
	private struct Edge 
	{ 
		public int FirstIndex { get; private set; }
		public int SecondIndex { get; private set; }

		public Edge(int firstIndex, int secondIndex)
		{
			FirstIndex = firstIndex;
			SecondIndex = secondIndex;
		}
	}
}
