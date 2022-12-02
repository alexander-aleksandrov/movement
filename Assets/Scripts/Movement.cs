using System;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    private float _maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    private float _maxAcceleration = 40f, _maxAirAcceleration = 1f;
    private Vector3 _velocity, _desiredVelocity;
    private Rigidbody _body;

    [SerializeField, Range(0f, 100f)]
    private float _jumpHeight = 2f;
    private bool _desiredJump;
    [SerializeField, Range(0, 5)]
    private int _maxAirJumps = 2;
    private int _jumpPhase;
    private int _groundContactsCount, _steepContactCount;
    bool OnGround => _groundContactsCount > 0;
    bool OnSteep => _steepContactCount > 0;

    [SerializeField, Range(0f, 90f)]
    private float _maxGroundAngle = 25f, _maxStairsAngle = 50f;
    private float _minGroundDotProduct, _minStairsDotProduct;
    private Vector3 _contactNormal, _steepNormal;
    private int _stepsSinceLastGround, _stepsSinceLastJump;
    [SerializeField, Range(0f, 100f)]
    private float _maxSnapSpeed = 100f;
    [SerializeField, Min(0f)]
    private float _probeDistance = 1f;
    [SerializeField]
    private LayerMask _probeMask = -1, _stairsMask = -1;

    private void OnValidate()
    {
        _minGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        _minStairsDotProduct = Mathf.Cos(_maxStairsAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        OnValidate();
    }

    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        _desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * _maxSpeed;
        _desiredJump |= Input.GetButtonDown("Jump");
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();

        if (_desiredJump)
        {
            _desiredJump = false;
            Jump();
        }
        _body.velocity = _velocity;
        ClearState();
    }
    private bool SnapToGround()
    {
        if (_stepsSinceLastGround > 1 || _stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = _velocity.magnitude;
        if (speed > _maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(_body.position, Vector3.down, out RaycastHit hit, _probeDistance, _probeMask))
        {
            return false;
        }
        if (hit.normal.y > GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        _groundContactsCount = 1;
        _contactNormal = hit.normal;
        float dot = Vector3.Dot(_velocity, hit.normal);
        if (dot > 0f)
        {
            _velocity = (_velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }
    private void ClearState()
    {
        _groundContactsCount = _steepContactCount = 0;
        _contactNormal = _steepNormal = Vector3.zero;

    }

    private void UpdateState()
    {
        _stepsSinceLastGround += 1;
        _stepsSinceLastJump += 1;
        _velocity = _body.velocity;
        if (OnGround || SnapToGround())
        {
            _stepsSinceLastGround = 0;
            _jumpPhase = 0;
            if (_groundContactsCount > 1)
            {
                _contactNormal.Normalize();
            }
        }
        else
        {
            _contactNormal = Vector3.up;
        }
    }

    private void Jump()
    {
        if (OnGround || _jumpPhase < _maxAirJumps)
        {
            _stepsSinceLastJump = 0;
            _jumpPhase++;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * _jumpHeight);
            float allignedSpeed = Vector3.Dot(_velocity, _contactNormal);
            if (allignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - allignedSpeed, 0f);
            }
            _velocity += _contactNormal * jumpSpeed;
        }
    }
    private float GetMinDot(int layer)
    {
        return (_stairsMask & (1 << layer)) == 0 ? _minGroundDotProduct : _minStairsDotProduct;
    }
    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }
    private void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minDot)
            {
                _groundContactsCount++;
                _contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {
                _steepContactCount += 1;
                _steepNormal += normal;
            }
        }
    }

    private bool CheckSteepContacts()
    {
        if (_steepContactCount > 1)
        {
            _steepNormal.Normalize();
            if (_steepNormal.y > _minGroundDotProduct)
            {
                _groundContactsCount = 1;
                _contactNormal = _steepNormal;
                return true;
            }
        }
        return false;
    }

    public Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
    }
    public void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(_velocity, xAxis);
        float currentZ = Vector3.Dot(_velocity, zAxis);

        float acceleration = OnGround ? _maxAcceleration : _maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        float newX = Mathf.MoveTowards(currentX, _desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, _desiredVelocity.z, maxSpeedChange);

        _velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }
}
