using UnityEngine;

public class ColeccionManager : MonoBehaviour
{
    public static ColeccionManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool DesbloquearObjeto(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (EstaDesbloqueado(id))
            return false;

        PlayerPrefs.SetInt("Coleccion_" + id, 1);
        PlayerPrefs.Save();

        Debug.Log("¡Nuevo objeto descubierto! " + id);

        return true;
    }

    public bool EstaDesbloqueado(string id)
    {
        return PlayerPrefs.GetInt("Coleccion_" + id, 0) == 1;
    }
}