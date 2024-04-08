using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [SerializeField]
    private GameObject entitiesManager;
    [SerializeField]
    private GameObject Ui;

    [Space, SerializeField]
    private Toggle fpsToggle;
    [SerializeField]
    private Text fpsDisplay;
    [SerializeField, Range(0.01f, 1f), Tooltip("Time in seconds between fps display refreshing")]
    private float fpsRefreshPeriod;

    [Space, SerializeField]
    private Text nbBoidsDisplay;
    [SerializeField]
    private Button addBoidsButton;
    [SerializeField]
    private Button removeBoidsButton;
    [SerializeField, Range(0, 5000)]
    private int maxNbBoidsInUi;
    [SerializeField, Range(0, 500)]
    private int nbBoidsStep;

    [Space, SerializeField]
    private Text nbPredatorsDisplay;
    [SerializeField]
    private Button addPredatorsButton;
    [SerializeField]
    private Button removePredatorsButton;
    [SerializeField, Range(0, 20)]
    private int maxNbPredatorsInUi;
    [SerializeField, Range(0, 5)]
    private int nbPredatorsStep;

    private EntitiesManagerScript entitiesManagerScript;

    private float fpsTimer;
    private int nbFrameSinceLastFpsUpdate;

    private bool displayUi = true;

    void Start()
    {
        entitiesManagerScript = entitiesManager.GetComponent<EntitiesManagerScript>();

        UpdateNbBoidsDisplay();
        UpdateNbPredatorsDisplay();
    }

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

    public void AddBoids()
    {
        int newNbBoids = Mathf.Min(
            entitiesManagerScript.numberOfBoids + nbBoidsStep,
            maxNbBoidsInUi
        );

        entitiesManagerScript.numberOfBoids = newNbBoids;
        UpdateNbBoidsDisplay();
        UpdateBoidsButtonsInteractivity();
    }

    public void RemoveBoids()
    {
        int newNbBoids = Mathf.Max(
            entitiesManagerScript.numberOfBoids - nbBoidsStep,
            0
        );

        entitiesManagerScript.numberOfBoids = newNbBoids;
        UpdateNbBoidsDisplay();
        UpdateBoidsButtonsInteractivity();
    }

    public void AddPredators()
    {
        int newNbPredators = Mathf.Min(
            entitiesManagerScript.numberOfPredators + nbPredatorsStep,
            maxNbPredatorsInUi
        );

        entitiesManagerScript.numberOfPredators = newNbPredators;
        UpdateNbPredatorsDisplay();
        UpdatePredatorsButtonsInteractivity();
    }

    public void RemovePredators()
    {
        int newNbPredators = Mathf.Max(
            entitiesManagerScript.numberOfPredators - nbPredatorsStep,
            0
        );

        entitiesManagerScript.numberOfPredators = newNbPredators;
        UpdateNbPredatorsDisplay();
        UpdatePredatorsButtonsInteractivity();
    }

    public void UpdateBoidsButtonsInteractivity()
    {
        addBoidsButton.interactable = entitiesManagerScript.numberOfBoids < maxNbBoidsInUi;
        removeBoidsButton.interactable = entitiesManagerScript.numberOfBoids > 0;
    }

    public void UpdatePredatorsButtonsInteractivity()
    {
        addPredatorsButton.interactable = entitiesManagerScript.numberOfPredators < maxNbPredatorsInUi;
        removePredatorsButton.interactable = entitiesManagerScript.numberOfPredators > 0;
    }

    private void UpdateNbBoidsDisplay()
    {
        int nbBoids = entitiesManagerScript.numberOfBoids;
        string formattedNbBoids = (nbBoids > 0) ? nbBoids.ToString("#,###") : nbBoids.ToString();
        string strToAdd = (nbBoids > 1) ? " fishes" : " fish";
        nbBoidsDisplay.text = formattedNbBoids + strToAdd;
    }

    private void UpdateNbPredatorsDisplay()
    {
        int nbPredators = entitiesManagerScript.numberOfPredators;
        string strToAdd = (nbPredators > 1) ? " sharks" : " shark";
        nbPredatorsDisplay.text = entitiesManagerScript.numberOfPredators + strToAdd;
    }

    private void ToggleUiDisplay()
    {
        displayUi = !displayUi;
        Ui.SetActive(displayUi);
    }

    public void ToggleFpsDisplay()
    {
        fpsDisplay.gameObject.SetActive(fpsToggle.isOn);
    }
}
