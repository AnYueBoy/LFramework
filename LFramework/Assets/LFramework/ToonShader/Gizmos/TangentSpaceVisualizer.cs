using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TangentSpaceVisualizer : MonoBehaviour
{
    [SerializeField] private ShowType showType;

    private void OnDrawGizmos()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh)
            {
                ShowTangentSpace(mesh);
            }
        }
    }

    private void ShowTangentSpace(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        for (int i = 0; i < vertices.Length; i++)
        {
            ShowTangentSpace(
                transform.TransformPoint(vertices[i]),
                transform.TransformDirection(normals[i]),
                transform.TransformDirection(tangents[i]),
                tangents[i].w
            );
        }
    }

    public float offset = 0.01f;
    public float scale = 0.1f;

    private void ShowTangentSpace(Vector3 vertex, Vector3 normal, Vector3 tangent, float binormalSign)
    {
        if (showType == ShowType.None)
        {
            return;
        }

        vertex += normal * offset;
        if (showType == ShowType.ALL || showType == ShowType.NORMAL)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(vertex, vertex + normal * scale);
        }

        if (showType == ShowType.ALL || showType == ShowType.TANGENT)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(vertex, vertex + tangent * scale);
        }

        if (showType == ShowType.ALL || showType == ShowType.BINTANGENT)
        {
            Vector3 binormal = Vector3.Cross(normal, tangent) * binormalSign;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(vertex, vertex + binormal * scale);
        }
    }

    private enum ShowType
    {
        None,
        ALL,
        NORMAL,
        TANGENT,
        BINTANGENT,
    }
}