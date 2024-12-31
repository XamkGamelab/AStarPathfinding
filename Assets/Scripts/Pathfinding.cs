using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class Pathfinding : MonoBehaviour
{
    //public Transform seeker, target;
    Queue<NodeUpdate> toUpdate = new Queue<NodeUpdate>();
    NodeGrid grid;
    float timer = 0;
    private void Awake()
    {
        grid = GetComponent<NodeGrid>();
    }
    private void FixedUpdate()
    {
        // since the method i'm using is Not Very frame-by-frame friendly, we're instead doing some queues to visualise the pathfinder itself!
        // unfortunately the low resolution makes the pathfinding with the weights really janky
        if (timer > 0.01f)
        {
            if (toUpdate.Count > 0)
            {
                UpdateNodeRenders(toUpdate.Dequeue());
            }
            timer = 0;
        }
        else
            timer += Time.deltaTime;
    }

    public void FindPath(PathRequest request, Action<PathResult> callback)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(request.pathStart);
        Node targetNode = grid.NodeFromWorldPoint(request.pathEnd);

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);
            toUpdate.Enqueue(new NodeUpdate(startNode,0));
            toUpdate.Enqueue(new NodeUpdate(targetNode, 4));
            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                if (currentNode == targetNode)
                {
                    sw.Stop();
                    print("Path found: " + sw.ElapsedMilliseconds + " ms");
                    pathSuccess = true;
                    toUpdate.Enqueue(new NodeUpdate(targetNode, 4));
                    break;
                }
                if (currentNode.parent != null){
                    toUpdate.Enqueue(new NodeUpdate(currentNode, 2));
                }
                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour)) { continue; }
                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;
                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                            toUpdate.Enqueue(new NodeUpdate(neighbour, 1));
                        }
                        else
                        {
                            openSet.UpdateItem(neighbour);
                        }
                        toUpdate.Enqueue(new NodeUpdate(neighbour, 3));
                    }
                }
            }
        }
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
            pathSuccess = waypoints.Length > 0;
        }
        callback(new PathResult(waypoints, pathSuccess, request.callback));
    }
    void UpdateNodeRenders(NodeUpdate node)
    {
        // case 0: start node
        // case 1: open
        // case 2: explored
        // case 3: text update
        // case 4: end node
        // case 5: final path
        switch (node._case)
        {
            case 0:
                node.node.renderedNode.GetComponent<MeshRenderer>().material = node.node.renderedNode.GetComponent<NodeRenderer>().Start;
                node.node.renderedNode.GetComponent<NodeRenderer>().GCost.text = "";
                node.node.renderedNode.GetComponent<NodeRenderer>().HCost.text = "";
                node.node.renderedNode.GetComponent<NodeRenderer>().FCost.text = "Start";
                break;
            case 1:
                node.node.renderedNode.GetComponent<MeshRenderer>().material = node.node.renderedNode.GetComponent<NodeRenderer>().Open;
                break;
            case 2:
                node.node.renderedNode.GetComponent<MeshRenderer>().material = node.node.renderedNode.GetComponent<NodeRenderer>().Explored;
                break;
             case 3:
                node.node.renderedNode.GetComponent<NodeRenderer>().UpdateNodeText(node.node);
                break;
                case 4:
                    node.node.renderedNode.GetComponent<MeshRenderer>().material = node.node.renderedNode.GetComponent<NodeRenderer>().End;
                node.node.renderedNode.GetComponent<NodeRenderer>().GCost.text = "";
                node.node.renderedNode.GetComponent<NodeRenderer>().HCost.text = "";
                node.node.renderedNode.GetComponent<NodeRenderer>().FCost.text = "End";
                break;
                case 5:
                node.node.renderedNode.GetComponent<MeshRenderer>().material = node.node.renderedNode.GetComponent<NodeRenderer>().Final;
                break;
            default:
                break;
        }
    }
    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        foreach (Node node in path)
        {
            toUpdate.Enqueue(new NodeUpdate(node, 5));
        }
        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;

    }

    Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;
        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }

    int GetDistance(Node a, Node b)
    {
        // lower number is how many diagonals are needed
        // higher-lower = how many horizontals are needed
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    struct NodeUpdate
    {
        public Node node;
        public int _case;
        public NodeUpdate(Node _node, int __case)
        {
            this.node = _node;
            this._case = __case;
        }
    }

}
