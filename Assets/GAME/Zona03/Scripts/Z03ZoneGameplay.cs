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
        private const string Registro = "z03_registro_incidentes";
        private const string Reserva = "z03_reserva_conectada";
        private const string Puerta = "puerta_control_servidores";
        private GameManager partida;
        private Z03HudPresenter hud;
        private Vector3 posicionIzquierda;
        private Vector3 posicionDerecha;
        private string pista;
        private string mensaje;
        private float mostrarHasta;
        private string objetivoActual;
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
            agente.gameObject.SetActive(abierta || partida.ObjetivoCompletado(Vigilancia) || partida.ObjetivoCompletado(Energia));
            if (abierta)
                luzAcceso.color = Color.green;
            else
            {
                if (agente.gameObject.activeSelf) luzAcceso.color = new Color(1f, .38f, .08f);
                particulasEvento.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            objetivoActual = ObjetivoActual();
            Mostrar(abierta ? "Acceso autorizado. Sal por la puerta norte." : objetivoActual);
        }

        public static bool EstacionPreparada(Z03Action accion)
        {
            var juego = GameManager.Instance;
            if (!juego) return false;
            if (accion == Z03Action.Registro) return juego.TieneObjeto(Registro);
            return accion == Z03Action.Reserva && juego.ObjetivoCompletado(Reserva);
        }

        private string ObjetivoActual()
        {
            if (partida.ObjetivoCompletado(Autorizacion))
                return "Cruza el control norte hacia servidores";
            bool vigilancia = partida.ObjetivoCompletado(Vigilancia);
            bool energia = partida.ObjetivoCompletado(Energia);
            if (vigilancia && energia)
                return "Presenta la credencial en la terminal central";
            string oeste = partida.TieneObjeto(Registro) ? "Validar vigilancia" : "Localizar archivo";
            string este = partida.ObjetivoCompletado(Reserva) ? "Restablecer energía" : "Conectar reserva";
            if (vigilancia) return "Ala este · " + este;
            if (energia) return "Ala oeste · " + oeste;
            return "Oeste · " + oeste + " / Este · " + este;
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
                case Z03Action.Registro:
                    if (!partida.TieneObjeto(Registro))
                    {
                        partida.AgregarObjeto(Registro);
                        partida.EncolarMensaje("Evidencia recuperada: registro de incidentes.");
                        partida.GuardarPartida(Registro);
                    }
                    Mostrar("Registro disponible. Preséntalo en el panel de vigilancia oeste.");
                    break;
                case Z03Action.Reserva:
                    Reactivar(Reserva, "Circuito de reserva");
                    break;
                case Z03Action.Vigilancia:
                    if (!partida.TieneObjeto(Registro) && !partida.ObjetivoCompletado(Vigilancia))
                    {
                        Mostrar("Falta el registro de incidentes. Busca el archivo en el ala oeste.");
                        break;
                    }
                    Reactivar(Vigilancia, "Vigilancia");
                    break;
                case Z03Action.Energia:
                    if (!partida.ObjetivoCompletado(Reserva) && !partida.ObjetivoCompletado(Energia))
                    {
                        Mostrar("Circuito sin alimentación. Conecta el interruptor de reserva al este.");
                        break;
                    }
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
            objetivoActual = ObjetivoActual();
        }

        private Vector3 CentroSalida() => (puertaIzquierda.position + puertaDerecha.position) * 0.5f;
        private void Reactivar(string objetivo, string sistema)
        {
            if (!partida.ObjetivoCompletado(objetivo))
            {
                partida.CompletarObjetivo(objetivo);
                if (objetivo != Reserva && !agente.gameObject.activeSelf)
                {
                    agente.gameObject.SetActive(true);
                    luzAcceso.color = new Color(1f, .38f, .08f);
                    efectos.PlayOneShot(audioAlarma);
                    partida.EncolarMensaje("Guardia en patrulla. Usa los bastidores para cortar su línea de visión.");
                }
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
            Mostrar("Acceso concedido. Cruza el control norte. Huye hacia la salida.");
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

            hud.SetObjective(objetivoActual);
            hud.SetStatus("VIDA " + partida.vidaActual + " / " + partida.vidaMaxima + (agente.gameObject.activeSelf ? "   ALERTA" : "   CONTROL"));
            hud.SetHelp(partida.jugadorMuerto ? "Sin vida. Volviendo al último checkpoint…" : Time.time < mostrarHasta ? mensaje : pista);
        }
    }
}
