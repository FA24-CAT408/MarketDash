using System.Reflection;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusFixtureGuard : MonoBehaviour
    {
        private void Start()
        {
            foreach (Behaviour behaviour in GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour.GetType().FullName != "KinematicCharacterController.PhysicsMover")
                    continue;

                PropertyInfo controllerProperty = behaviour.GetType().GetProperty("MoverController");
                if (controllerProperty != null && controllerProperty.GetValue(behaviour) == null)
                {
                    behaviour.enabled = false;
                    Debug.LogWarning($"Test Campus disabled malformed mover '{behaviour.name}' because it has no MoverController.", behaviour);
                }
            }
        }
    }
}
