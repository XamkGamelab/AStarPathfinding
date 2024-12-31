using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node:IHeapItem<Node>
{
    public GameObject renderedNode;
    public bool walkable; // is the node on an obstacle or not
    public Vector3 worldPosition;
    public int gridX, gridY;
    public int movementPenalty;
    public int gCost, hCost;
    public Node parent;
    int heapIndex;
    public Node(bool _walkable, Vector3 _worldPosition, int _gridX, int _gridY, int _penalty, GameObject _renderedNode)
    {
        walkable = _walkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty;
        renderedNode = _renderedNode;
    }

    public int fCost { get { return gCost + hCost; } }

    public int HeapIndex { get { return heapIndex; }set { heapIndex = value; } }

    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if(compare == 0)
        {
            compare=hCost.CompareTo(other.hCost);

        }
        return -compare;
    }
}
