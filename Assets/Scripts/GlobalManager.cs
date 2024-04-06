using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [SerializeField]
    private GameObject Ui;
    [SerializeField]
    private Text fpsDisplay;
    [SerializeField, Range(0.01f, 1f), Tooltip("Time in seconds between fps display refreshing")]
    private float fpsRefreshPeriod;

    private float fpsTimer;
    private int nbFrameSinceLastFpsUpdate;

    private bool displayUi = true;

    void Update()
    {
        ManageFpsDisplay();

        if (Input.GetKeyDown(KeyCode.U))
            ToggleUiDisplay();
    }

    private void ManageFpsDisplay()
    {
        nbFrameSinceLastFpsUpdate++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer > fpsRefreshPeriod)
        {
            int averageFps = (int)(nbFrameSinceLastFpsUpdate / fpsTimer);
            fpsDisplay.text = averageFps + " fps";

            fpsTimer = 0f;
            nbFrameSinceLastFpsUpdate = 0;
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void ToggleUiDisplay()
    {
        displayUi = !displayUi;
        Ui.SetActive(displayUi);
    }
}
