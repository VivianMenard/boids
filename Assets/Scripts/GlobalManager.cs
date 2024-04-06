using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [SerializeField]
    private Text fpsUiText;
    [SerializeField, Range(0.01f, 1f), Tooltip("Time in seconds between fps display refreshing")]
    private float fpsRefreshPeriod;

    private float fpsTimer;
    private int nbFrameSinceLastFpsUpdate;

    void Update()
    {
        ManageFpsDisplay();
    }

    private void ManageFpsDisplay()
    {
        nbFrameSinceLastFpsUpdate++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer > fpsRefreshPeriod)
        {
            int averageFps = (int)(nbFrameSinceLastFpsUpdate / fpsTimer);
            fpsUiText.text = averageFps + " fps";

            fpsTimer = 0f;
            nbFrameSinceLastFpsUpdate = 0;
        }
    }
}
