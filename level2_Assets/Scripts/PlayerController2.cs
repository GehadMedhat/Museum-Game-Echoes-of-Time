using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController2 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 80f;

    // Boundaries
[Header("Map Bounds")]
public float minX = -20f;
public float maxX = 20f;
public float minZ = -20f;
public float maxZ = 20f;

    [HideInInspector]
    public bool canMove = true;

    private CharacterController _cc;
    private Animator animator;
    private AudioSource footstepAudio;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Find Animator inside character model
        animator = GetComponentInChildren<Animator>();
        footstepAudio = GetComponent<AudioSource>();

        // Always keep cursor visible
        Cursor.visible = true;
    }

    void Update()
    {
        // Keep cursor visible all the time
        Cursor.visible = true;

        // Stop movement if disabled
        if (!canMove)
        {
            if (animator != null)
                animator.SetBool("isWalking", false);

            return;
        }

        // Get keyboard input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Check if player is moving
        bool isMoving = Mathf.Abs(v) > 0.1f;
        if (footstepAudio != null)
{
    if (isMoving)
    {
        if (!footstepAudio.isPlaying)
            footstepAudio.Play();
    }
    else
    {
        if (footstepAudio.isPlaying)
            footstepAudio.Stop();
    }
}

        // Update animation
        if (animator != null)
        {
            animator.SetBool("isWalking", isMoving);
        }

        // Rotate using A / D
        transform.Rotate(0f, h * rotateSpeed * Time.deltaTime, 0f);

        // Move using W / S
        Vector3 move = transform.forward * v;

        // Move player
        _cc.Move(move * moveSpeed * Time.deltaTime);

        Vector3 pos = transform.position;

pos.x = Mathf.Clamp(pos.x, minX, maxX);
pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

transform.position = pos;
    }
}
