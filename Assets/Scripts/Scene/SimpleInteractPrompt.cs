using TMPro;
using UnityEngine;

public class SimpleInteractPrompt : MonoBehaviour
{
    public Transform player;
    public TMP_Text interactionText;
    public TMP_Text storyText;
    public string promptMessage = "Nhấn E để tương tác";
    public string storyMessage = "";
    public float storyDuration = 6f;
    public bool triggerOnce = true;

    private bool playerInRange;
    private bool hasTriggered;
    private float storyHideTime;

    private void Start()
    {
        FindReferencesIfNeeded();
        HidePrompt();
    }

    private void Update()
    {
        if (storyText != null && storyText.gameObject.activeSelf && storyHideTime > 0f && Time.time >= storyHideTime)
            storyText.gameObject.SetActive(false);

        if (!playerInRange || (triggerOnce && hasTriggered))
            return;

        ShowPrompt();

        if (Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = true;
        FindReferencesIfNeeded();

        if (!triggerOnce || !hasTriggered)
            ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        HidePrompt();
    }

    private void Interact()
    {
        hasTriggered = true;
        HidePrompt();

        if (storyText == null)
            return;

        storyText.text = storyMessage;
        storyText.gameObject.SetActive(true);
        storyHideTime = Time.time + storyDuration;
    }

    private void ShowPrompt()
    {
        if (interactionText == null)
        {
            FindReferencesIfNeeded();
            if (interactionText == null)
                return;
        }

        interactionText.text = promptMessage;
        interactionText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (interactionText == null)
            interactionText = FindTextInScene("InteractionText");

        if (storyText == null)
            storyText = FindTextInScene("StoryText");
    }

    private TMP_Text FindTextInScene(string objectName)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName && text.gameObject.scene.IsValid())
                return text;
        }

        return null;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }
}
