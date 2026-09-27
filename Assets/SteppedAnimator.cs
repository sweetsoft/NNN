using UnityEngine;

/// <summary>Put on the character root to step all part Animators together.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
[AddComponentMenu("Animation/Stepped Animator")]
public sealed class SteppedAnimator : MonoBehaviour
{
    [SerializeField, Min(0.001f)] private float stepInterval = 0.1f;
    [Tooltip("Body Animator. All parts must use the same Animator Controller.")]
    [SerializeField] private Animator masterAnimator;
    /// <summary>再生ステートの照合元。Playはパーツ同期を保つためグループAPIを使用する。</summary>
    public Animator MasterAnimator => masterAnimator;

    private Animator[] animators;
    private bool[] originalEnabled;
    private bool[] originalKeepState;
    private AnimatorCullingMode[] originalCulling;
    private AnimatorControllerParameter[] parameters;
    private float elapsed;

    private void OnEnable()
    {
        animators = GetComponentsInChildren<Animator>(true);
        if (animators.Length == 0)
            return;
        if (masterAnimator == null)
            masterAnimator = animators[0];
        if (System.Array.IndexOf(animators, masterAnimator) < 0 || masterAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("Stepped Animator: select a child Animator with a Controller.", this);
            enabled = false;
            return;
        }
        foreach (Animator part in animators)
        {
            if (part.runtimeAnimatorController != masterAnimator.runtimeAnimatorController)
            {
                Debug.LogError("Stepped Animator: all parts must use the same Animator Controller.", part);
                enabled = false;
                return;
            }
        }
        parameters = masterAnimator.parameters;
        originalEnabled = new bool[animators.Length];
        originalKeepState = new bool[animators.Length];
        originalCulling = new AnimatorCullingMode[animators.Length];
        elapsed = 0f;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator part = animators[i];
            originalEnabled[i] = part.enabled;
            originalKeepState[i] = part.keepAnimatorStateOnDisable;
            originalCulling[i] = part.cullingMode;
            part.keepAnimatorStateOnDisable = true;
            part.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            part.enabled = false;
        }
        // Start every part at the same point, including on re-enable.
        foreach (Animator part in animators)
        {
            if (!part.gameObject.activeInHierarchy) continue;
            part.Rebind();
            part.Update(0f);
        }
    }

    private void LateUpdate()
    {
        if (originalEnabled == null || masterAnimator == null) return;
        elapsed += masterAnimator.updateMode == AnimatorUpdateMode.UnscaledTime
            ? Time.unscaledDeltaTime : Time.deltaTime;
        float interval = Mathf.Max(0.001f, stepInterval);
        while (elapsed >= interval)
        {
            elapsed -= interval;
            // Copy before evaluating: transitions may consume parameters during Update.
            foreach (Animator part in animators)
            {
                if (part == null || part == masterAnimator) continue;
                part.speed = masterAnimator.speed;
                foreach (var parameter in parameters)
                {
                    int id = parameter.nameHash;
                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Float: part.SetFloat(id, masterAnimator.GetFloat(id)); break;
                        case AnimatorControllerParameterType.Int: part.SetInteger(id, masterAnimator.GetInteger(id)); break;
                        case AnimatorControllerParameterType.Bool: part.SetBool(id, masterAnimator.GetBool(id)); break;
                    }
                }
                for (int layer = 1; layer < masterAnimator.layerCount; layer++)
                    part.SetLayerWeight(layer, masterAnimator.GetLayerWeight(layer));
            }
            foreach (Animator part in animators)
            {
                if (part != null && part.gameObject.activeInHierarchy) part.Update(interval);
                if (!isActiveAndEnabled) return;
            }
        }
    }

    // Use these group methods instead of calling Play/SetTrigger on just one part.
    public void Play(string stateName, int layer = 0)
    {
        if (originalEnabled == null) return;
        elapsed = 0f;
        foreach (Animator part in animators)
        {
            if (part == null || !part.gameObject.activeInHierarchy) continue;
            part.Play(stateName, layer, 0f);
            part.Update(0f);
        }
    }

    public void SetTrigger(string parameterName)
    {
        if (originalEnabled == null) return;
        foreach (Animator part in animators)
            if (part != null) part.SetTrigger(parameterName);
    }

    private void OnDisable()
    {
        if (originalEnabled == null) return;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator part = animators[i];
            if (part == null) continue;
            part.enabled = originalEnabled[i];
            part.keepAnimatorStateOnDisable = originalKeepState[i];
            part.cullingMode = originalCulling[i];
        }
        originalEnabled = null;
        elapsed = 0f;
    }
}
