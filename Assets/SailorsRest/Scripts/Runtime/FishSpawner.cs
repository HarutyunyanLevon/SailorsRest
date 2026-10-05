using System.Collections.Generic;
using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>Keeps the lake stocked with wild fish, picking species and tier by their weights.</summary>
    public class FishSpawner : MonoBehaviour
    {
        public GameBalance balance;
        public readonly List<FishAgent> Fish = new List<FishAgent>();

        void Start() => Refill();

        public void Refill()
        {
            while (Fish.Count < balance.fishCount && Spawn()) { }
        }

        public void Remove(FishAgent fish)
        {
            Fish.Remove(fish);
            Destroy(fish.gameObject);
        }

        bool Spawn()
        {
            if (balance.species == null || balance.species.Length == 0) return false;
            var species = balance.species[WeightedIndex(balance.species.Length, i => balance.species[i].spawnWeight)];
            int tier = MergeRules.MinTier + WeightedIndex(species.wildTierWeights.Length, i => species.wildTierWeights[i]);
            var variant = Random.value < species.goldenChance ? Variant.Golden : Variant.None;
            var data = new FishData(species.id, tier, species.RandomWeight(tier), variant);

            float x = Random.Range(balance.dockEdgeX + balance.spawnGapFromDock, balance.FarthestX);
            float y = GameBalance.WaterlineY - Random.Range(species.minDepth, species.maxDepth);
            float halfWidth = Random.Range(balance.patrolHalfWidth.x, balance.patrolHalfWidth.y);
            float minX = Mathf.Max(balance.dockEdgeX + balance.patrolGapFromDock, x - halfWidth);
            float maxX = Mathf.Min(balance.FarthestX, x + halfWidth);

            var go = new GameObject($"Fish {species.displayName} T{tier}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, y, 0f);
            var agent = go.AddComponent<FishAgent>();
            agent.Init(balance, species, data, minX, maxX, y);
            Fish.Add(agent);
            return true;
        }

        /// <summary>Picks an index with probability proportional to its weight.</summary>
        static int WeightedIndex(int count, System.Func<int, float> weightOf)
        {
            float total = 0f;
            for (int i = 0; i < count; i++) total += weightOf(i);
            float roll = Random.value * total;
            for (int i = 0; i < count; i++)
            {
                roll -= weightOf(i);
                if (roll <= 0f) return i;
            }
            return 0;
        }
    }
}
