using UnityEngine;

public class ContainerVisualization : MonoBehaviour
{
    [SerializeField] private bool _drawOutline;
    [SerializeField] private Color _color;

    private void OnDrawGizmosSelected()
    {
        if (!_drawOutline) return;

        Gizmos.color = _color;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
