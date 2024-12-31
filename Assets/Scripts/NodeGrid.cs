using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
    [SerializeField]
    GameObject nodeTemplate;
    //public bool pathGizmosOnly;
    public bool displayGridGizmos;
    //public Transform player;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public TerrainType[] walkableRegions;
    public int obstacleProximityPenalty = 10;
    LayerMask walkableMask;
    Dictionary<int,int> walkableRegionsMap = new Dictionary<int,int>();
    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;
    private void Awake()
    {
        //calculate node amount
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        foreach(TerrainType region in walkableRegions)
        {
            // we're using layers to mark unwalkable regions + movement penalties
            // unity masks are stored in a 32 bit int, with each bit representing one of the 32 masks. we do bitwise OR to get a mask with all our walkable layers.
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsMap.Add(Mathf.RoundToInt(Mathf.Log(region.terrainMask.value,2)),region.terrainPenalty);
        }
        CreateGrid();
    }
    public int MaxSize {  get { return gridSizeX*gridSizeY; } }
    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                int penalty = 0;
                // raycast walkables, don't need weights for unwalkable areas

                    Ray ray = new Ray(worldPoint+Vector3.up*50,Vector3.down);
                    RaycastHit hit;
                    if(Physics.Raycast(ray, out hit, 100, walkableMask))
                    {
                        walkableRegionsMap.TryGetValue(hit.collider.gameObject.layer,out penalty);
                    }
                if (!walkable)
                    penalty += obstacleProximityPenalty;
                GameObject node = Instantiate(nodeTemplate, new Vector3(worldPoint.x,0.1f,worldPoint.z), nodeTemplate.transform.rotation);
                node.transform.localScale =new Vector3(nodeRadius/5,1,nodeRadius/5);
                node.GetComponent<MeshRenderer>().material = node.GetComponent<NodeRenderer>().Unexplored;
                if(!walkable)
                    node.GetComponent<MeshRenderer>().material = node.GetComponent<NodeRenderer>().Obstacle;
                grid[x, y] = new Node(walkable, worldPoint, x, y,penalty,node);
            }
        }
        // what how did i break this..
        print("Calling map blur!");
        BlurPenaltyMap(3);
        print("Map blur finished!");
    }
    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        print("Kernel size is " + kernelSize);
        int kernelExtents = blurSize;
        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX,gridSizeY];
        print("Pass arrays created");
        for(int y=0;y < gridSizeY; y++)
        {
            for(int x=-kernelExtents; x<=kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x,0,kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }
            for(int x =1;x<gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1,0,gridSizeX);
                int addIndex=Mathf.Clamp(x+kernelExtents,0,gridSizeX-1);
                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x-1,y]-grid[removeIndex,y].movementPenalty+grid[addIndex,y].movementPenalty;
            }
        }
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }
            // these lines are for getting rid of the unblurred line at the bottom
            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty =Mathf.RoundToInt((float)penaltiesVerticalPass[x, y]/(kernelSize*kernelSize));
                grid[x,y].movementPenalty = blurredPenalty;

                if(blurredPenalty>penaltyMax)
                    penaltyMax = blurredPenalty;
                if(blurredPenalty<penaltyMin) penaltyMin = blurredPenalty;
            }
        }
    }
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    neighbours.Add(grid[checkX, checkY]);
            }
        }
        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt(gridSizeX * percentX-1);
        int y = Mathf.RoundToInt(gridSizeY * percentY-1);

        return grid[x, y];
    }
    //public List<Node> path;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null && displayGridGizmos)
        {
            //Node playerNode = NodeFromWorldPoint(player.position);
            foreach (Node n in grid)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.black,Mathf.InverseLerp(penaltyMin,penaltyMax,n.movementPenalty));
                Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
            }
        }
    }
    [System.Serializable]
    public class TerrainType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }

}
