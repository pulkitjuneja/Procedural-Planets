using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Vector3;

public class OctaSphereGenerator {

  [HideInInspector]
	public Vector3[] Vertices;

  [HideInInspector]
	public int[] Triangles;

  [Range(0,500)]
	public int resolution;

	// Internal:
	List<Vector3> verticesTemp;
	List<int> trianglesTemp;
	int numVertsPerFace;
	List<Edge> edges;

	// Edge indices
	static readonly int[] edgeVerticePairs = { 0, 1,  
                                               0, 2,  
                                               0, 3,  
                                               0, 4,  
                                               1, 2,  
                                               2, 3,  
                                               3, 4,  
                                               4, 1,  
                                               5, 1,  
                                               5, 2,  
                                               5, 3,  
                                               5, 4 };
	
  // Triangle edges
	static readonly int[] faceEdges = { 0, 1, 4,  
                                      1, 2, 5,
                                      2, 3, 6,
                                      3, 0, 7,
                                      8, 9, 4,  
                                      9, 10, 5,  
                                      10, 11, 6,  
                                      11, 8, 7 };
	// The six initial vertices
	static readonly Vector3[] baseVertices = { up, left, back, right, forward, down };

	// void OnValidate() {
	// 	this.generate(resolution);
	// 	generateDebugMesh();
	// }

	public void subdivideEdges() {
		edges = new List<Edge>(12);
		for (int i = 0; i < edgeVerticePairs.Length; i += 2) {
			Vector3 startVertex = verticesTemp[edgeVerticePairs[i]];
			Vector3 endVertex = verticesTemp[edgeVerticePairs[i + 1]];

			int[] edgeVertexIndices = new int[resolution + 2];
			edgeVertexIndices[0] = edgeVerticePairs[i];

			// Add vertices along edge
			for (int divisionIndex = 0; divisionIndex < resolution; divisionIndex++) {
				float t = (float)(divisionIndex +1f) / (float)(resolution +1f);
				edgeVertexIndices[divisionIndex + 1] = verticesTemp.Count;
				verticesTemp.Add (Slerp (startVertex, endVertex, t));
			}
			edgeVertexIndices[resolution + 1] = edgeVerticePairs[i + 1];
			int edgeIndex = i / 2;
			edges.Insert(edgeIndex, new Edge (edgeVertexIndices));
		}
	}

	public (Vector3[], int[]) generate (int resolution) {
		this.resolution = resolution;
    numVertsPerFace = ((int)Mathf.Pow(resolution, 2) + 6 + resolution*5) / 2;
		int numVerts = numVertsPerFace * 8 - resolution * 12 + 30;
		int numTrisPerFace = (resolution + 1) * (resolution + 1);
		verticesTemp = new List<Vector3> (numVerts);
		trianglesTemp = new List<int> (numTrisPerFace * 8 * 3);
		verticesTemp.AddRange (baseVertices);

		subdivideEdges();

		// Create faces
		for (int i = 0; i < 24; i += 3) {
			int faceIndex = i / 3;
			bool reverse = faceIndex >= 4;
			List<int> subdiviedFaceVertices = subdivideFace (edges[faceEdges[i]], edges[faceEdges[i + 1]], edges[faceEdges[i + 2]]);
			triangulateFaces(subdiviedFaceVertices, reverse);
		}

		Vertices = verticesTemp.ToArray();
		Triangles = trianglesTemp.ToArray();
		return (Vertices, Triangles);
	}

	List<int> subdivideFace (Edge left, Edge right, Edge bottom) {
		int numPointsInEdge = resolution + 2;
		var faceVertices = new List<int> (numVertsPerFace);

    // common vertex for both left and right sides
		faceVertices.Add (left.vertexIndices[0]);

    // Add vertices row-wise
		for (int i = 1; i < numPointsInEdge - 1; i++) {
			faceVertices.Add (left.vertexIndices[i]);

			Vector3 leftVertex = verticesTemp[left.vertexIndices[i]];
			Vector3 rightVertex = verticesTemp[right.vertexIndices[i]];
			int numInnerPoints = i - 1;
			for (int j = 0; j < numInnerPoints; j++) {
				float t = (j + 1f) / (numInnerPoints + 1f);
				faceVertices.Add (verticesTemp.Count);
				verticesTemp.Add (Slerp (leftVertex, rightVertex, t));
			}

			faceVertices.Add (right.vertexIndices[i]);
		}

		// Add bottom edge vertices
		for (int i = 0; i < numPointsInEdge; i++) {
			faceVertices.Add (bottom.vertexIndices[i]);
		}
		return faceVertices;
	}

	void triangulateFaces (List<int> faceVertices, bool reverse) {
		int numRows = resolution + 1;
		for (int row = 0; row < numRows; row++) {

      //
			int topVertex = ((row + 1) * (row + 1) - row - 1) / 2;
			int bottomVertex = ((row + 2) * (row + 2) - row - 2) / 2;

			int numTrianglesInRow = 1 + 2 * row;
			for (int column = 0; column < numTrianglesInRow; column++) {
				int v0, v1, v2;

				if (column % 2 == 0) {
					v0 = topVertex;
					v1 = bottomVertex + 1;
					v2 = bottomVertex;
					topVertex++;
					bottomVertex++;
				} else {
					v0 = topVertex;
					v1 = bottomVertex;
					v2 = topVertex - 1;
				}

				trianglesTemp.Add (faceVertices[v0]);
				trianglesTemp.Add (faceVertices[(reverse) ? v2 : v1]);
				trianglesTemp.Add (faceVertices[(reverse) ? v1 : v2]);
			}
		}
	}

  // void generateDebugMesh () {
  //   MeshFilter currentMeshFilter = gameObject.GetComponent<MeshFilter>();
  //   if(currentMeshFilter == null) {
  //     currentMeshFilter = gameObject.AddComponent<MeshFilter>();
  //   }
  //   Mesh mesh = currentMeshFilter.sharedMesh;
  //   if(mesh == null) {
  //     mesh = new Mesh();
  //     currentMeshFilter.sharedMesh = mesh;
  //   } else {
  //     mesh.Clear();
  //   }
  //   mesh.indexFormat =  UnityEngine.Rendering.IndexFormat.UInt32;
  //   mesh.SetVertices(Vertices);
  //   mesh.SetTriangles(Triangles,0, true);
  //   mesh.RecalculateNormals();
  // }


	public class Edge {
		public int[] vertexIndices;

		public Edge (int[] vertexIndices) {
			this.vertexIndices = vertexIndices;
		}
	}

}