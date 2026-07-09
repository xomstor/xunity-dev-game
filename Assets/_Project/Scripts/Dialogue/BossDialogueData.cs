using UnityEngine;

[CreateAssetMenu(fileName = "BossDialogueData", menuName = "Dialogue/Boss Dialogue Data")]
public class BossDialogueData : ScriptableObject
{
    [Header("NPC")]
    public string npcName = "Хранитель Раскола";
    public Sprite npcFace;
    public Sprite[] npcFaceFrames;

    [Header("Dialogue")]
    [TextArea(3, 15)]
    public string[] dialogueLines = new string[]
    {
        "Ты подошёл. Я ждал."
    };

    [Header("Boss Scene")]
    public string bossSceneName = "BossGameScene";
    public string bossSpawnPointName = "Boss1SpawnPoint";

    [Header("Settings")]
    public bool playOnlyOnce = true;
}
