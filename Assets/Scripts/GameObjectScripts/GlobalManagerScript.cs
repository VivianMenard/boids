using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages high level informations and actions.
/// </summary>
public class GlobalManagerScript : MonoBehaviour
{
    [SerializeField]
    private GameObject Ui, paramsUi, controlsUi, pauseIcon, playIcon;
    [SerializeField]
    private Button controlsButton, paramsButton, addBoidsButton, removeBoidsButton,
    addPredatorsButton, removePredatorsButton;
    [SerializeField]
    private TextMeshProUGUI fpsDisplay, nbBoidsDisplay, nbPredatorsDisplay;
    [SerializeField]
    private Toggle fpsToggle;
    [SerializeField]
    private List<GameObject> allUiComponentsWithBgColor = new List<GameObject>();


    [Space, SerializeField, Tooltip("Normal color of the buttons.")]
    private Color buttonNormalColor;
    [SerializeField, Tooltip("Color of the button when the related tab is open.")]
    private Color buttonTabSelectedColor;
    [SerializeField, Range(0.01f, 1f), Tooltip("Time in seconds between fps display refreshing.")]
    private float fpsRefreshPeriod;
    [SerializeField, Range(0, 5000), Tooltip("Maximal number of boids that can be set by the user.")]
    private int maxNbBoidsInUi;
    [SerializeField, Range(0, 20), Tooltip("Maximal number of predators that can be set by the user.")]
    private int maxNbPredatorsInUi;
    [SerializeField, Range(0, 500), Tooltip("Number of boids added every time the user adds boids.")]
    private int nbBoidsStep;
    [SerializeField, Range(0, 5), Tooltip("Number of predators added every time the user adds predators.")]
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

    void Update()
    {
        ManageFpsDisplay();

        if (Input.GetKeyDown(KeyCode.U))
            ToggleUiDisplay();
    }

    private void OnApplicationQuit()
    {
        Shader.SetGlobalInt(Constants.shaderPauseReference, 0);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, 0f);
    }

    /// <summary>
    /// Allows to update all the components with a background color in once.
    /// </summary>
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

    /// <summary>
    /// Allows to quit the game mode or to quit the application depending wether it's the editor or a build.
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Adds <c>nbBoidsStep</c> boids to the simulation, within the limit of <c>maxNbBoidsInUi</c> boids maximum.
    /// </summary>
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

    /// <summary>
    /// Removes <c>nbBoidsStep</c> boids to the simulation.
    /// </summary>
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

    /// <summary>
    /// Adds <c>nbPredatorsStep</c> predators to the simulation, within the limit of 
    /// <c>maxNbPredatorsInUi</c> predators maximum.
    /// </summary>
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

    /// <summary>
    /// Removes <c>nbPredatorsStep</c> boids to the simulation.
    /// </summary>
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

    /// <summary>
    /// Toggles the display of the parameters tab in the UI.
    /// </summary>
    public void ToggleParamsDisplay()
    {
        controlsUi.SetActive(false);
        paramsUi.SetActive(!paramsUi.activeInHierarchy);
        UpdateButtonsColor();
    }

    /// <summary>
    /// Toggles the display of controls tab in the UI.
    /// </summary>
    public void ToggleControlsDisplay()
    {
        paramsUi.SetActive(false);
        controlsUi.SetActive(!controlsUi.activeInHierarchy);
        UpdateButtonsColor();
    }

    /// <summary>
    /// Toggles the display of FPS in the UI.
    /// </summary>
    public void ToggleFpsDisplay()
    {
        fpsDisplay.gameObject.SetActive(fpsToggle.isOn);
    }

    /// <summary>
    /// Toggles simulation pause state. Manages entities movements and shader movements.
    /// </summary>
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

    /// <summary>
    /// Pauses all shader animations.
    /// </summary>
    private void PauseShaders()
    {
        float currentTimeOffset = Shader.GetGlobalFloat(Constants.shaderTimeOffsetReference);
        Shader.SetGlobalInt(Constants.shaderPauseReference, 1);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, Time.time + currentTimeOffset);
    }

    /// <summary>
    /// Unpauses all shader animations.
    /// </summary>
    private void UnPauseShaders()
    {
        float currentTimeOffset = Shader.GetGlobalFloat(Constants.shaderTimeOffsetReference);
        float timeDiff = Time.time - currentTimeOffset;

        Shader.SetGlobalInt(Constants.shaderPauseReference, 0);
        Shader.SetGlobalFloat(Constants.shaderTimeOffsetReference, -timeDiff);
    }

    /// <summary>
    /// Computes FPS and update associated display when needed.
    /// </summary>
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

    /// <summary>
    /// Makes boids add and remove button disabled/enabled depending wether it's relevant or not to be able to
    /// add/remove boids.
    /// </summary>
    private void UpdateBoidsButtonsInteractivity()
    {
        addBoidsButton.interactable = entitiesManager.NumberOfBoids < maxNbBoidsInUi;
        removeBoidsButton.interactable = entitiesManager.NumberOfBoids > 0;
    }

    /// <summary>
    /// Makes predators add and remove button disabled/enabled depending wether it's relevant or not to be able to
    /// add/remove predators.
    /// </summary>
    private void UpdatePredatorsButtonsInteractivity()
    {
        addPredatorsButton.interactable = entitiesManager.NumberOfPredators < maxNbPredatorsInUi;
        removePredatorsButton.interactable = entitiesManager.NumberOfPredators > 0;
    }

    /// <summary>
    /// Updates the number of boids display with the number of boids in the simulation.
    /// </summary>
    private void UpdateNbBoidsDisplay()
    {
        int nbBoids = entitiesManager.NumberOfBoids;
        string formattedNbBoids = (nbBoids > 0) ? nbBoids.ToString(Constants.nbBoidsFormat)
            : nbBoids.ToString();
        string nbFishesDisplayTemplate = (nbBoids > 1) ? Constants.nbFishesDisplayTemplatePlural :
            Constants.nbFishesDisplayTemplate;
        nbBoidsDisplay.text = string.Format(nbFishesDisplayTemplate, formattedNbBoids);
    }

    /// <summary>
    /// Updates the number of predators display with the number of predators in the simulation.
    /// </summary>
    private void UpdateNbPredatorsDisplay()
    {
        int nbPredators = entitiesManager.NumberOfPredators;
        string nbSharksDisplayTemplate = (nbPredators > 1) ? Constants.nbSharksDisplayTemplatePlural :
            Constants.nbSharksDisplayTemplate;
        nbPredatorsDisplay.text = string.Format(
            nbSharksDisplayTemplate, entitiesManager.NumberOfPredators
        );
    }

    /// <summary>
    /// Toggles the display of the entire UI.
    /// </summary>
    private void ToggleUiDisplay()
    {
        Ui.SetActive(!Ui.activeInHierarchy);
    }

    /// <summary>
    /// Updates the color of a specific button regarding wether the related tab is selected or not.
    /// </summary>
    /// 
    /// <param name="button">The specific button.</param>
    /// <param name="isTabSelected">Is the related tab selected.</param>
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

    /// <summary>
    /// Updates the color of parameters button and controls button regarding wether their related tab is selected or not.
    /// </summary>
    private void UpdateButtonsColor()
    {
        UpdateButtonColor(paramsButton, paramsUi.activeInHierarchy);
        UpdateButtonColor(controlsButton, controlsUi.activeInHierarchy);
    }
}
