using UnityEngine;

namespace NaijaEmpires
{
    /// A Farm is a renewable WORKPLACE, not an auto-producer. Up to BuildingConfig.FarmMaxWorkers
    /// villagers can be assigned to it (right-click the farm with villagers selected); each working
    /// villager gathers renewable Yam and carries it to the Town Centre, exactly like a resource node.
    /// This component just turns the farm's collider root into a capped, non-depleting Yam node.
    [RequireComponent(typeof(ResourceNode))]
    public class FarmProduction : MonoBehaviour
    {
        void Awake()
        {
            var node = GetComponent<ResourceNode>();
            if (node == null) node = gameObject.AddComponent<ResourceNode>();
            node.MakeRenewable(ResourceType.Yam, BuildingConfig.FarmMaxWorkers);
        }
    }
}
