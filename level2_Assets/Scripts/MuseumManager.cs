using UnityEngine;
using System.Collections;
using TMPro;
using EchoesOfTime.UI;   // LevelManager namespace

public class MuseumManager : MonoBehaviour
{
    public static MuseumManager Instance;

    [Header("Game Sounds")]
    public AudioClip foundObjectSound;
    public AudioClip warningSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip openSound;
    public AudioClip closeSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    [Header("Artifact Popup")]
    public GameObject popupPanel;
    public TMP_Text titleText;
    public TMP_Text eraText;
    public TMP_Text infoText;
    public TMP_Text factText;

    [Header("Progress")]
    public TMP_Text progressText;   // Artifacts Learned: X / 5
    public TMP_Text foundText;      // Found: X / 10

    [Header("Timer UI")]
    public TMP_Text timerText;      // Time: 60s

    [Header("Panels")]
    public GameObject learnCompletePanel;
    public GameObject challengePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Win Panel Buttons")]
    public GameObject continueButton;   // "Continue to Level 3" button
    public GameObject backToMapButton;  // "Back to Map" button — hidden until Continue is closed

    [Header("Artifacts")]
    public GameObject[] artifacts;

    [Header("Player")]
    public PlayerController playerController;

    [Header("Settings")]
    public int totalArtifacts = 5;
    public int totalObjects = 10;
    public float timeLimit = 60f;

    // Learning state
    private bool[] visited;
    private int artifactsVisited = 0;

    // Finding state
    public bool findingPhase = false;
    private int objectsFound = 0;
    private float timeRemaining = 0f;
    private bool warningPlayed = false;

    void Awake()
    {
        Instance = this;
        visited = new bool[totalArtifacts];
    }

    void Start()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        musicSource = sources[0];
        sfxSource = sources[1];

        // Hide all panels
        SetActive(popupPanel, false);
        SetActive(learnCompletePanel, false);
        SetActive(challengePanel, false);
        SetActive(winPanel, false);
        SetActive(losePanel, false);

        // Hide win buttons until needed
        SetActive(continueButton, false);
        SetActive(backToMapButton, false);

        // Show learning progress only
        SetActiveText(progressText, true);
        SetActiveText(foundText, false);
        SetActiveText(timerText, false);

