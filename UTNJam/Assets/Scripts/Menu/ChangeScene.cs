using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public void ChangeToScene()
    {
        SceneManager.LoadScene("prototipo");
    }
}
