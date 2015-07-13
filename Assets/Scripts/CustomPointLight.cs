using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomPointLight
{
		private Mesh mesh;
		private Vector3[] vertices;
		private Vector2[] uvs;
		private int[] triangles;

		public Mesh CreateLightMesh (float radius, int subdivisions)
		{
				vertices = new Vector3[subdivisions];
				uvs = new Vector2[subdivisions];	
				triangles = new int[subdivisions * 3];

				vertices [0] = Vector3.zero;
				uvs [0] = new Vector2 (0f, 0f);

				float angle = 360.0f / (subdivisions - 1);

				for (int i = 1; i < subdivisions; ++i) {
						vertices [i] = Quaternion.AngleAxis (angle * (float)(i - 1), Vector3.back) * Vector3.up * radius;
						uvs [i] = new Vector2 (1, 1);
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

				mesh = new Mesh ();
				mesh.vertices = vertices;
				mesh.uv = uvs;
				mesh.triangles = triangles;

				return mesh;
		}
}
