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

    public bool DesbloquearObjeto(string id) => Desbloquear(id);

    public bool EstaDesbloqueado(string id) => Desbloqueado(id);

    // Las versiones estáticas funcionan aunque no haya un ColeccionManager en la escena
    // (por ejemplo, si se da Play directo en el juego sin pasar por el menú)
    public static bool Desbloquear(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (Desbloqueado(id))
            return false;

        PlayerPrefs.SetInt("Coleccion_" + id, 1);
        PlayerPrefs.Save();

        Debug.Log("¡Nuevo objeto descubierto! " + id);

        return true;
    }

    public static bool Desbloqueado(string id)
    {
        return !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt("Coleccion_" + id, 0) == 1;
    }
}
