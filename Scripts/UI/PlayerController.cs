using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed   = 20f;
    [SerializeField] private float rotateSpeed = 200f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float camDistance = 5f;
    [SerializeField] private float camHeight   = 3f;
    [SerializeField] private float camSmooth   = 8f;

    [Header("Interaction")]
    [SerializeField] private float     interactRange = 5f;
    [SerializeField] private LayerMask statueLayer;

[HideInInspector] public bool canMove = true;

    [Header("Gravity")]
    [SerializeField] private float gravityMultiplier = 1.5f;

    [Header("Audio – Footsteps")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;          // assign 2-4 step sounds
    [SerializeField] private float       footstepInterval = 0.4f; // seconds between steps

    [Header("Audio – Note UI")]
    [SerializeField] private AudioSource noteAudioSource;
    [SerializeField] private AudioClip   noteOpenClip;
    [SerializeField] private AudioClip   noteCloseClip;

    private bool  _puzzleOpen        = false;
    private float _footstepTimer     = -1f;
    private int   _footstepIndex     = 0;
    private bool  _noteWasOpen       = false;

    private CharacterController _cc;
    private Animator            _anim;
    private Transform           _cam;
    private Vector3             _velocity;
    private bool                _isGrounded;

    private void Awake()
    {
        _cc   = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _cam  = Camera.main.transform;
    }

    private void Start()
    {
        _velocity = Vector3.zero;
        SnapToGround();
    }

    private void SnapToGround()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 5f,
                            Vector3.down,
                            out RaycastHit hit,
                            100f))
        {
            _cc.enabled = false;
            transform.position = hit.point;
            _cc.enabled = true;
        }
    }

    private void Update()
    {
        // ── Note open / close sound ───────────────────────────────────
        bool noteOpen = NoteUI.Instance != null && NoteUI.Instance.IsOpen;
        if (noteOpen && !_noteWasOpen)   PlayNoteSound(noteOpenClip);
        if (!noteOpen && _noteWasOpen)   PlayNoteSound(noteCloseClip);
        _noteWasOpen = noteOpen;

        if (noteOpen)    return;
        if (_puzzleOpen) return;

        HandleMovement();
        HandleCamera();
        HandleInteraction();
    }

    private void HandleMovement()
    {
if (!canMove) return;
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h =  1f;

        float v = 0f;
        if (Input.GetKey(KeyCode.UpArrow)   || Input.GetKey(KeyCode.W)) v =  1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) v = -1f;

        transform.Rotate(0f, h * rotateSpeed * Time.deltaTime, 0f);

        Vector3 move = transform.forward * v * moveSpeed;

        _isGrounded = _cc.isGrounded;
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -4f;
        else
            _velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        _cc.Move((move + _velocity) * Time.deltaTime);

        _anim.SetBool("isWalking", v != 0f);

        // ── Footstep audio ────────────────────────────────────────────
        bool moving = v != 0f && _isGrounded;
        if (moving)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                PlayFootstep();
                _footstepTimer = footstepInterval;
            }
        }
        else
        {
            // Force negative so the very first step fires immediately next move
            _footstepTimer = -1f;
        }
    }

    private void HandleCamera()
    {
        Vector3 target      = transform.position + Vector3.up * 1f;
        Vector3 desiredPos  = transform.position
                            - transform.forward * camDistance
                            + Vector3.up * camHeight;

        // Pull camera in if a wall is between player and desired position
        Vector3 direction = desiredPos - target;
        float   dist      = direction.magnitude;

        if (Physics.SphereCast(target, 0.2f, direction.normalized,
                               out RaycastHit hit, dist))
        {
            // Stop just in front of the wall
            desiredPos = target + direction.normalized * (hit.distance - 0.1f);
        }

        _cam.position = Vector3.Lerp(_cam.position, desiredPos, camSmooth * Time.deltaTime);
        _cam.LookAt(target);
    }

    private float _interactCooldown = 0f;

    private void HandleInteraction()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        Debug.Log("[PlayerController] E pressed");

        if (Time.realtimeSinceStartup < _interactCooldown)
        {
            Debug.Log($"[PlayerController] On cooldown, {_interactCooldown - Time.realtimeSinceStartup:F1}s remaining");
            return;
        }
        _interactCooldown = Time.realtimeSinceStartup + 1.5f;

        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        Debug.Log($"[PlayerController] SphereCast from {rayOrigin} forward {transform.forward} range {interactRange} layer {statueLayer.value}");

        // ── All interactable types ─────────────────────────────────────────
        StatueInteractable        statue        = null;
        PuzzleInteractable        puzzle        = null;   // Level 1 — unchanged
        CircuitPuzzleInteractable circuitPuzzle = null;   // Level 3
        ExhibitInteractable       exhibit       = null;

        if (Physics.SphereCast(rayOrigin, 0.8f, transform.forward,
                               out RaycastHit hit, interactRange, statueLayer))
        {
            Debug.Log($"[PlayerController] SphereCast HIT: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            statue        = hit.collider.GetComponent<StatueInteractable>();
            puzzle        = hit.collider.GetComponent<PuzzleInteractable>();        // Level 1 — unchanged
            circuitPuzzle = hit.collider.GetComponent<CircuitPuzzleInteractable>(); // Level 3
            exhibit       = hit.collider.GetComponent<ExhibitInteractable>();

            Debug.Log($"[PlayerController] Components — Statue:{statue != null}  Puzzle(L1):{puzzle != null}  CircuitPuzzle(L3):{circuitPuzzle != null}  Exhibit:{exhibit != null}");
        }
        else
        {
            Debug.LogWarning("[PlayerController] SphereCast hit NOTHING — check layer mask or distance");
        }

        if      (statue        != null) StartCoroutine(PointThenOpen(statue, null,   null,          null));
        else if (puzzle        != null) StartCoroutine(PointThenOpen(null,   puzzle, null,          null));
        else if (circuitPuzzle != null) StartCoroutine(PointThenOpen(null,   null,   circuitPuzzle, null));
        else if (exhibit       != null) StartCoroutine(PointThenOpen(null,   null,   null,          exhibit));
        else Debug.LogWarning("[PlayerController] Hit something but no interactable component found on it!");
    }

    private IEnumerator PointThenOpen(StatueInteractable        statue,
                                      PuzzleInteractable        puzzle,        // Level 1 — unchanged
                                      CircuitPuzzleInteractable circuitPuzzle, // Level 3
                                      ExhibitInteractable       exhibit)
    {
        _anim.SetBool("isWalking", false);
        _velocity = Vector3.zero;
        _anim.SetTrigger("interact");

        yield return new WaitForSeconds(0.3f);

        _anim.ResetTrigger("interact");

        if (statue        != null) statue.OnInteract();
        if (puzzle        != null) puzzle.OnInteract();         // Level 1 — unchanged
        if (circuitPuzzle != null) circuitPuzzle.OnInteract();  // Level 3
        if (exhibit       != null) exhibit.OnInteract();
    }

    public void ResetInteractCooldown()
    {
        _interactCooldown = 0f;
    }

    public void SetPuzzleOpen(bool isOpen)
    {
        _puzzleOpen = isOpen;
    }

    // ── Audio helpers ─────────────────────────────────────────────────
    private void PlayFootstep()
    {
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        footstepAudioSource.clip = footstepClips[_footstepIndex % footstepClips.Length];
        footstepAudioSource.Play();
        _footstepIndex++;
    }

    private void PlayNoteSound(AudioClip clip)
    {
        if (noteAudioSource == null || clip == null) return;
        noteAudioSource.PlayOneShot(clip);
    }
}
