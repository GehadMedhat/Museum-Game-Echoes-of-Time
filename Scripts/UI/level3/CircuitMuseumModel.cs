using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to CircuitManager (an empty GameObject).
/// broken_circuit and fixed_circuit should be pure 3D meshes with NO scripts.
/// </summary>
public class CircuitMuseumModel : MonoBehaviour
{
    [Header("Museum Models")]
    [SerializeField] private GameObject brokenCircuit;
    [SerializeField] private GameObject fixedCircuit;

    [Header("Optional Effects")]
    [SerializeField] private ParticleSystem celebrationParticles;
    [SerializeField] private AudioClip      activationSound;
    [SerializeField] private Light          revealLight;

    private AudioSource _audioSource;
    private bool        _revealed = false;

private void Awake()
{
    // Clear solved state on every play session
    PlayerPrefs.DeleteKey("CircuitPuzzleSolved");
    PlayerPrefs.Save();

    _audioSource = GetComponent<AudioSource>();
    if (_audioSource == null && activationSound != null)
        _audioSource = gameObject.AddComponent<AudioSource>();

    if (brokenCircuit != null) brokenCircuit.SetActive(true);
    if (fixedCircuit  != null) fixedCircuit.SetActive(false);
}

    public void Reveal()
    {
        if (_revealed) return;
        _revealed = true;

        if (brokenCircuit != null) brokenCircuit.SetActive(false);
        if (fixedCircuit  != null) fixedCircuit.SetActive(true);

        if (celebrationParticles != null) celebrationParticles.Play();

        if (_audioSource != null && activationSound != null)
        {
            _audioSource.clip = activationSound;
            _audioSource.Play();
        }

        if (revealLight != null)
            StartCoroutine(PulseLight());

        Debug.Log("[CircuitMuseumModel] Swapped to fixed circuit.");
    }

    public void Hide()
    {
        _revealed = false;
        if (brokenCircuit != null) brokenCircuit.SetActive(true);
        if (fixedCircuit  != null) fixedCircuit.SetActive(false);
    }

    private IEnumerator PulseLight()
    {
        float original = revealLight.intensity;
        revealLight.enabled = true;
        for (int i = 0; i < 4; i++)
        {
            revealLight.intensity = original * 3f;
            yield return new WaitForSeconds(0.2f);
            revealLight.intensity = original;
            yield return new WaitForSeconds(0.2f);
        }
    }
}
