using UnityEngine;

namespace ControlRoomZ03
{
    public sealed class Z03ZoneGameplay : MonoBehaviour
    {
        private PlayerController jugador;
        [SerializeField]
        private bool permitirDemostracionDirecta;
        private bool demostracion;
        [SerializeField]
        private Z03SecurityAgent agente;
        [SerializeField]
        private Transform puertaIzquierda;
        [SerializeField]
        private Transform puertaDerecha;
        [SerializeField]
        private ParticleSystem particulasEvento;
        [SerializeField]
        private Light luzAcceso;
        [SerializeField]
        private AudioSource efectos;
        [SerializeField]
        private AudioClip audioConfirmacion;
        [SerializeField]
        private AudioClip audioAlarma;
        [SerializeField]
        private string identificadorCredencial = "credencial_laboratorio";
        private const string Vigilancia = "z03_vigilancia";
        private const string Energia = "z03_energia";
        private const string Autorizacion = "z03_control_restaurado";
        private const string Puerta = "puerta_control_servidores";
        private GameManager partida;
        private Z03HudPresenter hud;
        private Vector3 posicionIzquierda;
        private Vector3 posicionDerecha;
        private string pista;
        private string mensaje;
        private float mostrarHasta;
        private void Start()
        {
            partida = GameManager.Instance;
            jugador = FindFirstObjectByType<PlayerController>();
            hud = FindFirstObjectByType<Z03HudPresenter>();
            posicionIzquierda = puertaIzquierda.localPosition;
            posicionDerecha = puertaDerecha.localPosition;
            demostracion = permitirDemostracionDirecta && !partida.enPartida;
            partida.RegistrarPuerta(Puerta);
            partida.EscenaIniciada();
            bool abierta = partida.ObjetivoCompletado(Autorizacion);
            agente.gameObject.SetActive(abierta);
            if (abierta)
                luzAcceso.color = Color.green;
            else
                particulasEvento.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Mostrar("Reactiva vigilancia y energía antes de autorizar la terminal.");
        }

        public string Diagnostico()
        {
            return "Vigilancia: " + Estado(Vigilancia) + "\nEnergía: " + Estado(Energia) + "\nAutorización: " + Estado(Autorizacion);
        }

        private string Estado(string objetivo) => partida.ObjetivoCompletado(objetivo) ? "OK" : "PENDIENTE";
        public void SetInteractionHint(string texto) => pista = texto;
        public void Interactuar(Z03Action accion)
        {
            if (partida.enPausa || partida.jugadorMuerto)
                return;
            switch (accion)
            {
                case Z03Action.Vigilancia:
                    Reactivar(Vigilancia, "Vigilancia");
                    break;
                case Z03Action.Energia:
                    Reactivar(Energia, "Energía");
                    break;
                case Z03Action.Terminal:
                    Autorizar();
                    break;
                case Z03Action.Salida:
                    if (!partida.PuertaDesbloqueada(Puerta))
                        Mostrar("Acceso bloqueado. Usa la terminal central.");
                    else if (Vector3.Distance(jugador.transform.position, CentroSalida()) < 5f)
                        partida.CambiarEscena("04_Servidores");
                    else
                        Mostrar("Acércate a la puerta de servidores.");
                    break;
            }
        }

        private Vector3 CentroSalida() => (puertaIzquierda.position + puertaDerecha.position) * 0.5f;
        private void Reactivar(string objetivo, string sistema)
        {
            if (!partida.ObjetivoCompletado(objetivo))
            {
                partida.CompletarObjetivo(objetivo);
                efectos.PlayOneShot(audioConfirmacion);
                partida.GuardarPartida(objetivo);
            }

            Mostrar(sistema + " operativo.");
        }

        private void Autorizar()
        {
            if (partida.ObjetivoCompletado(Autorizacion))
            {
                Mostrar("Acceso ya autorizado.");
                return;
            }

            if (!partida.ObjetivoCompletado(Vigilancia) || !partida.ObjetivoCompletado(Energia))
            {
                Mostrar("Restablece vigilancia y energía antes de autorizar.");
                return;
            }

            if (!demostracion && !partida.TieneObjeto(identificadorCredencial))
            {
                Mostrar("Falta la credencial del laboratorio.");
                return;
            }

            partida.CompletarObjetivo(Autorizacion);
            partida.RegistrarAcceso(identificadorCredencial);
            partida.AgregarObjeto("acceso_servidores");
            partida.DesbloquearPuerta(Puerta);
            partida.GuardarPartida(Autorizacion);
            particulasEvento.Play();
            luzAcceso.color = Color.green;
            agente.gameObject.SetActive(true);
            efectos.PlayOneShot(audioAlarma);
            Mostrar("Acceso concedido. Seguridad hostil activada. Huye hacia la salida.");
        }

        private void Mostrar(string texto)
        {
            mensaje = texto;
            mostrarHasta = Time.time + 4f;
        }

        private void Update()
        {
            if (partida.enPausa)
                return;
            bool abierta = partida.ObjetivoCompletado(Autorizacion);
            if (abierta)
            {
                puertaIzquierda.localPosition = Vector3.Lerp(puertaIzquierda.localPosition, posicionIzquierda + Vector3.left * 1.9f, Time.deltaTime * 2.5f);
                puertaDerecha.localPosition = Vector3.Lerp(puertaDerecha.localPosition, posicionDerecha + Vector3.right * 1.9f, Time.deltaTime * 2.5f);
            }

            hud.SetObjective(abierta ? "ACCESO LIBERADO · Escapa a servidores" : "Vigilancia " + Estado(Vigilancia) + " · Energía " + Estado(Energia));
            hud.SetStatus("VIDA " + partida.vidaActual + " / " + partida.vidaMaxima + (abierta ? "   ALERTA" : "   CONTROL"));
            hud.SetHelp(partida.jugadorMuerto ? "Sin vida. Volviendo al último checkpoint…" : Time.time < mostrarHasta ? mensaje : pista);
        }
    }
}
