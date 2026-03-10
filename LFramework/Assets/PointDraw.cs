using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointDraw : MonoBehaviour
{
    [SerializeField] private List<GameObject> nodeList;

    private void OnDrawGizmos()
    {
        if (nodeList == null || nodeList.Count == 0)
        {
            return;
        }

        var originCol = Gizmos.color;
        Gizmos.color = Color.green;

        for (int i = 0; i < nodeList.Count - 1; i++)
        {
            var node = nodeList[i];
            var nextNode = nodeList[i + 1];
            Gizmos.DrawLine(node.transform.position, nextNode.transform.position);
        }

        Gizmos.DrawLine(nodeList[^1].transform.position, nodeList[0].transform.position);
        Gizmos.color = originCol;
    }
}