// Responsabilidad (enunciado 5.3): leer y escribir el JSON en StreamingAssets
// y reconstruir las estructuras de datos a partir del contenido leido.
//
// Es una clase static (no MonoBehaviour) porque no necesita estar en ningun
// GameObject: el GameManager la llama directamente con SaveManager.Guardar(...).
//
// Archivos que usa, ambos en Assets/StreamingAssets:
//   nueva_partida.json -> estado inicial. Solo se LEE al pulsar "Nueva partida".
//   partida.json       -> el guardado. Se ESCRIBE en cada punto de control y se
//                         LEE al pulsar "Continuar" o al morir.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string ARCHIVO_GUARDADO = "partida.json";
    private const string ARCHIVO_NUEVA_PARTIDA = "nueva_partida.json";

    // Application.streamingAssetsPath apunta a Assets/StreamingAssets en el editor
    // y a <Juego>_Data/StreamingAssets en el ejecutable de PC
    private static string Ruta(string archivo)
    {
        return Path.Combine(Application.streamingAssetsPath, archivo);
    }

    public static bool ExisteGuardado()
    {
        return File.Exists(Ruta(ARCHIVO_GUARDADO));
    }

    // ---------- Escritura y lectura ----------

    public static void Guardar(DatosPartida datos)
    {
        // Por si la carpeta no existe (por ejemplo, en un build recien hecho)
        Directory.CreateDirectory(Application.streamingAssetsPath);

        // true = JSON con sangria, para poder mostrarlo legible en la sustentacion
        string json = JsonUtility.ToJson(datos, true);
        File.WriteAllText(Ruta(ARCHIVO_GUARDADO), json);
    }

    public static DatosPartida CargarGuardado()
    {
        return Leer(ARCHIVO_GUARDADO);
    }

    public static DatosPartida CargarNuevaPartida()
    {
        return Leer(ARCHIVO_NUEVA_PARTIDA);
    }

    private static DatosPartida Leer(string archivo)
    {
        string ruta = Ruta(archivo);

        if (!File.Exists(ruta))
        {
            Debug.LogWarning("No se encontro " + ruta + ", se usan los valores por defecto");
            return new DatosPartida();
        }

        DatosPartida datos = JsonUtility.FromJson<DatosPartida>(File.ReadAllText(ruta));

        // FromJson devuelve null si el archivo esta vacio
        if (datos == null)
        {
            return new DatosPartida();
        }

        return datos;
    }

    // ---------- Reconstruccion de estructuras (JSON -> estructura) ----------

    // La lista viene en orden de salida, asi que se encola en ese mismo orden:
    // el primero en entrar sera el primero en salir con Dequeue
    public static Queue<string> ReconstruirCola(List<string> enOrden)
    {
        Queue<string> cola = new Queue<string>();

        foreach (string elemento in enOrden)
        {
            cola.Enqueue(elemento);
        }

        return cola;
    }

    // La lista viene de abajo hacia arriba: al hacer Push en ese orden,
    // el ultimo elemento queda en el tope, igual que estaba antes de guardar
    public static Stack<string> ReconstruirPila(List<string> deAbajoArriba)
    {
        Stack<string> pila = new Stack<string>();

        foreach (string elemento in deAbajoArriba)
        {
            pila.Push(elemento);
        }

        return pila;
    }

    public static Dictionary<string, bool> ReconstruirDiccionario(List<EstadoPuerta> pares)
    {
        Dictionary<string, bool> diccionario = new Dictionary<string, bool>();

        foreach (EstadoPuerta par in pares)
        {
            diccionario[par.codigo] = par.desbloqueada;
        }

        return diccionario;
    }

    // ---------- Extraccion de contenido (estructura -> JSON) ----------
    // Para sacar el contenido de la cola y la pila se usan SOLO sus operaciones
    // propias (Dequeue/Enqueue y Pop/Push), sin indices ni ToList. Al terminar,
    // la estructura queda exactamente igual que antes.

    // Se saca cada elemento con Dequeue, se anota y se vuelve a meter al final.
    // Despues de dar una vuelta completa la cola conserva su orden original.
    public static List<string> ColaALista(Queue<string> cola)
    {
        List<string> enOrden = new List<string>();
        int cantidad = cola.Count;

        for (int i = 0; i < cantidad; i++)
        {
            string elemento = cola.Dequeue();
            enOrden.Add(elemento);
            cola.Enqueue(elemento);
        }

        return enOrden;
    }

    // Se vacia la pila con Pop (salen del tope hacia abajo) y luego se vuelven
    // a apilar empezando por el de abajo, para que el tope quede donde estaba
    public static List<string> PilaALista(Stack<string> pila)
    {
        List<string> deArribaAbajo = new List<string>();

        while (pila.Count > 0)
        {
            deArribaAbajo.Add(pila.Pop());
        }

        List<string> deAbajoArriba = new List<string>();

        for (int i = deArribaAbajo.Count - 1; i >= 0; i--)
        {
            pila.Push(deArribaAbajo[i]);
            deAbajoArriba.Add(deArribaAbajo[i]);
        }

        return deAbajoArriba;
    }

    public static List<EstadoPuerta> DiccionarioALista(Dictionary<string, bool> diccionario)
    {
        List<EstadoPuerta> pares = new List<EstadoPuerta>();

        foreach (KeyValuePair<string, bool> par in diccionario)
        {
            EstadoPuerta estado = new EstadoPuerta();
            estado.codigo = par.Key;
            estado.desbloqueada = par.Value;
            pares.Add(estado);
        }

        return pares;
    }
}
