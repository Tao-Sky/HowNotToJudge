﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions.Comparers;

public class Gamasutra : MonoBehaviour
{
    [SerializeField] private GameObject corridor;   // prefab of the corridor

    public List<GameObject> rooms;  // type of rooms possibles
    List<GameObject> lvlRooms = new List<GameObject>();
    List<Vector3> lastPos = new List<Vector3>();    // to check if the elements are always in movement

    public int nbRoom;
    private float tileSizeX = 16f;
    private float tileSizeY = 9f;

    private List<GameObject> listBufferHorizontal;  // to replace the elements on a "grid"
    private List<GameObject> listBufferVertical;    // to replace the elements on a "grid"

    private bool align = false;
    private bool startAgencement = false;
    private float percentRoom = 0.8f;

    [Header("Load")]
    [SerializeField]
    private GameObject loadingScreen;   // prefab of the corridor


    public List<GameObject> roomsBefore;
    public List<GameObject> roomAfter;
    void Awake()
    {
        _instance = this;
    }

    public static Gamasutra instance
    {
        get
        {
            return _instance;
        }
    }
    private static Gamasutra _instance;

    void initialize()
    {
        loadingScreen.SetActive(true);

        while (lvlRooms.Count != 0)
        {
            GameObject go = lvlRooms[lvlRooms.Count - 1];
            lvlRooms.Remove(go);
            Destroy(go);
        }
        while (lastPos.Count != 0)
        {
            lastPos.Remove(lastPos[lastPos.Count-1]);
        }

        listBufferHorizontal.Clear();
        listBufferVertical.Clear();
       
        align = false;
        startAgencement = false;
    }

    // Use this for initialization
    void Start ()
	{
        Debug.Log("START");
        //loadingScreen.SetActive(true);
        createLevel();
        
    }

    void createLevel()
    {

        createRooms(nbRoom);                    // create several rooms on the map in a specific perimeter
        
        // add physics to place the elements next to other
        foreach (GameObject go in lvlRooms)     
        {
            if (!go.GetComponent<Rigidbody2D>())
            {
                go.AddComponent<Rigidbody2D>();
            }
            go.GetComponent<Rigidbody2D>().gravityScale = 0;
            go.GetComponent<Rigidbody2D>().freezeRotation = true;
        }
        // (initialiaze) get the place of the elements to check if they are moving or not, to launch the following code      
        for (int i = 0; i < lvlRooms.Count; i++)
        {
            lastPos.Add(Vector3.zero);
        }
        // duplicate the lists of rooms to align them in a "grid" in a horizontal and vertical way
        listBufferHorizontal = new List<GameObject>(lvlRooms);
        listBufferVertical = new List<GameObject>(lvlRooms);
        startAgencement = true;

    }

    void LateUpdate()
    {
        int cpt = 0;
        // check if all the elements are align
        if (!align && startAgencement)
        {
            // check all the elemnts are not moving
            for (int i = 0; i < lvlRooms.Count; i++)
            {
                if (lastPos[i] != lvlRooms[i].GetComponent<Transform>().position)
                {
                    cpt++;
                }
                lastPos[i] = (lvlRooms[i].GetComponent<Transform>().position);
            }
            // if any elements move anymore
            if (cpt == 0)
            {
                align = true;                   // when every alement are align
                alignRooms();                   // align the element on a grid
                checkAttainable();              // check if all elements are attainable and remove the non attainable
                
            }
        }

    }

