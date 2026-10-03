using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] GameObject horse;
    [SerializeField] GameObject chair;
    public void TestSpawner()
    {
        GameObject obj = new GameObject();
        GameObject hor = Instantiate(horse, new Vector3(0,0,0), Quaternion.identity, obj.transform);
        GameObject ch = Instantiate(chair, new Vector3(0,1,0), Quaternion.identity, obj.transform);
    }
}
