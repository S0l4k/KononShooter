using UnityEngine;
using UnityEngine.Animations.Rigging;
using DG.Tweening;

public class POI : MonoBehaviour
{
    private RigBuilder _rigBuilder;
    private MultiAimConstraint _aimConstraint;
    private int _sourceIndex = -1;
    private float _weight = 0f;
    private Tween _weightTween;
    private bool _isDisabled = false;
    private bool _isSourceAdded = false;

    private void Start()
    {
        if (PlayerRigsProvider.Instance == null)
        {
            return;
        }

        _rigBuilder = PlayerRigsProvider.Instance.rigBuilder;
        _aimConstraint = PlayerRigsProvider.Instance.headConstraint;

        if (_rigBuilder == null || _aimConstraint == null)
        {
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDisabled) return;

        Transform playerTransform = other.transform;
        while (playerTransform != null)
        {
            if (playerTransform.CompareTag("Player"))
            {
                OnPlayerEnter();
                return;
            }
            playerTransform = playerTransform.parent;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isDisabled) return;

        Transform playerTransform = other.transform;
        while (playerTransform != null)
        {
            if (playerTransform.CompareTag("Player"))
            {
                OnPlayerExit();
                return;
            }
            playerTransform = playerTransform.parent;
        }
    }

    private void OnPlayerEnter()
    {
        if (_isDisabled) return;

        if (!_isSourceAdded)
        {
            AddSourceToConstraint();
        }

        if (_weightTween != null)
            _weightTween.Kill();

        _weightTween = DOTween.To(
            () => _weight,
            x => SetSourceWeight(x),
            1f,
            0.3f
        );
    }

    private void OnPlayerExit()
    {
        if (_isDisabled) return;

        if (_weightTween != null)
            _weightTween.Kill();

        _weightTween = DOTween.To(
            () => _weight,
            x => SetSourceWeight(x),
            0f,
            0.3f
        );
    }

    private void AddSourceToConstraint()
    {
        if (_aimConstraint == null)
        {
            return;
        }

        var data = _aimConstraint.data;
        var sources = data.sourceObjects;

        _sourceIndex = sources.Count;
        sources.Add(new WeightedTransform(transform, 0f));

        data.sourceObjects = sources;
        _aimConstraint.data = data;

        _rigBuilder.Build();

        _isSourceAdded = true;
    }

    private void RemoveSourceFromConstraint()
    {
        if (!_isSourceAdded || _aimConstraint == null) return;

        var data = _aimConstraint.data;
        var sources = data.sourceObjects;

        if (_sourceIndex >= 0 && _sourceIndex < sources.Count)
        {
            sources.RemoveAt(_sourceIndex);
            data.sourceObjects = sources;
            _aimConstraint.data = data;
            _rigBuilder.Build();
        }

        _isSourceAdded = false;
        _sourceIndex = -1;
    }

    private void SetSourceWeight(float weight)
    {
        if (_aimConstraint == null || _sourceIndex < 0) return;

        var data = _aimConstraint.data;
        var sources = data.sourceObjects;

        if (_sourceIndex < sources.Count)
        {
            sources.SetWeight(_sourceIndex, weight);
            data.sourceObjects = sources;
            _aimConstraint.data = data;
        }
    }

    public void DisablePOI()
    {
        _isDisabled = true;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        if (_weightTween != null)
            _weightTween.Kill();

        _weightTween = DOTween.To(
            () => _weight,
            x => SetSourceWeight(x),
            0f,
            0.3f
        ).OnComplete(() =>
        {
            RemoveSourceFromConstraint();
        });

    }
}