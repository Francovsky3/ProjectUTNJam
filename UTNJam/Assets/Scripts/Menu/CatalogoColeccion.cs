using System.Collections.Generic;
using UnityEngine;

public class CatalogoColeccion : MonoBehaviour
{
    [SerializeField] private List<Objeto> objetos;

    public List<Objeto> Objetos => objetos;
}
