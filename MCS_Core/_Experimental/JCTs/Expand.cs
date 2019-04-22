//using UnityEngine;
//using System.Collections.Generic;
//
//[ExecuteInEditMode]
//public class Expand : MonoBehaviour
//{
//	public float m_expand;
//	private float m_lastExpand;
//
//	SkinnedMeshRenderer m_meshRender;
//
//	private SkinnedMeshRenderer MeshRender
//	{
//		get
//		{
//			if (m_meshRender)
//				return m_meshRender;
//			SkinnedMeshRenderer meshRender = transform.GetComponentInChildren(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
//			m_meshRender = meshRender;
//			return meshRender;
//		}
//	}
//
//	void Awake()
//	{
//	}
//
//
//	void Update()
//	{
//		if (m_expand != m_lastExpand)
//		{
//			SkinnedMeshRenderer meshRender = MeshRender;
//			Mesh mesh = meshRender.sharedMesh;
//
//			Vector3[] vertices = mesh.vertices;
//			Vector3[] normals = mesh.normals;
//			Vector3[] deltas = null;
//
//			if (deltas == null || deltas.Length == 0)
//				deltas = new Vector3[vertices.Length];
//
//			for(int i = 0; i < deltas.Length; i++)
//				deltas[i] = normals[i] * m_lastExpand * 0.001f;
//
//			for(int i = 0; i < deltas.Length; i++)
//				vertices[i] -= deltas[i];
//
//			for(int i = 0; i < deltas.Length; i++)
//				deltas[i] = normals[i] * m_expand * 0.001f;
//
//			for(int i = 0; i < deltas.Length; i++)
//				vertices[i] += deltas[i];
//
//			mesh.vertices = vertices;
//			m_lastExpand = m_expand;
//		}
//	}
//}
//
