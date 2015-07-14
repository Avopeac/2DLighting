using UnityEngine;
using System.Collections;

[AddComponentMenu("Patchwork/2D Light System")]
public class LightSystem : MonoBehaviour {

	public const string LIGHT_SOURCE_TAG = "Light Source";

	[Header("General Settings:")]
	public LayerMask mask;
	public Material lightMaskMaterial;

	[Header("Ambient Settings: ")]
	public Color color;

	private GameObject[] pointLightSources;

	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{

		//For some reason the dst needs to be cleared because camera doesn't do it.
		RenderTexture.active = dst;
		GL.Clear(true, true, Color.clear);

		GameObject[] pointLightSources = GameObject.FindGameObjectsWithTag (LIGHT_SOURCE_TAG);
		int length = pointLightSources.Length;
		for (int i = 0; i < length; ++i) {
				RenderTexture lightMapTexture = pointLightSources[i].GetComponent<LightSource> ().LightMap;
				Graphics.Blit(lightMapTexture, dst, lightMaskMaterial, 0);
		}

		Graphics.Blit (src, dst, lightMaskMaterial, 1);
	}
}