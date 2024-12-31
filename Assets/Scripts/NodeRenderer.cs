using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeRenderer:MonoBehaviour
{
    public TextMesh FCost;
    public TextMesh GCost;
    public TextMesh HCost;
    public Material Unexplored, Explored, Open, Start, End, Obstacle, Final;
    public void UpdateNodeText(Node nodeToUpdate)
    {

        FCost.text = nodeToUpdate.fCost.ToString();

        GCost.text = nodeToUpdate.gCost.ToString();

        HCost.text = nodeToUpdate.hCost.ToString();


    }
}
