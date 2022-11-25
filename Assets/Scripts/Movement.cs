using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    private float _maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    private float _maxAcceleration = 40f;
    [SerializeField]
    private Rect _allowesArea = new Rect(-5f, -5f, 10f, 10f);

    private Vector3 _velocity;

    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * _maxSpeed;
        float maxSpeedChange = _maxAcceleration * Time.deltaTime;
        _velocity.x = Mathf.MoveTowards(_velocity.x, desiredVelocity.x, maxSpeedChange);
        _velocity.z = Mathf.MoveTowards(_velocity.z, desiredVelocity.z, maxSpeedChange);

        Vector3 d = _velocity * Time.deltaTime;
        transform.localPosition += d;
    }
}
