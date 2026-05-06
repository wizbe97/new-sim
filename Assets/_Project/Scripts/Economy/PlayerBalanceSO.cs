using UnityEngine;

namespace Project.Economy
{
    [CreateAssetMenu(
        fileName = "PlayerBalance",
        menuName = "Project/Economy/Player Balance")]
    public sealed class PlayerBalanceSO : ScriptableObject
    {
        [Header("Starting Values")]
        [SerializeField, Min(0)] private int startingBalance = 500;

        public int StartingBalance => startingBalance;
    }
}