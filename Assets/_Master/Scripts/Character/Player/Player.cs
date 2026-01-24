using UnityEngine;

namespace FD.Character
{
    public class Player : BaseCharacter
    {
        [Header("Player Specific")]
        [SerializeField] private float interactionRange = 2f;
        
        protected override void Awake()
        {
            base.Awake();
            // Player specific initialization
        }

        protected override void Start()
        {
            base.Start();
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
    }
}
