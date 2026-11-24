// Esta clase es SOLO el contenido que se escribe en el JSON.
// El enunciado (10.2) pide guardar "contenido, no estructuras": por eso aqui no hay
// Queue, Stack ni Dictionary. Ademas JsonUtility de Unity no sabe serializarlos.
// Todo se guarda como List<string> o como lista de pares, y al cargar el
// GameManager vuelve a armar la cola, la pila y el diccionario (ver SaveManager).

using System;
using System.Collections.Generic;

[Serializable]
public class DatosPartida
{
    // Escena y punto de progreso alcanzado
    public string escenaActual = "01_Recepcion";
    public string puntoControl = "inicio";

    // Vida del jugador
    public int vidaMaxima = 100;
    public int vidaActual = 100;

    // Objetos / evidencias y objetivos completados
    public List<string> inventario = new List<string>();
    public List<string> objetivosCompletados = new List<string>();

    // Contenido del Dictionary<string, bool> de puertas, guardado como pares
    public List<EstadoPuerta> puertas = new List<EstadoPuerta>();

    // Contenido de la Queue de mensajes, en orden de salida:
    // el primero de la lista es el siguiente que se va a mostrar
    public List<string> mensajesPendientes = new List<string>();

    // Contenido de la Stack de accesos, de abajo hacia arriba:
    // el ULTIMO de la lista es el tope de la pila
    public List<string> historialAccesos = new List<string>();

    // Estadisticas de la partida
    public float tiempoJugado;
    public int muertes;
}

// Un par clave-valor del diccionario de puertas.
// JsonUtility no guarda Dictionary, pero si una lista de objetos [Serializable].
[Serializable]
public class EstadoPuerta
{
    public string codigo;
    public bool desbloqueada;
}
