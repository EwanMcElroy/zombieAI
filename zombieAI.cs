using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//programmed by Ewan McElroy

public class zombieAI : MonoBehaviour
{
    #region variables
    private int nodeIndex = 0;
    [SerializeField] private float maxSpeed = 0.01f;
    [SerializeField] private float nodeRadius = 1f;
    [SerializeField] private float soundDist = 1.0f;
    [SerializeField] private float maxCOuntDown = 3;
    [SerializeField] private float rayDist = 3;
    [SerializeField] private float wanderDist;
    [SerializeField] private float maxWanderCount = 2;
    [SerializeField] private float seekDist = 1.0f;
    private float counter;
    private float wanderCounter;

    private Rigidbody rb;

    [SerializeField] private GameObject[] nodes;
    private GameObject player;
    private GameObject zombieClosest;
    private GameObject playerClosest;
    private List<GameObject> path = new List<GameObject>();
    //private List<GameObject> pickList = new List<GameObject>();
    //private List<GameObject> potentialDirection = new List<GameObject>();

    private bool pathFound;

    private Vector3 wanderLocation;
    #endregion

    [HideInInspector] public enum aiState // ai Enum
    {
        IDLE,
        PATHFIND,
        FOLLOWPATH,
        CHASEPLAYER
    };

    [HideInInspector] public aiState behaviourState = aiState.IDLE;

    private void Start()
    {
        // ininitialisation
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player");
        nodes = GameObject.FindGameObjectsWithTag("Nodes");
        counter = maxCOuntDown;
        wanderCounter = maxWanderCount;
        StateChecker();
    }
    void StateChecker()
    {
        switch (behaviourState)
        {
            case (aiState.IDLE):
                {
                    idle();
                    break;
                }
            case (aiState.PATHFIND):
                {
                    Debug.Log("Pathing!");
                    nonNavMesh();
                    break;
                }
            case (aiState.FOLLOWPATH):
                {
                    seek(path[nodeIndex].transform.position);
                    break;
                }
            case (aiState.CHASEPLAYER):
                {
                    counter -= Time.deltaTime;
                    if (counter <= 0 && Vector3.Distance(transform.position, player.transform.position) > seekDist)
                    {
                        counter = maxCOuntDown;
                        path.Clear();
                        pathFound = false;
                        behaviourState = aiState.PATHFIND;
                    }
                    seek(player.transform.position);
                    break;
                }
        }

        Invoke("StateChecker", 0.0f);
    }

    private void idle()
    {
        if(player.GetComponent<AudioSource>().isPlaying && Vector3.Distance(player.transform.position, transform.position) < soundDist) // if a sound plays
        {
            behaviourState = aiState.PATHFIND; // find the player
        }
        wanderCounter -= Time.deltaTime;
        if (wanderCounter <= 0)
        {
            wanderLocation = findWanderLocation();
            wanderCounter = maxWanderCount;
        }
        seek(wanderLocation);
    }

    private Vector3 findWanderLocation()
    {
        RaycastHit hit;
        float minX, maxX, minZ, maxZ, xPos, zPos; 
        if(Physics.Raycast(transform.position, transform.TransformDirection(transform.forward), out hit, rayDist))
        {
            Debug.Log("hit");
            minX = transform.forward.x - 1;
            maxX = transform.forward.x + 1;
            minZ = transform.forward.z - wanderDist;
            maxZ = transform.forward.z - (wanderDist + 1);

            xPos = UnityEngine.Random.Range(minX, maxX);
            zPos = UnityEngine.Random.Range(minZ, maxZ);
        }
        else
        {
            Debug.Log("Not hit");
            minX = transform.forward.x - 1;
            maxX = transform.forward.x + 1;
            minZ = transform.forward.z + wanderDist;
            maxZ = transform.forward.z + (wanderDist + 1);

            xPos = UnityEngine.Random.Range(minX, maxX);
            zPos = UnityEngine.Random.Range(minZ, maxZ);
        }
        return new Vector3(xPos, transform.position.y, zPos);
    }

    private void nonNavMesh()
    {
        if (!pathFound) // if a path hasn't been found
        {
            if (path.Count == 0)
            {
                zombieClosest = findClosestNodetoObject(this.gameObject); // find closest node to zombie
                playerClosest = findClosestNodetoObject(player); // find closest node to player
                path.Add(zombieClosest); // add the closest to path
                if(zombieClosest.name == playerClosest.name)
                {
                    behaviourState = aiState.CHASEPLAYER;
                }
            }

            //potentialDirection is now a local list, cleans up code in memory.
            List<GameObject> potentialDirection = new List<GameObject>();
            potentialDirection = path[path.Count - 1].GetComponent<nodeScript>().attachedNodes; // gets attached nodes ** NOTE ** Getcomponents kill performance....

            if (potentialDirection.Count == 1) // if only one attached node
            {
                path.Add(potentialDirection[0]);  // add the node
            }
            else
            {
                List<GameObject> pickList = new List<GameObject>();
                //pickList.Clear(); // clear the  pick list
                for (int i = 0; i < potentialDirection.Count; i++)
                {
                    if (!path.Contains(potentialDirection[i])) // if the potential node is not on the path
                    {
                        pickList.Add(potentialDirection[i]); // add to pick list
                    }
                }

                GameObject addItem = null;
                float distance = Mathf.Infinity;
                for (int i = 0; i < pickList.Count; i++)
                {
                    Vector3 dist = pickList[i].transform.position - playerClosest.transform.position; // subtract the position of the pick list form the closest node

                    float cDist = dist.sqrMagnitude; // calulate the square mag

                    if (cDist < distance) // if the new distance is shorter than the old distance
                    {
                        addItem = pickList[i]; // set current path add
                        distance = cDist; // se new shortest distance
                    }

                }
                path.Add(addItem); // add item to path
            }
            if (path[path.Count - 1] == playerClosest) // if the last item on the path is the closest node to player
            {
                pathFound = true; // set path to found
            }
        }
        if (pathFound)
        {
            behaviourState = aiState.FOLLOWPATH; // change AI state
        }
    }

    private void seek(Vector3 _target)
    {
        Vector3 targetPos = _target; // get target position

        Vector3 dVelocity = Vector3.Normalize(targetPos - transform.position) * maxSpeed;  //  calulate desired velocity

        Vector3 steer = dVelocity - rb.velocity; // calulate steering vector 

        rb.velocity += steer; // add steering vector to velocity

        if(steer.sqrMagnitude > 0.0f)
        {
            transform.forward = Vector3.Normalize(new Vector3(steer.x, 0, steer.z));
        }

        if (behaviourState == aiState.FOLLOWPATH)
        {
            if (Vector3.Distance(transform.position, path[nodeIndex].transform.position) < nodeRadius)
            {
                if (nodeIndex >= path.Count - 1)
                {
                    behaviourState = aiState.CHASEPLAYER;
                }
                else
                {
                    nodeIndex++;
                }
            }
        }
    }

    private GameObject findClosestNodetoObject(GameObject _object)
    {
        int closest = -1;
        for (int i = 0; i < nodes.Length; i++)
        {
            if(closest == -1)
            {
                closest = i;
            }
            else
            {
                if(Vector3.Distance(_object.transform.position, nodes[i].transform.position) < Vector3.Distance(_object.transform.position, nodes[closest].transform.position))
                {
                    closest = i;
                }
            }
        }
        return nodes[closest];
    }
}
