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
	public LightSystem lightSystem;

    [Header("Shadow settings: ")]
	public Material shadowMaterial;
    public int shadowCapacity = 50;
    public float shadowProjectionRange = 100.0f;

    [Header("Light settings: ")]
	public Material lightMaterial;
    public float radius = 10.0f;
	public Color inner = Color.white;
	public Color outer = Color.yellow;

	//Declaring some things that will be used frequently
    private Vector2 position;
	private List<int> indices;

	//For updating visible occluders
	private int activeCounter = 0;
	private List<Mesh> geometries;
	private Collider2D[] visibleOccluders;
	
	//The light mesh
	private Mesh customLightMesh;

	//Light camera
	public RenderTexture LightMap { get; private set; }
	private Camera lightCamera;

    // Use this for initialization
    void Start()
    {
		//Save 2D position
        position = new Vector2(transform.position.x, transform.position.y);
		indices = new List<int> ();

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
		customLightMesh = light.CreateLightMesh (radius, 32);

		//Screen size texture for this light, we use mipmaps to downsample and get some blur
		LightMap = new RenderTexture (Screen.width, Screen.height, 0);
		LightMap.generateMips = true;
		LightMap.useMipMap = true;
		LightMap.mipMapBias = 3;

		//Create child objects for a visible mesh and a camera to isolate single light sources
		CreateCameraChild ();
    }

	/// <summary>
	/// Creates the camera child. Each light needs to be rendered into a texture along with its shadow geometry.
	/// </summary>
	private void CreateCameraChild()
	{
		Camera mainCamera = lightSystem.GetComponent<Camera> ();

		//Create new child game object
		GameObject obj = new GameObject ();
		obj.transform.parent = transform;
		obj.transform.position = mainCamera.transform.position;
		obj.hideFlags = HideFlags.HideInHierarchy;

		//Create the camera to capture light source and shadows
		Camera cam = obj.AddComponent<Camera> ();
		cam.CopyFrom (mainCamera);
		cam.backgroundColor = new Color(0,0,0,0);
		cam.cullingMask = 0;
		cam.cullingMask = 1 << 10;
		cam.targetTexture = LightMap;

		lightCamera = cam;
	}
	
	void OnRenderObject()
	{
		LightMap.DiscardContents (true, true);

		//Render one light
		lightMaterial.SetFloat ("_Radius", radius);
		lightMaterial.SetColor ("_Inner", inner);
		lightMaterial.SetColor ("_Outer", outer);
		Graphics.DrawMesh (customLightMesh, position, transform.rotation, lightMaterial, 10, lightCamera);

		//Then render all shadow geometry
		for (int i = 0; i < activeCounter; ++i) {
			Graphics.DrawMesh(geometries[i], Matrix4x4.identity, shadowMaterial, 10, lightCamera);
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

            //Send old mesh object and the visible occluder
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
        int[] indices = GetShadowProjectionIndices(position, ref path);

		//We have double the amount of vertices since we project the shadow outward
		int vertCount = 2 * indices.Length;
		Vector3[] vertices = new Vector3[vertCount];
		int[] triangles = new int[3 * (vertCount - 2)];

		//Not enough vertices
		if (vertCount < 2)
			return;

		//Sort the angles so that mesh build in correct order
		SortAngles (position, ref indices, ref path);

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
	/// Sorts the angles.
	/// </summary>
	/// <param name="position">Position.</param>
	/// <param name="indices">Indices.</param>
	/// <param name="path">Path.</param>
	protected void SortAngles(Vector2 position, ref int[] indices, ref Vector2[] path)
	{
		int length = indices.Length;

		//The position on the radius in the direction from the light source to the occluder
		Vector2 radiusPosition = this.position - (position - this.position) * radius;

		do {
			int n = 0;

			for (int i = 1; i < length; ++i) {

				Vector2 prev = path [indices [i - 1]] - radiusPosition;
				Vector2 curr = path [indices [i]] - radiusPosition;

				if (Mathf.Atan2(prev.y, prev.x) > Mathf.Atan2(curr.y, curr.x)) {

					int temp = indices [i];
					indices [i] = indices [i - 1];
					indices [i - 1] = temp;

					n = i;
				}
			}

			length = n;

		} while (length != 0);
	}

	/// <summary>
	/// Gets the shadow projection indices.
	/// </summary>
	/// <returns>The shadow projection indices.</returns>
	/// <param name="position">Position.</param>
	/// <param name="path">Path.</param>
    protected int[] GetShadowProjectionIndices(Vector2 position, ref Vector2[] path)
    {
        int length = path.Length;

		//Clear old indices
		indices.Clear ();
	
        //Set previous point
		int prevIndex = length - 1;
        for (int i = 0; i < length; ++i)
        {
            //Find current edge normal
            Vector2 edgeNormal = PolygonUtils.GetNormal(path[prevIndex], path[i]);

			//Find offset and plane equation
			float d = Vector2.Dot(edgeNormal, path[i] - position);

			//Eliminate popping by finding points at the maximum radius
			float plane = Vector2.Dot(edgeNormal, this.position + (position - this.position) * radius);
		
			//Sign determines back or front
			if (Mathf.Sign(plane + d) > 0.05f)
			{
			
				if (!indices.Contains(prevIndex))
					indices.Add(prevIndex);

				if (!indices.Contains(i))
					indices.Add(i);
			} 
		
			prevIndex = i;
        }

        return indices.ToArray();
    }
}
