using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerRigsProvider : MonoBehaviour
{
    public static PlayerRigsProvider Instance;

    public RigBuilder rigBuilder;
    public MultiAimConstraint headConstraint;

    private void Awake()
    {
        Instance = this;
    }
}