using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public class Room
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject chestPrefab;
    public GameObject enemyPrefab;

    [SerializeField] public int dungeonWidth = 64;
    [SerializeField] public int dungeonHeight = 64;
    [SerializeField] public int roomMinSize = 6;
    [SerializeField] public int roomMaxSize = 12;
    public int maxIterations = 5;

    public List<Room> rooms;
    public List<Room> corridors;
    // Start is called before the first frame update
    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        rooms = new List<Room>();
        corridors = new List<Room>();

        BSP(maxIterations, new Rect(0, 0, dungeonWidth, dungeonHeight));

        foreach (Room room in rooms)
        {
            CreateRoom(room);
        }

        foreach (Room corridor in corridors)
        {
            CreateRoom(corridor);
        }

        BuildWalls();
    }

    void BSP(int iterations, Rect area)
    {
        if (iterations == 0 || (area.width <= roomMinSize && area.height <= roomMinSize))
        {
            int width = Random.Range(roomMinSize, (int)area.width);
            int height = Random.Range(roomMinSize, (int)area.height);
            int x = (int)area.x + Random.Range(0, (int)(area.width - width));
            int y = (int)area.y + Random.Range(0, (int)(area.height - height));
            rooms.Add(new Room(x, y, width, height));
        }
        else
        {
            // Impartire pe orizontala sau verticala a spatiului
            bool splitHorizontal = Random.value > 0.5f;

            if (area.width / area.height >= 1.25f)
            {
                splitHorizontal = false;
            }
            else if (area.height / area.width >= 1.25f)
            {
                splitHorizontal = true;
            }

            if (splitHorizontal)
            {
                int split = Random.Range((int)area.x + roomMinSize, (int)area.x - roomMinSize);
                BSP(iterations - 1, new Rect(area.x, area.y, split - area.x, area.height));
                BSP(iterations - 1, new Rect(split, area.y, area.xMax - split, area.height));
                corridors.Add(new Room(split - 1, (int)area.y, 2, (int)area.height));
            }
            else
            {
                int split = Random.Range((int)area.y + roomMinSize, (int)area.y - roomMinSize);
                BSP(iterations - 1, new Rect(area.x, area.y, area.width, split - area.y));
                BSP(iterations - 1, new Rect(area.x, split, area.width, area.yMax - split));
                corridors.Add(new Room((int)area.x, split - 1, (int)area.width, 2));
            }
        }
    }

    void CreateRoom(Room room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                if (Random.value < 0.1f) // Chance to spawn a floor tile
                    continue; // Skip some tiles to create variation
                Instantiate(floorPrefab, transform.position + new Vector3(x, 0.1f, y), Quaternion.Euler(90, 0, 0));
                if (Random.value < 0.0001f) // Chance to spawn a chest
                {
                    Instantiate(chestPrefab, transform.position + new Vector3(x, 0.11f, y), Quaternion.identity);
                }
                if (Random.value < 0.0005f) // Chance to spawn an enemy
                {
                    Instantiate(enemyPrefab, transform.position + new Vector3(x, 0.11f, y), Quaternion.identity);
                }
            }
        }
    }

    void BuildWalls()
    {
        List<Vector3> floorTiles = new List<Vector3>();
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 0, 0), // right
            new Vector3(-1, 0, 0), // left
            new Vector3(0, 0, 1), // up
            new Vector3(0, 0, -1) // down
        };

        foreach(GameObject floor in GameObject.FindGameObjectsWithTag("Floor"))
        {
            floorTiles.Add(floor.transform.position);
        }

        foreach(Vector3 tile in floorTiles)
        {
            foreach(Vector3 direction in directions)
            {
                Vector3 neighbor = tile + direction;
                if (!floorTiles.Contains(neighbor))
                {
                    Instantiate(wallPrefab, new Vector3(neighbor.x, 1.0f, neighbor.z), Quaternion.identity);
                    Instantiate(wallPrefab, new Vector3(neighbor.x, 3.0f, neighbor.z), Quaternion.identity);
                }
            }
        }
    }
}
