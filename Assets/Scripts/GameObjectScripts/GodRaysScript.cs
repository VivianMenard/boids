using UnityEngine;

/// <summary>
/// Just few tools to help with godrays set dressing.
/// </summary>
public class GodRaysScript : MonoBehaviour
{
    /// <summary>
    /// Automatically rotate all god rays to make them come from the center (the first child in the hierarchy).
    /// </summary>
    [ContextMenu("Recompute god rays rotation")]
    private void ComputeGodRaysRotation()
    {
        Transform center = transform.GetChild(0);

        for (int childNumber = 1; childNumber < transform.childCount; childNumber++)
        {
            Transform godRay = transform.GetChild(childNumber);
            godRay.rotation = Quaternion.LookRotation(godRay.position - center.position);
        }
    }

    /// <summary>
    /// Automatically rename all god rays according to a template defined in <c>Constants.cs</c>.
    /// </summary>
    [ContextMenu("Rename god rays")]
    private void RenameGodRays()
    {
        for (int childNumber = 1; childNumber < transform.childCount; childNumber++)
            transform.GetChild(childNumber).name = string.Format(
                Constants.godraysNameTemplate,
                childNumber.ToString()
            );
    }
}
