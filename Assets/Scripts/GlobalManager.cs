using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [SerializeField]
    private GameObject entitiesManager, Ui, paramsUi, pauseIcon, playIcon;

    [Space, SerializeField]
    private Toggle fpsToggle;
    [SerializeField]
    private Text fpsDisplay;
    [SerializeField, Range(0.01f, 1f), Tooltip("Time in seconds between fps display refreshing")]
    private float fpsRefreshPeriod;

    [Space, SerializeField]
    private Text nbBoidsDisplay;
    [SerializeField]
    private Text nbPredatorsDisplay;
    [SerializeField]
    private Button addBoidsButton, removeBoidsButton,
    addPredatorsButton, removePredatorsButton;
    [SerializeField, Range(0, 5000)]
    private int maxNbBoidsInUi;
    [SerializeField, Range(0, 500)]
    private int nbBoidsStep;
    [SerializeField, Range(0, 20)]
    private int maxNbPredatorsInUi;
    [SerializeField, Range(0, 5)]
    private int nbPredatorsStep;

    private bool pause = false;

    private EntitiesManagerScript entitiesManagerScript;

    private float fpsTimer;
    private int nbFrameSinceLastFpsUpdate;

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
            fpsDisplay.text = string.Format(Constants.fpsDisplayTemplate, averageFps);

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
        string formattedNbBoids = (nbBoids > 0) ? nbBoids.ToString(Constants.nbBoidsFormat)
            : nbBoids.ToString();
        string nbFishesDisplayTemplate = (nbBoids > 1) ? Constants.nbFishesDisplayTemplatePlural :
            Constants.nbFishesDisplayTemplate;
        nbBoidsDisplay.text = string.Format(nbFishesDisplayTemplate, formattedNbBoids);
    }

    private void UpdateNbPredatorsDisplay()
    {
        int nbPredators = entitiesManagerScript.numberOfPredators;
        string nbSharksDisplayTemplate = (nbPredators > 1) ? Constants.nbSharksDisplayTemplatePlural :
            Constants.nbSharksDisplayTemplate;
        nbPredatorsDisplay.text = string.Format(
            nbSharksDisplayTemplate, entitiesManagerScript.numberOfPredators
        );
    }

    private void ToggleUiDisplay()
    {
        Ui.SetActive(!Ui.activeInHierarchy);
    }

    public void ToggleParamsDisplay()
    {
        paramsUi.SetActive(!paramsUi.activeInHierarchy);
    }

    public void ToggleFpsDisplay()
    {
        fpsDisplay.gameObject.SetActive(fpsToggle.isOn);
    }

    public void TogglePause()
    {
        if (pause)
        {
            UnPauseShaders();
            entitiesManagerScript.entitiesMovement = true;
        }
        else
        {
            PauseShaders();
            entitiesManagerScript.entitiesMovement = false;
        }

        pause = !pause;

        pauseIcon.SetActive(!pause);
        playIcon.SetActive(pause);
    }

    private void PauseShaders()
    {
        float currentTimeOffset = Shader.GetGlobalFloat(Constants.shaderTimeOffsetReference);
        Shader.SetGlobalInt(Constants.shaderPauseReference, 1);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, Time.time + currentTimeOffset);
    }

    private void UnPauseShaders()
    {
        float currentTimeOffset = Shader.GetGlobalFloat(Constants.shaderTimeOffsetReference);
        float timeDiff = Time.time - currentTimeOffset;

        Shader.SetGlobalInt(Constants.shaderPauseReference, 0);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, -timeDiff);
    }

    private void OnApplicationQuit()
    {
        Shader.SetGlobalInt(Constants.shaderPauseReference, 0);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, 0f);
    }
}
