using UnityEngine;
using System.Collections;

public class PointGizmo : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnDrawGizmos()
	{
		Gizmos.DrawSphere (this.transform.position, 0.1f);
	}
}
