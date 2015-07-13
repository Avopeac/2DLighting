using UnityEngine;
using System.Collections;

[AddComponentMenu("Patchwork/2D Light System")]
public class LightSystem : MonoBehaviour {

	public const string LIGHT_SOURCE_TAG = "Light Source";

	[Header("General Settings:")]
	public LayerMask mask;

	[Header("Ambient Settings: ")]
	public Color color;

	[Header("Configuration: ")]
	public Material lightMaskMaterial;
	public RenderTexture lightMaskTexture;

	private GameObject[] pointLightSources;

	// Use this for initialization
	void Start () {
		lightMaskTexture = RenderTexture.GetTemporary (Screen.width, Screen.height);
		CreateLightCameraChild ();
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		lightMaskTexture.DiscardContents ();

		GameObject[] pointLightSources = GameObject.FindGameObjectsWithTag (LIGHT_SOURCE_TAG);
	
		foreach (GameObject pointLightSource in pointLightSources) {
			RenderTexture lightMapTexture = pointLightSource.GetComponent<LightSource> ().LightMap;
			Graphics.Blit(lightMapTexture, lightMaskTexture, lightMaskMaterial, 0); 
		}

		Graphics.Blit (src, lightMaskTexture, lightMaskMaterial, 1);
		Graphics.Blit (lightMaskTexture, dst);
	}

	/// <summary>
	/// Creates the light camera child.
	/// </summary>
	private void CreateLightCameraChild()
	{
		GameObject obj = new GameObject ();
		Camera cam = obj.AddComponent<Camera> ();
		cam.CopyFrom (gameObject.GetComponent<Camera> ());
		cam.clearFlags = CameraClearFlags.Color;
		cam.backgroundColor = color;
		cam.targetTexture = lightMaskTexture;
		cam.cullingMask = 1 << (int) Mathf.Log(mask.value, 2);

		obj.transform.parent = this.transform;
		obj.hideFlags = HideFlags.HideInHierarchy;
	}
}

