using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float _horizontal;
    private float _vertical;

    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _speed;
    [SerializeField] private LayerMask _ground;

    [SerializeField] private float _originOffsetY;
    [SerializeField] private float _distanceFromCenter;
    [SerializeField] private float _downwardRayLength;

    private RaycastHit2D _leftHitInfo;
    private RaycastHit2D _rightHitInfo;

    [SerializeField] private float _posYOffet;

    [SerializeField] private float _horizontalRayLength;
    [SerializeField] private float _originOffsetX;

    private float similarityThreshold = 0.2f;
    [SerializeField] private float _maxSpeed;

    [SerializeField] private float _inwardAngle;

    [SerializeField] private float _angularSpeed;

    private bool _isRotating = false;
    private float _minAngle = 0.5f;

    private Vector2 _input;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");
        _vertical = Input.GetAxisRaw("Vertical");
        _input = new Vector2(_horizontal, _vertical);
        
    }

    private void FixedUpdate()
    {
        // If vertical, ignore gravity
        if (VectorSimilarity(transform.right, Vector2.up) > similarityThreshold || VectorSimilarity(-transform.right, Vector2.up) > similarityThreshold)
        {
            _rb.gravityScale = 0;
        }
        else
        {
            _rb.gravityScale = 1;
        }

        // Detect rotation and wall climbing
        if (DoubleRaycastDown())
        {
            OverrideRays();

            PositionOnGround();

        }

        // If no input, stop player
        if (_input == Vector2.zero) 
        {
            _rb.velocity = Vector2.zero;
        }

        if (!_isRotating)
        {
            _rb.AddForce(_horizontal * _speed * Vector2.right, ForceMode2D.Force);
            _rb.AddForce(_vertical * _speed * Vector2.up, ForceMode2D.Force);
        }
        

        // Clamp the velocity to the maximum speed
        if (_rb.velocity.sqrMagnitude > _maxSpeed)
        {
            _rb.velocity = _rb.velocity.normalized * _maxSpeed;
        }
        
    }

    bool DoubleRaycastDown()
    {

        // Calculate the positions for the left and right rays
        Vector2 leftRayOrigin = transform.position + _originOffsetY * transform.up
            + _distanceFromCenter * transform.right;
        Vector2 rightRayOrigin = transform.position + _originOffsetY * transform.up
            - _distanceFromCenter * transform.right;

        // Calculate the inward direction for the rays using rotation
        Vector2 inwardDirectionLeft = Quaternion.Euler(0, 0, _inwardAngle) * -transform.up;
        Vector2 inwardDirectionRight = Quaternion.Euler(0, 0, -_inwardAngle) * -transform.up;

        _leftHitInfo = Physics2D.Raycast(leftRayOrigin, inwardDirectionLeft, _downwardRayLength, _ground);
        _rightHitInfo = Physics2D.Raycast(rightRayOrigin, inwardDirectionRight, _downwardRayLength, _ground);

        Debug.DrawRay(leftRayOrigin, inwardDirectionLeft * _downwardRayLength, Color.red);
        Debug.DrawRay(rightRayOrigin, inwardDirectionRight * _downwardRayLength, Color.blue);

        // Return true if both rays hit something
        return _leftHitInfo.collider != null && _rightHitInfo.collider != null;
    }


    void PositionOnGround()
    {
        // Calculate average normal and average point between the left and right rays
        Vector2 averageNormal = (_leftHitInfo.normal + _rightHitInfo.normal) / 2;
        Vector2 averagePoint = (_leftHitInfo.point + _rightHitInfo.point) / 2;

        Debug.DrawLine(averagePoint, averagePoint + averageNormal, Color.green);

        // Calculate the target rotation based on the average normal
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, averageNormal);

        // If the angle difference is small, don't rotate (to avoid jittering at the edge of two surfaces with different slopes, i.e. corners)
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        Quaternion finalRotation = Quaternion.RotateTowards(transform.rotation,
            targetRotation, _angularSpeed);

        if (angleDifference > _minAngle)
        {
            transform.rotation = Quaternion.Euler(0, 0, finalRotation.eulerAngles.z);

        }
        _isRotating = Quaternion.Angle(transform.rotation, finalRotation) > _minAngle;

        // Move the character to the average position, with the offset applied
        transform.position = averagePoint + (Vector2)transform.up * _posYOffet;

    }

    void OverrideRays()
    {
        // Shoot a horizontal ray in the direction that the player is moving towards
        // If ray intersects with a wall, override the left/right downward ray
        if (_rb.velocity.magnitude > 0)
        {
            // If moving towards player's left
            if (VectorSimilarity(_input, -transform.right) > similarityThreshold && _rb.velocity != Vector2.zero)
            {
                
                RaycastHit2D overrideLeftHitInfo;
                Vector2 overrideLeftRayOrigin = (Vector2)transform.position + _originOffsetY * (Vector2)transform.up - _originOffsetX * (Vector2)transform.right;

                overrideLeftHitInfo = Physics2D.Raycast(overrideLeftRayOrigin, -transform.right, _horizontalRayLength, _ground);
                Debug.DrawRay(overrideLeftRayOrigin, _horizontalRayLength * -transform.right, Color.black);

                if (overrideLeftHitInfo.collider != null)
                {
                    Debug.Log("Left wall detected");
                    _leftHitInfo = overrideLeftHitInfo;
                }
            }

            // If moving towards player's right
            else if (VectorSimilarity(_input, transform.right) > similarityThreshold && _rb.velocity != Vector2.zero)
            {
                RaycastHit2D overrideRightHitInfo;
                Vector2 overrideRightRayOrigin = (Vector2)transform.position + _originOffsetY * (Vector2)transform.up + _originOffsetX * (Vector2)transform.right;

                overrideRightHitInfo = Physics2D.Raycast(overrideRightRayOrigin, transform.right, _horizontalRayLength, _ground);
                Debug.DrawRay(overrideRightRayOrigin, _horizontalRayLength * transform.right, Color.black);

                if (overrideRightHitInfo.collider != null)
                {
                    Debug.Log("Right wall detected");
                    _rightHitInfo = overrideRightHitInfo;
                }
            }
        }
    }

    private float VectorSimilarity(Vector2 vectorA, Vector2 vectorB)
    {
        // ignore magnitude
        vectorA.Normalize();
        vectorB.Normalize();

        return Vector2.Dot(vectorA, vectorB);
    }
}

