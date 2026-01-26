using System;
using UnityEngine;

namespace FD.Character
{
    public class Player : BaseCharacter
    {
        [Header("Player Specific")]
        [SerializeField] private float interactionRange = 2f;

        [Header("Score")]
        [SerializeField] private int startingScore = 0;
        [SerializeField] private int escapedEnemyPenalty = 1;

        private int currentScore;

        public event Action<int> OnScoreChanged;
        
        protected override void Awake()
        {
            base.Awake();
            // Player specific initialization
        }

        protected override void Start()
        {
            base.Start();
            currentScore = startingScore;
            RaiseScoreChanged();
            // Player specific start logic
        }

        protected override void Update()
        {
            base.Update();
            // Player specific update logic
            HandleInput();
        }

        private void HandleInput()
        {
            // Handle player input
        }

        public float InteractionRange => interactionRange;

        public int CurrentScore => currentScore;

        public void AddScore(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            SetScore(currentScore + amount);
        }

        public void ApplyEnemyEscapePenalty()
        {
            AddScore(-Mathf.Abs(escapedEnemyPenalty));
        }

        private void SetScore(int newScore)
        {
            if (newScore == currentScore)
            {
                return;
            }

            currentScore = newScore;
            RaiseScoreChanged();
        }

        private void RaiseScoreChanged()
        {
            Debug.Log($"Player score changed: {currentScore}");
            OnScoreChanged?.Invoke(currentScore);
        }
    }
}
