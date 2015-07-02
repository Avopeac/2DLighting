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

    //The material for shadow geometry
    public Material shadowMaterial;

    // Use this for initialization
    void Start()
    {
        position = new Vector2(transform.position.x, transform.position.y);
        geometries = new List<GameObject>(SHADOW_CAPACITY);

        for (int i = 0; i < SHADOW_CAPACITY; ++i)
        {
            geometries.Add(CreateShadowChild("Shadow child" + i));
        }
    }

    private GameObject CreateShadowChild(string name)
    {
        Mesh mesh = new Mesh();
        mesh.MarkDynamic();

        GameObject obj = new GameObject(name);
        //obj.transform.parent = transform;
        obj.SetActive(false);

        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = shadowMaterial;

        MeshFilter filter = obj.AddComponent<MeshFilter>();
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

       //while (colliders.Length > geometries.Count)
       // {
       //     geometries.Add(CreateShadowChild("Extended shadow child"));
       // }

        int i = geometries.Count - 1;
        while (colliders.Length < geometries.Count && i >= 0)
        {
            geometries[i].SetActive(false);
            
            i--;
        }

        i = 0;
        foreach (Collider2D collider in colliders)
        {
            GameObject obj = geometries[i];
            // obj.transform.position = -this.transform.position;
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

        mesh.Clear(false);

        Vector2 position = collider.transform.position;
        Vector2[] path = collider.GetPath(0);

        float[] angles = GetEdgeAngles(position, ref path);
        LinkedList<int> boundaries = GetBoundaryIndices(ref angles);

        //Find the vertices between boundary points
        int vertCount = 2 * boundaries.Count;
        Vector3[] vertices = new Vector3[vertCount];

        int index = 0;
        foreach (int i in boundaries)
        {
            Vector2 pos = position + path[i];
            Vector2 dir = pos - this.position;

            vertices[index++] = pos;
            vertices[index++] = pos + projectionRange * dir;
        }

        index = 0;
        int[] indices = new int[(int)(1.5f * vertCount)];
  
        for (int i = 0; i < indices.Length; i += 3)
        {
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
