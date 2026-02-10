using UnityEngine;

namespace FD.DI
{
    /// <summary>
    /// Very simple test để verify Unity MonoBehaviour lifecycle
    /// </summary>
    public class SimpleDebugTest : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("SimpleDebugTest: Awake!");
        }
        
        private void Start()
        {
            Debug.Log("SimpleDebugTest: Start!");
        }
    }
}