    GameObject getFirst()
    {
        // check all the elements ths most on the left and get the second, to allow player to go up in the game
        float xMin = Mathf.Infinity;
        for (int i = 0; i < lvlRooms.Count; i++)
        {
            // get the point on the max left
            float x = lvlRooms[i].GetComponent<Transform>().position.x - lvlRooms[i].GetComponent<GamasutraRoom>().getXY().x/2;
            if (x < xMin)
            {
                xMin = x;
            }
        }

        int cptMin = 0;         // get the nb of room on the most on left
        for (int i = 1; i < lvlRooms.Count; i++)
        {
            cptMin++;
        }
        // get the first and the second gameobject on the Y axis
        GameObject firstY = null;
        GameObject secondY = null;
        bool instanciateGameObject = false;
        for (int i = 0; i < lvlRooms.Count; i++)
        {
            float x = lvlRooms[i].GetComponent<Transform>().position.x - lvlRooms[i].GetComponent<GamasutraRoom>().getXY().x/2;
            float y = lvlRooms[i].GetComponent<Transform>().position.y;
            if (x == xMin)
            {
                if (!instanciateGameObject)
                {
                    firstY = lvlRooms[i];
                    secondY = lvlRooms[i];
                    instanciateGameObject = true;
                }
                else
                {
                     if (lvlRooms[i].GetComponent<Transform>().position.y > secondY.GetComponent<Transform>().position.y)
                    {
                        firstY = lvlRooms[i];
                    }
                    else if (lvlRooms[i].GetComponent<Transform>().position.y > firstY.GetComponent<Transform>().position.y)
                    {
                        secondY = lvlRooms[i];
                    }
                }
            }
        }
        // if at least 2 min, get the second, else get the first
        GameObject firstRoom;
        if (cptMin > 1)
        {
            firstRoom = secondY;
        }
        else
        {
            firstRoom = firstY;
        }
        return firstRoom;
    }

    /*
    *@brief : check if all the elements are attainable
    */
    void checkAttainable()
    {
        // start on the element which will be the enter (at the right of the corridor)
        GameObject first = getFirst(); 
        roomsBefore = new List<GameObject>(lvlRooms);
        roomAfter = new List<GameObject>();
        // go on the elements that are accessible
        first.GetComponent<GamasutraRoom>().getAttainable(); 
        // if attainable elements are > 80% ok else restart
        int restRooms = roomAfter.Count;
        Debug.Log("RESTART ---" + roomAfter.Count + " " + roomsBefore.Count + " " + (int)(percentRoom * nbRoom));
        if (restRooms < (int) (percentRoom*nbRoom))
        {
            initialize();
            createLevel();
        }
        else
        {
            while (roomsBefore.Count!=0)
            {
                // destroy the rooms which are not in the flow of rooms
                GameObject go = roomsBefore[roomsBefore.Count-1];
                roomsBefore.Remove(go);
                Destroy(go);
            }
            // add the corridor
            GameObject c = addCorridor(first);

            // enable all the colliders of the roof/ground/walls
            for (int i = 0; i < roomAfter.Count; i++)
            {
                roomAfter[i].GetComponent<GamasutraRoom>().activeBorderCollider();
            }
            c.GetComponent<BoxCollider2D>().enabled = false;
            if (GameManager.instance != null)
            {
                GameManager.instance.getPlayerGameObject().transform.position =GameObject.Find("spawnPlayer").transform.position;
                GameObject player = Instantiate(GameManager.instance.getPlayerGameObject());
                //tra,sform the local position in world position
                // player.transform.position = GameObject.Find("spawnPlayer").transform.position;
                GameManager.instance.setCurrentRoom(c);
                GameObject.Find("Main Camera").GetComponent<CamFollow>().firstPositionCorridor(c); //utile ?
                //player.transform.SetParent(this.gameObject.transform);
                GameObject.Find("Main Camera").SetActive(true);
                GameObject.Find("Main Camera").GetComponent<CamFollow>().player = player.transform;
                GameObject.Find("MiniMapCamera").GetComponent<CamFollow>().player = player.transform;
                GameObject.Find("MiniMapCamera").GetComponent<CamFollow>().setMinimap(true);
                loadingScreen.SetActive(false);
                GameManager.instance.getAudioManager().LaunchTheme();
            }
        }
    }

