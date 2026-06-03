using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int defaultDamage = 10;

    private Vector3 _direction;
    private float _lifeTimer;
    private int _damage;

    public void Initialize(Vector3 direction, int damage = -1)
    {
        _direction = direction.normalized;
        transform.forward = _direction;
        _lifeTimer = lifetime;

        _damage = (damage >= 0) ? damage : defaultDamage;
    }

    private void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
}