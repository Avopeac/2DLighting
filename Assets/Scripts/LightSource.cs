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

        foreach (Collider2D collider in colliders)
        {
          //  if (collider.gameObject.GetComponent<Occluder>() != null)
                shadows.Add(CreateShadowGeometry(collider as PolygonCollider2D));
        }

        return shadows;
    }

    private Mesh CreateShadowGeometry(PolygonCollider2D collider)
    {
        Vector2 position = collider.transform.position;
        Vector2[] path = collider.GetPath(0);

        float[] angles = GetEdgeAngles(position, ref path);
        LinkedList<int> boundaries = GetBoundaryIndices(ref angles);

        if (boundaries.Count > 1)
        {
            int first = boundaries.First.Value;
            int last = boundaries.Last.Value;

            GetLightRayToPosition(position + path[first], Color.green);
            GetLightRayToPosition(position + path[last], Color.green);
        }

        return null;
    }

    protected LinkedList<int> GetBoundaryIndices(ref float[] angles)
    {
        LinkedList<int> indices = new LinkedList<int>();
        int length = angles.Length;

        int previous = length - 1;
        Debug.Log("start");
        for (int i = 0; i < length; ++i)
        {
            Debug.Log(angles[i]);
            if (angles[i] < 0 && angles[previous] > 0 || angles[i] > 0 && angles[previous] < 0)
                indices.AddLast(previous);

            previous = i;
        }

        return indices;
    }

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
            Vector2 ray = GetLightRayToPosition(position + path[i], Color.cyan);
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
