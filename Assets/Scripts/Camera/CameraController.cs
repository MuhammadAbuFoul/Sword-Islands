using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float playerWeight = 0.7f;
    [SerializeField] private float maxCursorDistance = 5f;
    [SerializeField] private float dashFollowSpeed = 50f; // Much faster follow during dash

    private Camera cam;
    private float maxCursorDistanceSqr;
    private Vector3 lastPlayerPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        maxCursorDistanceSqr = maxCursorDistance * maxCursorDistance;
        FindPlayerIfNeeded();
        if (player != null)
            lastPlayerPosition = player.position;
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector2 targetPos = CalculateTargetPosition();

        // Calculate player velocity to detect dashing
        float playerSpeed = (player.position - lastPlayerPosition).magnitude / Time.deltaTime;
        lastPlayerPosition = player.position;

        MoveToTarget(targetPos, playerSpeed);
    }

    private Vector2 CalculateTargetPosition()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        Vector2 playerPos = player.position;
        Vector2 cursorDirection = (Vector2)(mouseWorldPos - (Vector3)playerPos);

        // Use squared magnitude to avoid sqrt
        if (cursorDirection.sqrMagnitude > maxCursorDistanceSqr)
        {
            cursorDirection = cursorDirection.normalized * maxCursorDistance;
        }

        Vector2 cursorPos = playerPos + cursorDirection;
        return Vector2.Lerp(cursorPos, playerPos, playerWeight);
    }

    private void MoveToTarget(Vector2 targetPos, float playerSpeed)
    {
        Vector3 newPos = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        // Use faster follow speed when player is dashing (moving fast)
        // Normal player speed is around 5, dash is much faster
        float currentFollowSpeed = playerSpeed > 10f ? dashFollowSpeed : followSpeed;

        transform.position = Vector3.Lerp(transform.position, newPos, currentFollowSpeed * Time.deltaTime);
    }
}