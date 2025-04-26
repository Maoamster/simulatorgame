using UnityEngine;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private float autoAdvanceDelay = 0.5f;

    private NpcBehaviour _currentNPC;
    private DialogueData _currentDialogue;
    private int _currentLineIndex;
    private Transform _dialogueAnchor;
    private Coroutine _autoAdvanceCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartDialogue(NpcBehaviour npc, DialogueData dialogue, Transform anchor)
    {
        _currentNPC = npc;
        _currentDialogue = dialogue;
        _currentLineIndex = 0;
        _dialogueAnchor = anchor;

        // Stop any auto-advance coroutine
        if (_autoAdvanceCoroutine != null)
        {
            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }

        // Show first line
        ShowCurrentLine();
    }

    public void AdvanceDialogue()
    {
        // If text is still typing, complete it instead of advancing
        if (dialogueUI.IsTyping())
        {
            dialogueUI.CompleteTyping();
            return;
        }

        // Stop any auto-advance coroutine
        if (_autoAdvanceCoroutine != null)
        {
            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }

        // Move to next line
        _currentLineIndex++;

        // Check if dialogue is complete
        if (_currentLineIndex >= _currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        // Show next line
        ShowCurrentLine();
    }

    public void SelectChoice(int choiceIndex)
    {
        DialogueData.DialogueLine currentLine = _currentDialogue.lines[_currentLineIndex];

        if (currentLine.choices != null && choiceIndex < currentLine.choices.Length)
        {
            DialogueData.DialogueChoice choice = currentLine.choices[choiceIndex];

            // Invoke choice event
            choice.onChoiceSelected?.Invoke();

            // Move to next dialogue if specified
            if (choice.nextDialogue != null)
            {
                StartDialogue(_currentNPC, choice.nextDialogue, _dialogueAnchor);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void ShowCurrentLine()
    {
        DialogueData.DialogueLine line = _currentDialogue.lines[_currentLineIndex];

        // Update UI position to follow anchor
        if (_dialogueAnchor != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_dialogueAnchor.position);
            dialogueUI.transform.position = screenPos;
        }

        // Show dialogue
        dialogueUI.Show(line.speakerName, line.text, line.speakerPortrait);

        // Show choices if any
        dialogueUI.ShowChoices(line.choices);

        // Play voice clip if available
        if (line.voiceClip != null)
        {
            AudioSource.PlayClipAtPoint(line.voiceClip, Camera.main.transform.position);
        }

        // Auto-advance if enabled and no choices
        if (_currentDialogue.autoAdvance && (line.choices == null || line.choices.Length == 0))
        {
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine(line.displayTime));
        }
    }

    private IEnumerator AutoAdvanceCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Wait until typing is complete
        while (dialogueUI.IsTyping())
        {
            yield return null;
        }

        yield return new WaitForSeconds(autoAdvanceDelay);
        AdvanceDialogue();
    }

    private void EndDialogue()
    {
        dialogueUI.Hide();

        if (_currentNPC != null)
        {
            _currentNPC.EndDialogue();
            _currentNPC = null;
        }

        _currentDialogue = null;
    }
}