    void alignRooms()
    {
        // remove the collider on the limit of the room in order to align them without physic
        foreach (GameObject l in lvlRooms)
        {
            l.GetComponent<BoxCollider2D>().enabled = false;
            l.GetComponent<Rigidbody2D>().isKinematic = true;
        }
        // align on the horizontal way
        while (listBufferHorizontal.Count > 0)
        {
            GameObject higher = listBufferHorizontal[0];
            for (int i = 0; i < listBufferHorizontal.Count; i++)
            {
                if (listBufferHorizontal[i].GetComponent<Transform>().position.x < higher.GetComponent<Transform>().position.x)
                {
                    higher = listBufferHorizontal[i];
                }
            }
            float xOffset = higher.GetComponent<GamasutraRoom>().getXY().x / 2;
            float x = higher.GetComponent<Transform>().localPosition.x - xOffset;

            float y = higher.GetComponent<Transform>().localPosition.y;
            float z = higher.GetComponent<Transform>().localPosition.z;

            bool positif = (x >= 0);
            x = Mathf.Abs(x);
            float xx = Mathf.Floor(x / tileSizeX);
            if (x - xx * tileSizeX > tileSizeX / 2.0f)
                xx++;
            float xxx = xx * tileSizeX;
            if (!positif)
                xxx = -xxx;

            higher.GetComponent<Transform>().localPosition = new Vector3(xxx + xOffset, y, z);

            listBufferHorizontal.Remove(higher);

        }
        // align on the vertical way
        while (listBufferVertical.Count > 0)
        {
            GameObject higher = listBufferVertical[0];
            for (int i = 0; i < listBufferVertical.Count; i++)
            {
                if (listBufferVertical[i].GetComponent<Transform>().position.y > higher.GetComponent<Transform>().position.y)
                {
                    higher = listBufferVertical[i];
                }
            }
            float yOffset = higher.GetComponent<GamasutraRoom>().getXY().y / 2;

            float x = higher.GetComponent<Transform>().localPosition.x;
            float y = higher.GetComponent<Transform>().localPosition.y + yOffset;
            float z = higher.GetComponent<Transform>().localPosition.z;

            bool positif = (y >= 0);
            y = Mathf.Abs(y);
            float yy = Mathf.Floor(y / tileSizeY);
            if (y - yy * tileSizeY > tileSizeY / 2.0f)
                yy++;
            float yyy = yy * tileSizeY;
            if (!positif)
                yyy = -yyy;


            higher.GetComponent<Transform>().localPosition = new Vector3(x, yyy - yOffset, z);
            listBufferVertical.Remove(higher);
        }

        foreach (GameObject l in lvlRooms)
        {
            l.GetComponent<GamasutraRoom>().getColliderSupperpose().enabled = true; // enable the little
        }
        Debug.Log("COUNT : "+lvlRooms.Count);
        for (int i = 0; i < lvlRooms.Count; i++)
        {
           lvlRooms[i].GetComponent<GamasutraRoom>().checkSupperpose(lvlRooms);   // check if the room is superpose and alone
        }
        for (int i = 0; i < lvlRooms.Count; i++)
        {
            lvlRooms[i].GetComponent<GamasutraRoom>().checkSides(lvlRooms);   // check if the room is superpose and alone
        }
       
    }

    void createRooms(int nbRooms)
    {
        List<GameObject> listPotentialShopRooms = new List<GameObject>();

        for (int i = 0; i < nbRooms; i++)
        {
            GameObject s = (GameObject) Instantiate(rooms[Random.Range(0, rooms.Count)], this.transform);
            s.GetComponent<Transform>().localPosition = getRandomPointInEllipse(90,2);//getRandomPointInCircle(1.0f);
            lvlRooms.Add(s);

            if (s.name == "1x1(Clone)")
            {
                listPotentialShopRooms.Add(s);
            }
        }

        if (listPotentialShopRooms.Count > 0)
        {
            int i = Random.Range(0, listPotentialShopRooms.Count);
            listPotentialShopRooms[i].GetComponent<GamasutraRoom>().setIsShopRoom(true);
        }
    }

