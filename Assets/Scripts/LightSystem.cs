using UnityEngine;
using System.Collections;

[AddComponentMenu("Patchwork/2D Light System")]
public class LightSystem : MonoBehaviour {

	public const string LIGHT_SOURCE_TAG = "Light Source";

	[Header("General Settings:")]
	public LayerMask mask;
	public Material maskMaterial;

	[Header("Ambient Settings: ")]
	public Color color;

	private RenderTexture lightMaskTexture;
	private GameObject[] pointLightSources;

	// Use this for initialization
	void Start () {
		lightMaskTexture = RenderTexture.GetTemporary (Screen.width, Screen.height);
		lightMaskTexture.antiAliasing = 8;
		lightMaskTexture.filterMode = FilterMode.Trilinear;
		lightMaskTexture.useMipMap = true;
		lightMaskTexture.generateMips = true;


		CreateLightCameraChild ();
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		lightMaskTexture.DiscardContents ();

		GameObject[] pointLightSources = GameObject.FindGameObjectsWithTag (LIGHT_SOURCE_TAG);
	
		foreach (GameObject pointLightSource in pointLightSources) {
			RenderTexture lightMapTexture = pointLightSource.GetComponent<LightSource> ().lightMapTexture;
			Graphics.Blit(lightMapTexture, lightMaskTexture, maskMaterial, 0); 
		}

		Graphics.Blit (src, lightMaskTexture, maskMaterial, 1);
		Graphics.Blit (lightMaskTexture, dst);
	}

	void CreateLightCameraChild()
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

	void OnDisable()
	{
		RenderTexture.ReleaseTemporary (lightMaskTexture);
	}
}

