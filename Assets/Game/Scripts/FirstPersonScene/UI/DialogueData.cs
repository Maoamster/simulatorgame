using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(3, 10)]
        public string text;
        public AudioClip voiceClip;
        public float displayTime = 3f; // How long to display if auto-advancing
        public Sprite speakerPortrait;
        public string speakerName;
        public DialogueChoice[] choices;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public DialogueData nextDialogue;
        public UnityEngine.Events.UnityEvent onChoiceSelected;
    }

    public DialogueLine[] lines;
    public bool autoAdvance = false;
}