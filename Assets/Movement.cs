using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{

    Vector2 position;

    // Use this for initialization
    void Start()
    {
        position = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        float x = 0.1f * Mathf.Cos(Mathf.PI * Time.fixedTime);
        float y = 0.1f * Mathf.Sin(Mathf.PI * Time.fixedTime);

        Vector3 pos = transform.position;
        pos.x += x;
        pos.y += y;

        transform.position = pos;
    }
}
