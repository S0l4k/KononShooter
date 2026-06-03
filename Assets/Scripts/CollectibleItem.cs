using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Damage Bonus")]
    [SerializeField] private int damageBonus = 5; 

    [Header("Effects")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;

    [Header("References")]
    [SerializeField] private POI parentPOI;

    private bool _isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_isCollected) return;

        Transform playerTransform = other.transform;
        while (playerTransform != null)
        {
            if (playerTransform.CompareTag("Player"))
            {
                Collect(playerTransform.gameObject);
                return;
            }
            playerTransform = playerTransform.parent;
        }
    }

    private void Collect(GameObject player)
    {
        if (_isCollected) return;
        _isCollected = true;

        Vector3 collectPosition = transform.position;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.AddDamageBonus(damageBonus);
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, collectPosition, collectVolume);
        }


        if (parentPOI != null)
        {
            parentPOI.DisablePOI();
        }

        if (parentPOI != null)
        {
            Destroy(parentPOI.gameObject);
        }
        else
        {
            Destroy(transform.parent.gameObject);
        }
    }
}