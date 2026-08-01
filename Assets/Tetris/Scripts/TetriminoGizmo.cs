using UnityEngine;

public class TetriminoGizmo : MonoBehaviour
{
    [SerializeField] private Vector3 _boundSize;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position,_boundSize);
    }
}
