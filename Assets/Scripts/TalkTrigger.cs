using UnityEngine;

public class TalkTrigger : MonoBehaviour
{
    public NPCController controller;
    private bool _hasPlayer = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_hasPlayer) return;

        _hasPlayer = true;
        controller.OnPlayerEnter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!_hasPlayer) return;

        _hasPlayer = false;
        controller.OnPlayerExit();
    }
}