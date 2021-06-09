﻿using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    public HandCollider handCollider = null;
    public Transform cameraFollow = null;
    public float rotateCameraSpeed = 1.0f;
    public float speed = 1.0f;
    public float runSpeedMultiplier = 2.0f;
    [Range(0.1f, 1f)]
    public float maxBackwardsSpeedValue = -0.3f;
    public float attackCooldown = 0.5f;
    public float jumpForce = 1.0f;
    public float jumpTime = 1.0f;

    private new Rigidbody rigidbody;
    Camera mainCamera;
    private Vector3 moveInputValue;
    private Vector3 rotationInputValue;
    private bool isRunningPressed;
    private bool isInAttackMode;
    private bool isJumping;
    private float attackCooldownTime;
    private float jumpTimeValue;

    private Action<bool> onAttackModePressed;
    private Action onAttackPressed;
    private Action onJumping;
    private Action<Vector3> onRotateCamera;

    public Action<bool> OnAttackModePressed { get => onAttackModePressed; set => onAttackModePressed = value; }
    public Action OnAttackPressed { get => onAttackPressed; set => onAttackPressed = value; }
    public Action OnJumping { get => onJumping; set => onJumping = value; }
    public Action<Vector3> OnRotatingCamera { get => onRotateCamera; set => onRotateCamera = value; }

    public void SetUp(Rigidbody rigidbody)
    {
        this.rigidbody = rigidbody;
        moveInputValue = Vector2.zero;
        isRunningPressed = false;
        isInAttackMode = false;
        attackCooldownTime = 0.0f;
        jumpTimeValue = 0.0f;
        mainCamera = Camera.main;

        handCollider.OnEnemyCollision += OnEnemyHit;
    }

    public void HandleMovement(out float inputValue, out bool isRunning)
    {
        bool isGrounded = IsGrounded();
        bool canRun = isRunningPressed && isGrounded;
        moveInputValue.z = Mathf.Clamp(moveInputValue.z, -maxBackwardsSpeedValue, 1.0f);
        Vector3 velocity = moveInputValue * Time.deltaTime * (canRun ? speed * runSpeedMultiplier : speed);
        Vector3 newPosition = transform.position + velocity;

        rigidbody.MovePosition(newPosition);

        HandleJump();
        HandleCameraRotation();

        if (isGrounded && jumpTimeValue <= 0.0f)
            rigidbody.useGravity = false;
        else
            rigidbody.useGravity = true;

        float inputX = Mathf.Abs(moveInputValue.x);
        float inputZ = Mathf.Abs(moveInputValue.z);
        inputValue = inputX + inputZ;
        isRunning = canRun;
    }

    public void HandleRotation()
    {
        if (moveInputValue == Vector3.zero)
            return;

        Vector3 lookAtPosition = new Vector3(moveInputValue.x, 0.0f, moveInputValue.z);
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition);

        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, 1.0f);
    }

    public void HandleCameraRotation()
    {
        Vector3 rotationVector = rotationInputValue * Time.deltaTime * rotateCameraSpeed;
        rotationVector.z = 0.0f;
        cameraFollow.Rotate(rotationVector);
        cameraFollow.rotation = Quaternion.Euler(cameraFollow.rotation.eulerAngles.x, cameraFollow.rotation.eulerAngles.y, 0.0f);
    }

    private void SetHandColliderStatus(bool enabled)
    {
        handCollider.Collider.enabled = enabled;
    }

    private void OnEnemyHit(Enemy enemy)
    {
        Debug.Log(enemy.name);
    }

    private void HandleJump()
    {
        if (isJumping && jumpTimeValue > 0.0f)
        {
            Jump();
            jumpTimeValue -= Time.fixedDeltaTime;
        }
        else if (isJumping && IsGrounded())
        {
            isJumping = false;
            onJumping.Invoke();
        }
    }

    private void Attack()
    {
        StartCoroutine(AttackRoutine());
    }

    private void Jump()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        RaycastHit hitInfo;
        float offset = 0.1f;
        Vector3 startPoint = new Vector3(transform.position.x, transform.position.y + offset, transform.position.z);
        Vector3 direction = Vector3.down;

        Physics.Raycast(startPoint, direction, out hitInfo, 0.1f + offset);
        return hitInfo.transform != null;
    }

    private bool isFalling()
    {
        return rigidbody.velocity.y < 0.0f;
    }

    //---------- PlayerInput methods ----------//

    private void OnMovement(InputValue value)
    {
        Vector2 inputValue = value.Get<Vector2>();
        Vector3 newMoveInputValue = new Vector3(inputValue.x, 0, inputValue.y);

        //newMoveInputValue = inputValue.y * mainCamera.transform.forward + inputValue.x * mainCamera.transform.right;
        //newMoveInputValue = new Vector3(newMoveInputValue.x, 0.0f, newMoveInputValue.z);

        moveInputValue = newMoveInputValue;
    }

    private void OnRun(InputValue value)
    {
        isRunningPressed = value.isPressed;
    }

    private void OnAttackMode()
    {
        isInAttackMode = !isInAttackMode;
        onAttackModePressed?.Invoke(isInAttackMode);
    }

    private void OnAttack()
    {
        if (!isInAttackMode)
            return;

        if (attackCooldownTime > 0.0f)
            return;

        Attack();
    }

    private void OnJump(InputValue value)
    {
        bool canJump = value.isPressed && IsGrounded();

        if (canJump)
        {
            isJumping = true;
            jumpTimeValue = jumpTime;
            onJumping?.Invoke();
        }
        else
        {
            jumpTimeValue = 0.0f;
        }
    }

    private void OnRotateCamera(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        rotationInputValue = Vector3.Normalize(new Vector3(input.y, input.x, 0));
    }

    //----------------------------------------//

    private IEnumerator AttackRoutine()
    {
        onAttackPressed?.Invoke();
        attackCooldownTime = attackCooldown;
        SetHandColliderStatus(true);

        while (attackCooldownTime > 0.0f)
        {
            attackCooldownTime = Mathf.Clamp(attackCooldownTime - Time.deltaTime, 0.0f, attackCooldown);
            yield return null;
        }

        SetHandColliderStatus(false);
        yield return null;
    }
}
