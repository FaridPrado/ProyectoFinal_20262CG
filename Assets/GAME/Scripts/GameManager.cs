using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Estado del jugador")]
    public int vidaMaxima = 100;
    public int vidaActual = 100;

    [Header("Progreso")]
    public string escenaActual = "00_Menu";

    public List<string> inventario = new List<string>();
    public List<string> objetivosCompletados = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CambiarEscena(string nombreEscena)
    {
        escenaActual = nombreEscena;
        SceneManager.LoadScene(nombreEscena);
    }

    public void AgregarObjeto(string objeto)
    {
        if (!inventario.Contains(objeto))
        {
            inventario.Add(objeto);
        }
    }

    public bool TieneObjeto(string objeto)
    {
        return inventario.Contains(objeto);
    }

    public void CompletarObjetivo(string objetivo)
    {
        if (!objetivosCompletados.Contains(objetivo))
        {
            objetivosCompletados.Add(objetivo);
        }
    }

    public void RecibirDanio(int cantidad)
    {
        vidaActual -= cantidad;

        if (vidaActual <= 0)
        {
            vidaActual = 0;
            Derrota();
        }
    }

    public void Curar(int cantidad)
    {
        vidaActual += cantidad;

        if (vidaActual > vidaMaxima)
        {
            vidaActual = vidaMaxima;
        }
    }

    public void Derrota()
    {
        Debug.Log("Jugador derrotado");
    }

    public void Victoria()
    {
        Debug.Log("Victoria");
    }
}