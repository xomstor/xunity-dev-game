using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Slot Buttons (0=AutoSave, 1-3=Manual)")]
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotLabels;

    [Header("Mode")]
    public bool isSaveMode = true;

    [Header("Panel")]
    public GameObject panel;

    void OnEnable()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (SaveManager.Instance == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            if (slotButtons[i] == null) continue;

            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slot));

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                string label = SaveManager.Instance.GetSlotLabel(slot);
                if (SaveManager.Instance.HasSave(slot))
                {
                    string path = SaveManager.Instance.GetSlotPath(slot);
                    System.IO.FileInfo fi = new System.IO.FileInfo(path);
                    label += $"\n<size=70%>{fi.LastWriteTime:dd.MM.yy HH:mm}</size>";

                    if (slot == SaveManager.AutoSaveSlot)
                        slotButtons[i].interactable = !isSaveMode;
                }
                else
                {
                    label += "\n<size=70%>Пусто</size>";
                    if (slot == SaveManager.AutoSaveSlot)
                        slotButtons[i].interactable = false;
                }
                slotLabels[i].text = label;
            }

            if (slot == SaveManager.AutoSaveSlot && isSaveMode)
                slotButtons[i].interactable = false;
        }
    }

    void OnSlotClicked(int slot)
    {
        if (isSaveMode)
            SaveManager.Instance?.SaveGame(slot);
        else
            SaveManager.Instance?.LoadGame(slot);

        RefreshSlots();
        panel?.SetActive(false);
    }

    public void Open(bool saveMode)
    {
        isSaveMode = saveMode;
        panel?.SetActive(true);
        RefreshSlots();
    }

    public void Close()
    {
        panel?.SetActive(false);
    }
}
