using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] GameObject horse;
    [SerializeField] GameObject chair;
    [SerializeField] GameObject lamp;
    [SerializeField] Transform spawnpoint;
    public void TestSpawner()
    {
        GameObject obj = new GameObject();
        GameObject hor = Instantiate(horse, spawnpoint.position, Quaternion.identity, obj.transform);
        GameObject ch = Instantiate(chair, spawnpoint.position + new Vector3(0,1,0), Quaternion.identity, obj.transform);
    }
}
