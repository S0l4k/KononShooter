using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float baseMovementSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float cameraSmoothTime = 0.15f;
    [SerializeField] private float verticalSmoothTime = 0.2f;
    [SerializeField] private float aimEdgeThreshold = 0.15f;
    [SerializeField] private float targetDistance = 10f;
    [SerializeField] private Animator animator;
    [SerializeField] private Rig aimRig;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cameraTransform;
    private float _currentSpeedMultiplier = 1f;
    private float _buffEndTime = 0f;
    private bool _hasSpeedBuff = false;

    [Header("Damage")]
    [SerializeField] private int baseBulletDamage = 10; 
    private int _bonusDamage = 0;

    [Header("Crosshair")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private float crosshairFadeSpeed = 5f;

    [Header("Shooting")]
    [SerializeField] private Transform muzzle; 
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f; 
    [SerializeField] private AudioClip shootSound;

    private float _nextFireTime = 0f;
    private AudioSource _audioSource;

    private float _aimWeight = 0f;
    private float _currentXRotation = 0f;
    private float _targetXRotation = 0f;
    private Vector2 _targetScreenOffset;
    private bool _isAiming = false;

    private float _smoothMouseX;
    private float _smoothMouseY;
    private float _mouseVelocityX;
    private float _mouseVelocityY;
    private float _rotationVelocity;

    private CanvasGroup _crosshairCanvasGroup;
    private float _crosshairAlpha = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _targetScreenOffset = new Vector2(0.5f, 0.5f);
        _currentXRotation = 0f;
        _targetXRotation = 0f;
        UpdateTargetWorldPosition();
        UpdateCameraRotation();

        if (crosshair != null)
        {
            _crosshairCanvasGroup = crosshair.GetComponent<CanvasGroup>();
            if (_crosshairCanvasGroup == null)
            {
                _crosshairCanvasGroup = crosshair.gameObject.AddComponent<CanvasGroup>();
            }
            _crosshairCanvasGroup.alpha = 0f;
            crosshair.gameObject.SetActive(false);
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        HandleCameraRotation();
        HandleMovement();
        HandleAiming();
        UpdateCrosshair();
        HandleShooting();
    }

    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _smoothMouseX = Mathf.SmoothDamp(
            _smoothMouseX, mouseX, ref _mouseVelocityX, cameraSmoothTime
        );
        _smoothMouseY = Mathf.SmoothDamp(
            _smoothMouseY, mouseY, ref _mouseVelocityY, cameraSmoothTime
        );

        bool shouldRotateWhileAiming = _isAiming && ShouldRotateFromEdge();

        if (!_isAiming || shouldRotateWhileAiming)
        {
            transform.Rotate(Vector3.up * _smoothMouseX);

            _targetXRotation -= _smoothMouseY;
            _targetXRotation = Mathf.Clamp(_targetXRotation, -60f, 60f);

            _currentXRotation = Mathf.SmoothDamp(
                _currentXRotation,
                _targetXRotation,
                ref _rotationVelocity,
                verticalSmoothTime
            );

            UpdateCameraRotation();
        }
    }

    private void UpdateCameraRotation()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(_currentXRotation, 0f, 0f);
        }
    }

    private void HandleAiming()
    {
        if (Input.GetMouseButton(1))
        {
            _isAiming = true;
            _aimWeight = Mathf.MoveTowards(_aimWeight, 1f, Time.deltaTime * 5f);

            _targetScreenOffset.x += _smoothMouseX * 0.001f;
            _targetScreenOffset.y += _smoothMouseY * 0.001f;

            _targetScreenOffset.x = Mathf.Clamp(_targetScreenOffset.x,
                aimEdgeThreshold, 1f - aimEdgeThreshold);
            _targetScreenOffset.y = Mathf.Clamp(_targetScreenOffset.y,
                aimEdgeThreshold, 1f - aimEdgeThreshold);

            UpdateTargetWorldPosition();
        }
        else
        {
            _isAiming = false;
            _aimWeight = Mathf.MoveTowards(_aimWeight, 0f, Time.deltaTime * 5f);

            _targetScreenOffset = Vector2.MoveTowards(
                _targetScreenOffset, new Vector2(0.5f, 0.5f), Time.deltaTime * 2f
            );
            UpdateTargetWorldPosition();
        }

        _aimWeight = Mathf.Clamp(_aimWeight, 0f, 1f);
        animator.SetFloat("AimWeight", _aimWeight);
        animator.SetLayerWeight(1, _aimWeight);

        if (aimRig != null)
            aimRig.weight = _aimWeight;
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null) return;

        float targetAlpha = _isAiming ? 1f : 0f;
        _crosshairAlpha = Mathf.MoveTowards(_crosshairAlpha, targetAlpha, Time.deltaTime * crosshairFadeSpeed);
        _crosshairCanvasGroup.alpha = _crosshairAlpha;

        crosshair.gameObject.SetActive(_crosshairAlpha > 0.01f);

        if (crosshair.gameObject.activeSelf)
        {
            Vector2 screenPos = new Vector2(
                _targetScreenOffset.x * Screen.width,
                _targetScreenOffset.y * Screen.height
            );

            crosshair.position = screenPos;
        }
    }

    private bool ShouldRotateFromEdge()
    {
        return _targetScreenOffset.x <= aimEdgeThreshold + 0.01f ||
               _targetScreenOffset.x >= 1f - aimEdgeThreshold - 0.01f ||
               _targetScreenOffset.y <= aimEdgeThreshold + 0.01f ||
               _targetScreenOffset.y >= 1f - aimEdgeThreshold - 0.01f;
    }

    private void UpdateTargetWorldPosition()
    {
        if (cameraTransform == null || aimTarget == null)
            return;

        Vector3 screenPos = new Vector3(
            _targetScreenOffset.x * Screen.width,
            _targetScreenOffset.y * Screen.height,
            targetDistance
        );

        aimTarget.position = Camera.main.ScreenToWorldPoint(screenPos);
    }

    private void HandleMovement()
    {
        if (_hasSpeedBuff && Time.time >= _buffEndTime)
        {
            _hasSpeedBuff = false;
            _currentSpeedMultiplier = 1f;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        float speed = moveDirection.magnitude;
        animator.SetFloat("MoveX", speed);

        if (speed > 0.1f)
        {
            float currentSpeed = baseMovementSpeed * _currentSpeedMultiplier;
            transform.Translate(moveDirection * (currentSpeed * Time.deltaTime), Space.World);
        }
    }
    public void ApplySpeedBuff(float multiplier, float duration)
    {
        _currentSpeedMultiplier = multiplier;
        _buffEndTime = Time.time + duration;
        _hasSpeedBuff = true;
    }
    private void HandleShooting()
    {
        if (!_isAiming) return;

        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            Shoot();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || muzzle == null)
        {
            return;
        }

        Vector3 direction = (aimTarget.position - muzzle.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction, GetBulletDamage());
        }

        if (shootSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(shootSound);
        }
    }
    public void AddDamageBonus(int bonus)
    {
        _bonusDamage += bonus;
    }

    public int GetBulletDamage()
    {
        return baseBulletDamage + _bonusDamage;
    }
}