    GameObject addCorridor(GameObject first)
    {
        GameObject c = (GameObject)Instantiate(corridor);
        c.transform.SetParent(this.gameObject.transform);
        float x = first.GetComponent<Transform>().position.x - first.GetComponent<GamasutraRoom>().getXY().x/2;
        float newY = first.GetComponent<Transform>().position.y;
        // if the height is 2, align the corridor on the top or on the bottom in random
        if (first.GetComponent<GamasutraRoom>().getHeight() > 1)
        {
            int random = Random.Range(0, 2);
            if (random == 0) // on top
            {
                newY += first.GetComponent<GamasutraRoom>().getXY().y/4;
                // remove room from corridor to first room
                //first.GetComponent<GamasutraRoom>().doorLT.SetActive(false);
                foreach (SpriteRenderer g in first.GetComponent<GamasutraRoom>().doorLT.GetComponentsInChildren<SpriteRenderer>())
                {
                    g.enabled = false;
                    g.GetComponent<BoxCollider2D>().isTrigger = true;
                    g.gameObject.AddComponent<DiscoverRoom>();
                }
            }
            // on bottom
            else
            {
                newY -= first.GetComponent<GamasutraRoom>().getXY().y/4;
                // remove room from corridor to first room
                //first.GetComponent<GamasutraRoom>().doorLB.SetActive(false);
                foreach (SpriteRenderer g in first.GetComponent<GamasutraRoom>().doorLB.GetComponentsInChildren<SpriteRenderer>())
                {
                    g.enabled = false;
                    g.GetComponent<BoxCollider2D>().isTrigger = true;
                    g.gameObject.AddComponent<DiscoverRoom>();
                }
            }
        }
        else
        {
            // remove room from corridor to first room
            //first.GetComponent<GamasutraRoom>().doorL.SetActive(false);
            foreach (SpriteRenderer g in first.GetComponent<GamasutraRoom>().doorL.GetComponentsInChildren<SpriteRenderer>())
            {
                g.enabled = false;
                g.GetComponent<BoxCollider2D>().isTrigger = true;
                g.gameObject.AddComponent<DiscoverRoom>();
            }
        }
        float newX = x - c.GetComponent<BoxCollider2D>().bounds.size.x/2;
        
        float newZ = first.GetComponent<Transform>().position.z;

        c.GetComponent<Transform>().position=new Vector3(newX,newY,newZ);
        return c;

    }

    Vector2 getRandomPointInEllipse(float ellipse_width, float ellipse_height)
    {
        float t = 2 * Mathf.PI * Random.Range(0.0f, 1.0f);
        float u = Random.Range(0.0f, 1.0f) + Random.Range(0f, 1f);
        float r = 0;
        if (u > 1)
        {
            r = 2.0f - u;
        }
        else
        {
            r = u;
        }
        return new Vector2(roundm(ellipse_width * r * Mathf.Cos(t)/2, tileSizeX), roundm(ellipse_height * r * Mathf.Sin(t)/2, tileSizeY));

    }


    Vector2 getRandomPointInCircle(float radius)
    {
        float t = 2*Mathf.PI*Random.Range(0.0f,1.0f);
        float u = Random.Range(0.0f, 1.0f)+ Random.Range(0f, 1f);
        float r = 0;
        if (u > 1)
        {
            r = 2.0f - u;
        }
        else
        {
            r = u;
        }
        //return new Vector2(radius*r*Mathf.Cos(t),radius*r*Mathf.Sin(t));

        return new Vector2(roundm(radius*r*Mathf.Cos(t), tileSizeX), roundm(radius*r*Mathf.Sin(t),tileSizeY));
    }

    float roundm(float n , float  m )
    {
        return Mathf.Floor((n + m - 1)/m)*m;
    }

    // check if alone
    
}
