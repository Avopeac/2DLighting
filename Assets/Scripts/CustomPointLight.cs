using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CustomPointLight : MonoBehaviour
{

		[Range(4, 64)]
		public int
				subdivisions = 32;
		[Range(0, 1)]
		public float
				intensity = 0.5f;
		[Range(0, 100)]
		public float
				radius = 1f;


		public Color innerColor = Color.white;
		public Color outerColor = Color.yellow;
		public Material meshMaterial;

		private Mesh mesh;
		private MeshFilter filter;
		private new MeshRenderer renderer;
		private Vector3[] vertices;
		private Vector2[] uvs;
		private int[] triangles;
		private Color[] colors;

		// Use this for initialization
		void Start ()
		{
				//Create a new mesh component dynamically
				if (gameObject.GetComponent<MeshRenderer> () == null) {
						gameObject.AddComponent<MeshRenderer> ();
				}
		
				if ((filter = gameObject.GetComponent<MeshFilter> ()) == null) {
						filter = gameObject.AddComponent<MeshFilter> () as MeshFilter;
				}

				CreateLightMesh ();
		}

		void CreateLightMesh ()
		{
				vertices = new Vector3[subdivisions];
				uvs = new Vector2[subdivisions];	
				colors = new Color[subdivisions];
				triangles = new int[subdivisions * 3];

				vertices [0] = Vector3.zero;
				uvs [0] = new Vector2 (0.5f, 0.5f);
				colors [0] = new Color (innerColor.r, innerColor.g, innerColor.b, intensity);

				float angle = 360.0f / (subdivisions - 1);

				float normedHorizontal, normedVertical;
				for (int i = 1; i < subdivisions; ++i) {
						vertices [i] = Quaternion.AngleAxis (angle * (float)(i - 1), Vector3.back) * Vector3.up;
						normedHorizontal = (vertices [i].x + 1.0f) * 0.5f;
						normedVertical = (vertices [i].y + 1.0f) * 0.5f;
						uvs [i] = new Vector2 (normedHorizontal, normedVertical);
						colors [i] = new Color (outerColor.r, outerColor.g, outerColor.b, 0);
				}

				int index;
				for (int i = 0; i + 2 < subdivisions; ++i) {
						index = i * 3;  
						triangles [index + 0] = 0;  
						triangles [index + 1] = i + 1;  
						triangles [index + 2] = i + 2;  
				}

				index = triangles.Length - 3;
				triangles [index + 0] = 0;
				triangles [index + 1] = subdivisions - 1;
				triangles [index + 2] = 1;

				if (mesh != null) {
					DestroyImmediate(mesh);
				}

				mesh = new Mesh ();
				mesh.vertices = vertices;
				mesh.uv = uvs;
				mesh.colors = colors;
				mesh.triangles = triangles;

				if (filter != null && mesh != null) {
						filter.mesh = mesh;
						filter.GetComponent<Renderer>().material = meshMaterial;
				}
		}

		public float getIntensity ()
		{
				return intensity;
		}

		public float getRadius ()
		{
				return radius;
		}

		public Color getInnerColor ()
		{
				return innerColor;
		}

		public Color getOuterColor ()
		{
				return outerColor;
		}

		public int getSubdivisions ()
		{
				return subdivisions;
		}

		void Update ()
		{
				if (Application.isEditor) {	
						CreateLightMesh ();
				}

				UpdateLight (radius, intensity, innerColor, outerColor);
		}

		void UpdateLight (float radius, float intensity, Color inner, Color outer)
		{
				MeshFilter filter = gameObject.GetComponent<MeshFilter> ();

				if (filter != null && filter.sharedMesh != null) {
						Mesh oldMesh = filter.sharedMesh;
						if (vertices == null) {
								vertices = mesh.vertices;	
						}
		
						Vector3[] updatedVertices = new Vector3[vertices.Length];
						colors [0] = new Color (inner.r, inner.g, inner.b, intensity);	
						Vector3 vertex = vertices [0]; 
						vertex.x *= radius;
						vertex.y *= radius;
						vertex.z = gameObject.transform.position.z;
						updatedVertices [0] = vertex;
		
		
						for (int i = 1; i < updatedVertices.Length; ++i) {
								vertex = vertices [i];
								vertex.x *= radius;
								vertex.y *= radius;
								vertex.z = gameObject.transform.position.z;
								colors [i] = new Color (outer.r, outer.g, outer.b, 0f);
								updatedVertices [i] = vertex;
						}
		
						oldMesh.vertices = updatedVertices;
						oldMesh.colors = colors;
				}
		}

		/* Immediate mode drawing of point light, this is deprecated code since no way to render to certain layers was found. */
		void DrawLight (Vector3 position, float radius, float intensity, int subdivs, Material material, Color inner, Color outer)
		{
				
				GL.PushMatrix ();
		
				/* Apply material */
				material.SetPass (0);
				GL.Begin (GL.TRIANGLE_STRIP);
		
				/* Create the first vertex aka origin and lerp color toward edges */
		
				GL.Color (new Color (inner.r, inner.g, inner.b, intensity));
				GL.Vertex3 (position.x, position.y, position.z);
		
				float half = Mathf.PI / 2f;
				float whole = Mathf.PI * 2f;
		
				for (float angle = -half/subdivs; angle <= whole; angle += half/subdivs) {
						/* Top half of circle */
						float x = radius * Mathf.Sin (angle);
						float y = radius * Mathf.Cos (angle);
			
						/* Color is black at edges */
						GL.Color (new Color (outer.r, outer.g, outer.b, 0f));
						GL.Vertex3 (x + position.x, y + position.y, position.z);
						/* Color is white in middle */
						GL.Color (new Color (inner.r, inner.g, inner.b, intensity));
						GL.Vertex3 (position.x, position.y, position.z);
				}
		
				GL.End ();
				GL.PopMatrix ();
		}
}
