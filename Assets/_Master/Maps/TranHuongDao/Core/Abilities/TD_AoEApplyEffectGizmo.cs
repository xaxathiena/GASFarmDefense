using UnityEngine;


/// <summary>
/// Helper component to draw AoE Aura Gizmos in the Unity Editor.
/// </summary>
public class TD_AoEApplyEffectGizmo : MonoBehaviour
{
    public float Radius;
    public Color Color = new Color(0, 1, 1, 0.2f); // Cyan with transparency
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color;
        Gizmos.DrawWireSphere(transform.position, Radius);

        // Draw a semi-transparent solid sphere as well for better visibility
        Gizmos.color = new Color(Color.r, Color.g, Color.b, Color.a * 0.25f);
        Gizmos.DrawSphere(transform.position, Radius);
    }
#endif
}
