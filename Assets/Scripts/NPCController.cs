using UnityEngine;
using UnityEngine.Animations.Rigging;
using DG.Tweening;

public class NPCController : MonoBehaviour, IDamageable
{
    [Header("Animacje")]
    [SerializeField] private Animator animator;
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Rigi")]
    [SerializeField] private Rig talkRig;
    [SerializeField] private Rig stareRig;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Audio - Dźwięki rozmowy (gdy NPC rozmawia ze sobą)")]
    [SerializeField] private AudioClip[] talkSounds;
    [SerializeField, Range(1f, 5f)] private float talkSoundInterval = 3f;
    [SerializeField, Range(0f, 1f)] private float talkVolume = 1f;

    [Header("Audio - Dźwięki reakcji na gracza")]
    [SerializeField] private AudioClip[] playerReactionSounds;
    [SerializeField, Range(1f, 5f)] private float reactionSoundInterval = 2.5f;
    [SerializeField, Range(0f, 1f)] private float reactionVolume = 1f;

    [Header("Audio - Trafienie")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;

    [Header("Audio - Śmierć")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    private bool _isPlayerInRange = false;
    private bool _isDead = false;
    private int _currentHealth;
    private float _talkWeight = 1f;
    private float _stareWeight = 0f;
    private Tween _talkTween;
    private Tween _stareTween;
    private Tween _animTween;

    private void Start()
    {
        _currentHealth = maxHealth;

        talkRig.weight = 1f;
        stareRig.weight = 0f;
        animator.SetFloat("TalkWeight", 1f);

        StartTalkingSounds();
    }

    private void StartTalkingSounds()
    {
        CancelInvoke(nameof(PlayRandomTalkSound));
        CancelInvoke(nameof(PlayRandomReactionSound));

        InvokeRepeating(nameof(PlayRandomTalkSound), 0f, talkSoundInterval);
    }

    private void StartReactionSounds()
    {
        CancelInvoke(nameof(PlayRandomTalkSound));
        CancelInvoke(nameof(PlayRandomReactionSound));

        InvokeRepeating(nameof(PlayRandomReactionSound), 0f, reactionSoundInterval);
    }

    private void StopAllSounds()
    {
        CancelInvoke(nameof(PlayRandomTalkSound));
        CancelInvoke(nameof(PlayRandomReactionSound));
    }

    private void PlayRandomTalkSound()
    {
        if (_isDead || talkSounds == null || talkSounds.Length == 0) return;

        AudioClip clip = talkSounds[Random.Range(0, talkSounds.Length)];
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, talkVolume);
        }
    }

    private void PlayRandomReactionSound()
    {
        if (_isDead || playerReactionSounds == null || playerReactionSounds.Length == 0) return;

        AudioClip clip = playerReactionSounds[Random.Range(0, playerReactionSounds.Length)];
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, reactionVolume);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
            }
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterTalkativeKill();
        }
        StopAllSounds();
        KillTweens();

        talkRig.weight = 0f;
        stareRig.weight = 0f;

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 0.5f);
    }

    public void OnPlayerEnter()
    {
        if (_isDead) return;
        if (_isPlayerInRange) return;
        _isPlayerInRange = true;

        StartReactionSounds();
        SwitchToPlayer();
    }

    public void OnPlayerExit()
    {
        if (_isDead) return;
        if (!_isPlayerInRange) return;
        _isPlayerInRange = false;

        StartTalkingSounds();
        SwitchToNPC();
    }

    private void SwitchToPlayer()
    {
        KillTweens();

        _talkTween = DOTween.To(() => _talkWeight, x => SetTalkWeight(x), 0f, transitionDuration);
        _stareTween = DOTween.To(() => _stareWeight, x => SetStareWeight(x), 1f, transitionDuration);

        _animTween = DOTween.To(
            () => animator.GetFloat("TalkWeight"),
            x => animator.SetFloat("TalkWeight", x),
            0f,
            transitionDuration
        );
    }

    private void SwitchToNPC()
    {
        KillTweens();

        _talkTween = DOTween.To(() => _talkWeight, x => SetTalkWeight(x), 1f, transitionDuration);
        _stareTween = DOTween.To(() => _stareWeight, x => SetStareWeight(x), 0f, transitionDuration);

        _animTween = DOTween.To(
            () => animator.GetFloat("TalkWeight"),
            x => animator.SetFloat("TalkWeight", x),
            1f,
            transitionDuration
        );
    }

    private void SetTalkWeight(float weight)
    {
        _talkWeight = weight;
        talkRig.weight = weight;
    }

    private void SetStareWeight(float weight)
    {
        _stareWeight = weight;
        stareRig.weight = weight;
    }

    private void KillTweens()
    {
        if (_talkTween != null) _talkTween.Kill();
        if (_stareTween != null) _stareTween.Kill();
        if (_animTween != null) _animTween.Kill();
    }
}