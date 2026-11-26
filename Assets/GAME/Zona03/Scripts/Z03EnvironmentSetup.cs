using UnityEngine;
using UnityEngine.AI;

namespace ControlRoomZ03
{
    [DefaultExecutionOrder(-500)]
    public sealed class Z03EnvironmentSetup : MonoBehaviour
    {
        [SerializeField] private NavMeshData navegacion;
        private NavMeshDataInstance instancia;

        private void OnEnable()
        {
            if (navegacion) instancia = NavMesh.AddNavMeshData(navegacion);
        }

        private void OnDisable()
        {
            if (instancia.valid) instancia.Remove();
        }
    }
}
