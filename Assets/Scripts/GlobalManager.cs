using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [SerializeField]
    private GameObject Ui, paramsUi, controlsUi, pauseIcon, playIcon;

    [Space, SerializeField]
    private Color buttonNormalColor, buttonTabSelectedColor;
    [SerializeField]
    private List<GameObject> allUiComponentsWithBgColor = new List<GameObject>();

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
    private Button controlsButton, paramsButton, addBoidsButton, removeBoidsButton,
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

    private EntitiesManagerScript entitiesManager;

    private float fpsTimer;
    private int nbFrameSinceLastFpsUpdate;

    void Start()
    {
        entitiesManager = Accessors.EntitiesManager;

        UpdateNbBoidsDisplay();
        UpdateNbPredatorsDisplay();
        UpdateButtonsColor();
    }

    [ContextMenu("Update components color")]
    private void UpdateComponentsColor()
    {
        foreach (GameObject component in allUiComponentsWithBgColor)
        {
            Image imageComponent = component.GetComponent<Image>();
            if (imageComponent == null)
                throw new MissingComponentException(
                    component.name +
                    ": No Image component to change the background color of."
                );

            imageComponent.color = buttonNormalColor;
        }
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
            entitiesManager.NumberOfBoids + nbBoidsStep,
            maxNbBoidsInUi
        );

        entitiesManager.NumberOfBoids = newNbBoids;
        UpdateNbBoidsDisplay();
        UpdateBoidsButtonsInteractivity();
    }

    public void RemoveBoids()
    {
        int newNbBoids = Mathf.Max(
            entitiesManager.NumberOfBoids - nbBoidsStep,
            0
        );

        entitiesManager.NumberOfBoids = newNbBoids;
        UpdateNbBoidsDisplay();
        UpdateBoidsButtonsInteractivity();
    }

    public void AddPredators()
    {
        int newNbPredators = Mathf.Min(
            entitiesManager.NumberOfPredators + nbPredatorsStep,
            maxNbPredatorsInUi
        );

        entitiesManager.NumberOfPredators = newNbPredators;
        UpdateNbPredatorsDisplay();
        UpdatePredatorsButtonsInteractivity();
    }

    public void RemovePredators()
    {
        int newNbPredators = Mathf.Max(
            entitiesManager.NumberOfPredators - nbPredatorsStep,
            0
        );

        entitiesManager.NumberOfPredators = newNbPredators;
        UpdateNbPredatorsDisplay();
        UpdatePredatorsButtonsInteractivity();
    }

    public void UpdateBoidsButtonsInteractivity()
    {
        addBoidsButton.interactable = entitiesManager.NumberOfBoids < maxNbBoidsInUi;
        removeBoidsButton.interactable = entitiesManager.NumberOfBoids > 0;
    }

    public void UpdatePredatorsButtonsInteractivity()
    {
        addPredatorsButton.interactable = entitiesManager.NumberOfPredators < maxNbPredatorsInUi;
        removePredatorsButton.interactable = entitiesManager.NumberOfPredators > 0;
    }

    private void UpdateNbBoidsDisplay()
    {
        int nbBoids = entitiesManager.NumberOfBoids;
        string formattedNbBoids = (nbBoids > 0) ? nbBoids.ToString(Constants.nbBoidsFormat)
            : nbBoids.ToString();
        string nbFishesDisplayTemplate = (nbBoids > 1) ? Constants.nbFishesDisplayTemplatePlural :
            Constants.nbFishesDisplayTemplate;
        nbBoidsDisplay.text = string.Format(nbFishesDisplayTemplate, formattedNbBoids);
    }

    private void UpdateNbPredatorsDisplay()
    {
        int nbPredators = entitiesManager.NumberOfPredators;
        string nbSharksDisplayTemplate = (nbPredators > 1) ? Constants.nbSharksDisplayTemplatePlural :
            Constants.nbSharksDisplayTemplate;
        nbPredatorsDisplay.text = string.Format(
            nbSharksDisplayTemplate, entitiesManager.NumberOfPredators
        );
    }

    private void ToggleUiDisplay()
    {
        Ui.SetActive(!Ui.activeInHierarchy);
    }

    public void ToggleParamsDisplay()
    {
        controlsUi.SetActive(false);
        paramsUi.SetActive(!paramsUi.activeInHierarchy);
        UpdateButtonsColor();
    }

    public void ToggleControlsDisplay()
    {
        paramsUi.SetActive(false);
        controlsUi.SetActive(!controlsUi.activeInHierarchy);
        UpdateButtonsColor();
    }

    private void UpdateButtonColor(Button button, bool isTabSelected)
    {
        Image imageComponent = button.GetComponent<Image>();
        if (imageComponent == null)
            throw new MissingComponentException(
                button.name +
                ": No Image component to change the background color of."
            );

        imageComponent.color = (isTabSelected) ? buttonTabSelectedColor :
            buttonNormalColor;
    }

    private void UpdateButtonsColor()
    {
        UpdateButtonColor(paramsButton, paramsUi.activeInHierarchy);
        UpdateButtonColor(controlsButton, controlsUi.activeInHierarchy);
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
            entitiesManager.EntitiesMovement = true;
        }
        else
        {
            PauseShaders();
            entitiesManager.EntitiesMovement = false;
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
