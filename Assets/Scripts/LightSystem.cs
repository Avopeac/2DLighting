using UnityEngine;
using System.Collections;

[AddComponentMenu("Patchwork/2D Light System")]
public class LightSystem : MonoBehaviour {

	[Header("General Settings:")]
	public LayerMask mask;
	public Material maskMaterial;

	[Header("Ambient Settings: ")]
	public Color color;

	private RenderTexture lightMaskTexture;

	// Use this for initialization
	void Start () {
		lightMaskTexture = RenderTexture.GetTemporary (Screen.width,
		                                               Screen.height,
		                                               16,
		                                               RenderTextureFormat.ARGB32,
		                                               RenderTextureReadWrite.Default);
		CreateLightCameraChild ();
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		Graphics.Blit (src, dst, maskMaterial);
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
