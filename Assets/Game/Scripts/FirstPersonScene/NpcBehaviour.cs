using UnityEngine;
using UnityEngine.Events;

public class NpcBehaviour : MonoBehaviour, IInteractable
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "Stranger";
    [SerializeField] private Transform lookTarget; // Where the NPC should look (usually player's head)
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Transform dialogueAnchor; // Where to position dialogue UI

    [Header("Dialogue")]
    [SerializeField] private DialogueData[] dialogues;
    [SerializeField] private int currentDialogueIndex = 0;

    [Header("Events")]
    [SerializeField] private UnityEvent onInteractionStart;
    [SerializeField] private UnityEvent onInteractionEnd;

    private bool _isPlayerInRange = false;
    private Transform _playerTransform;
    private Animator _animator;
    private bool _isInDialogue = false;

    private void Start()
    {
        _animator = GetComponent<Animator>();

        // Create dialogue anchor if not assigned
        if (dialogueAnchor == null)
        {
            GameObject anchorObj = new GameObject("DialogueAnchor");
            dialogueAnchor = anchorObj.transform;
            dialogueAnchor.parent = transform;
            dialogueAnchor.localPosition = new Vector3(0, 2f, 0); // Above NPC's head
        }
    }

    private void Update()
    {
        // Look at player when in range
        if (_isPlayerInRange && lookTarget != null)
        {
            Vector3 direction = lookTarget.position - transform.position;
            direction.y = 0; // Keep NPC upright

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
            }
        }

        // Show interaction prompt when in range
        if (_isPlayerInRange && !_isInDialogue)
        {
            UIManager.Instance.ShowInteractionPrompt("Press E to talk to " + npcName);
        }
    }

    public void Interact(FirstPersonController player)
    {
        if (!_isInDialogue)
        {
            StartDialogue(player);
        }
        else
        {
            AdvanceDialogue();
        }
    }

    private void StartDialogue(FirstPersonController player)
    {
        _isInDialogue = true;
        _playerTransform = player.transform;

        // Trigger animation if available
        _animator?.SetTrigger("Talk");

        // Show dialogue UI
        DialogueManager.Instance.StartDialogue(this, dialogues[currentDialogueIndex], dialogueAnchor);

        // Invoke events
        onInteractionStart?.Invoke();

        // Disable player movement (optional)
        player.SetControlsActive(false);
    }

    private void AdvanceDialogue()
    {
        DialogueManager.Instance.AdvanceDialogue();
    }

    public void EndDialogue()
    {
        _isInDialogue = false;

        // Reset animation
        _animator?.SetTrigger("Idle");

        // Increment dialogue index if there are more dialogues
        currentDialogueIndex = (currentDialogueIndex + 1) % dialogues.Length;

        // Invoke events
        onInteractionEnd?.Invoke();

        // Re-enable player movement
        if (_playerTransform != null)
        {
            _playerTransform.GetComponent<FirstPersonController>()?.SetControlsActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            lookTarget = other.transform.Find("CameraTarget") ?? other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            UIManager.Instance.HideInteractionPrompt();

            // End dialogue if player walks away
            if (_isInDialogue)
            {
                EndDialogue();
            }
        }
    }
}