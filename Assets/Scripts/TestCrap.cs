using UnityEngine;
using System.Collections;

public class TestCrap : MonoBehaviour {

	public CustomPointLight pointLight;
	public Material pointLightMaterial;

	// Use this for initialization
	void Start () {
	
		pointLight = new CustomPointLight ();
		Mesh mesh = pointLight.CreateLightMesh (Color.white, Color.white, 1f, 10f, 32);

		MeshFilter filter = GetComponent<MeshFilter> ();
		filter.mesh = mesh;

		MeshRenderer renderer = GetComponent<MeshRenderer> ();
		renderer.material = pointLightMaterial;
	}
}
