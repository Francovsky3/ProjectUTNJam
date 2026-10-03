using System.Collections.Generic;
using UnityEngine;


public class PartsState : GameState
{
    [Header("Object spawning")]
    [SerializeField] List<GameObject> objects;
    [SerializeField] float minPosOffset = 5;
    [SerializeField] float maxPosOffset = 20;
    [SerializeField]Transform spawnPoint;
    int objectAmount;

    void Start()
    {
        cam.transform.position = cameraPos.transform.position;
        objectAmount = Random.Range(3,6);
        for(int i = 0; i < objectAmount; i++)
        {
            Vector3 spawnOffset = new Vector3(Random.Range(minPosOffset, maxPosOffset), Random.Range(minPosOffset, maxPosOffset), 0);

            Instantiate(objects[Random.Range(0, objects.Count -1)], spawnPoint.transform.position + spawnOffset, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
