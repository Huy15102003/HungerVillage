using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Public score variable (persisted on the GameObject via DontDestroyOnLoad)
        public int Score = 0;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Public method to add score
        public void AddScore(int amount)
        {
            Score += amount;
            Debug.Log($"GameManager: Score added {amount}, total = {Score}");
        }
    }
}
