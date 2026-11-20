// Datos iniciales empaquetados y progreso por usuario en PC.
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string ARCHIVO_GUARDADO = "partida.json";
    private const string ARCHIVO_NUEVA_PARTIDA = "nueva_partida.json";

    private static string RutaGuardada => Path.Combine(Application.persistentDataPath, ARCHIVO_GUARDADO);
    private static string GuardadoAnterior => Path.Combine(Application.streamingAssetsPath, ARCHIVO_GUARDADO);

    public static bool ExisteGuardado()
    {
        return File.Exists(RutaGuardada) || File.Exists(GuardadoAnterior);
    }

    public static void Guardar(DatosPartida datos)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        string temporal = RutaGuardada + ".tmp";
        File.WriteAllText(temporal, JsonUtility.ToJson(datos, true));
        if (File.Exists(RutaGuardada))
            File.Replace(temporal, RutaGuardada, null);
        else
            File.Move(temporal, RutaGuardada);
    }

    public static DatosPartida CargarGuardado()
    {
        return Leer(File.Exists(RutaGuardada) ? RutaGuardada : GuardadoAnterior);
    }

    public static DatosPartida CargarNuevaPartida()
    {
        return Leer(Path.Combine(Application.streamingAssetsPath, ARCHIVO_NUEVA_PARTIDA));
    }

    private static DatosPartida Leer(string ruta)
    {
        if (!File.Exists(ruta)) return new DatosPartida();
        try
        {
            return JsonUtility.FromJson<DatosPartida>(File.ReadAllText(ruta)) ?? new DatosPartida();
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("No se pudo leer la partida en " + ruta + ": " + error.Message);
            return new DatosPartida();
        }
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
