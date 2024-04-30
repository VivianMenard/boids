using UnityEngine;

public class GodRays : MonoBehaviour
{

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