        UpdateArtifactProgress();

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!findingPhase)
            return;

        // Countdown timer
        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(timeRemaining, 0f));
            timerText.text = "Time: " + seconds + "s";
            timerText.color = seconds <= 10 ? Color.red : Color.white;
            if (seconds <= 10 && !warningPlayed)
            {
                warningPlayed = true;
                sfxSource.PlayOneShot(warningSound);
            }
        }

        // Time over
        if (timeRemaining <= 0f)
        {
            findingPhase = false;
            OnTimerEnd();
        }
    }

    // =====================================================
    // LEARNING PHASE
    // =====================================================

    public void ShowArtifact(ArtifactData data, int index)
    {
        if (findingPhase) return;

        StartCoroutine(PlayArtifactNarration(data));

        if (playerController != null)
            playerController.canMove = false;

        if (titleText != null) titleText.text = data.artifactName;
        if (eraText != null) eraText.text = data.era;
        if (infoText != null) infoText.text = data.description;
        if (factText != null) factText.text = "* " + data.funFact;

        SetActive(popupPanel, true);

        if (index >= 0 && index < visited.Length && !visited[index])
        {
            visited[index] = true;
            artifactsVisited++;
            UpdateArtifactProgress();
        }
    }

    public void CloseInfo()
    {
        sfxSource.Stop();
        sfxSource.PlayOneShot(closeSound);

        SetActive(popupPanel, false);

        if (playerController != null)
            playerController.canMove = true;

        if (artifactsVisited >= totalArtifacts)
            SetActive(learnCompletePanel, true);
    }

    // =====================================================
    // FINDING PHASE
    // =====================================================

    public void StartFinding()
    {
        Time.timeScale = 1f;

        // Hide learning UI
        SetActive(learnCompletePanel, false);
        SetActive(popupPanel, false);
        SetActiveText(progressText, false);

        // Hide artifacts
        if (artifacts != null)
        {
            foreach (GameObject artifact in artifacts)
            {
                if (artifact != null)
                    artifact.SetActive(false);
            }
        }

        // Show 10 hidden objects
        if (HiddenObjectsManager.Instance != null)
            HiddenObjectsManager.Instance.ActivateAll();

        // Reset counters
        objectsFound = 0;
        timeRemaining = timeLimit;
        warningPlayed = false;
        findingPhase = true;

        // Show challenge UI
        SetActive(challengePanel, true);
        SetActiveText(foundText, true);
        SetActiveText(timerText, true);

        UpdateFoundText();

        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining) + "s";
            timerText.color = Color.white;
        }

        if (playerController != null)
            playerController.canMove = true;

        Debug.Log("Finding phase started. Objects to find: " + totalObjects);
    }

    public void ObjectFound()
    {
        objectsFound++;
        sfxSource.PlayOneShot(foundObjectSound);
        UpdateFoundText();

        Debug.Log("Object found: " + objectsFound + " / " + totalObjects);

        if (objectsFound >= totalObjects)
        {
            findingPhase = false;
            OnWin();
        }
    }

    // =====================================================
    // END STATES
    // =====================================================

    void OnWin()
    {
        sfxSource.PlayOneShot(winSound);
        SetActive(challengePanel, false);
        SetActiveText(foundText, false);
        SetActiveText(timerText, false);

        if (HiddenObjectsManager.Instance != null)
            HiddenObjectsManager.Instance.DeactivateAll();

        // Show win panel with Continue button only
        SetActive(winPanel, true);
        SetActive(continueButton, true);
        SetActive(backToMapButton, false);

        if (playerController != null)
            playerController.canMove = false;
    }

    void OnTimerEnd()
    {
        sfxSource.PlayOneShot(loseSound);
        SetActive(challengePanel, false);
        SetActiveText(foundText, false);
        SetActiveText(timerText, false);

        if (HiddenObjectsManager.Instance != null)
            HiddenObjectsManager.Instance.DeactivateAll();

        SetActive(losePanel, true);

        if (playerController != null)
            playerController.canMove = false;
    }

    // =====================================================
    // WIN PANEL BUTTONS
    // =====================================================

    /// <summary>
    /// Called by the "Continue to Level 3" button on the Win Panel.
    /// Marks this era (medieval) complete, unlocks modern, then loads Level_ModernAge.
    /// </summary>
public void ContinueToLevel3()
{
    Time.timeScale   = 1f;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible   = true;

    if (LevelManager.Instance != null)
        LevelManager.Instance.CompleteAndLoadNext("medieval"); // ← was "level3"
    else
        UnityEngine.SceneManagement.SceneManager.LoadScene("level3");
}

    /// <summary>
    /// Called by the "Back to Map" button on the Win Panel.
    /// Hides the win panel and shows the Back to Map button (or goes straight to menu).
    /// </summary>


    /// <summary>
    /// Called when the player closes the Continue popup without proceeding.
    /// Hides the Continue button and reveals the Back to Map button.
    /// </summary>
    public void OnCloseContinuePopup()
    {
        SetActive(continueButton, false);
        SetActive(backToMapButton, true);
    }

    // =====================================================
    // UI HELPERS
    // =====================================================

    void UpdateArtifactProgress()
    {
        if (progressText != null)
        {
            progressText.text =
                "Artifacts Learned: " +
                artifactsVisited +
                " / " +
                totalArtifacts;
        }
    }

    void UpdateFoundText()
    {
        if (foundText != null)
        {
            foundText.text =
                "Found: " +
                objectsFound +
                " / " +
                totalObjects;
        }
    }

    void SetActive(GameObject obj, bool state)
    {
        if (obj != null)
            obj.SetActive(state);
    }
    
public void RetryLevel()
{
    Time.timeScale   = 1f;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible   = true;

    if (LevelManager.Instance != null)
        LevelManager.Instance.LoadLevel("ancient"); // ← load level 1 directly
    else
        UnityEngine.SceneManagement.SceneManager.LoadScene("level1");
}

    void SetActiveText(TMP_Text text, bool state)
    {
        if (text != null)
            text.gameObject.SetActive(state);
    }

    IEnumerator PlayArtifactNarration(ArtifactData data)
    {
        sfxSource.Stop();
        sfxSource.PlayOneShot(openSound);

        yield return new WaitForSeconds(0.5f);

        if (data.narrationSound != null)
            sfxSource.PlayOneShot(data.narrationSound);
    }
}
