using UnityEngine;
using Abel.GAS.Cues;

namespace Abel.GAS.Cues.Notifiers
{
    public class GCN_Custom_Start : GameplayCueNotify
    {
        public override void OnActive(GameplayCueParameters parameters) 
        {
            base.OnActive(parameters);
            // Initialize VFX/SFX here
        }

        public override void OnTick(float deltaTime) 
        {
            base.OnTick(deltaTime);
            // Custom frame-by-frame logic
        }

        public override void OnRemove() 
        {
            base.OnRemove();
            // Cleanup logic
        }
    }
}