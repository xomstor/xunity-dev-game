using UnityEngine;

public static class PlayerAction
{
    public static float interactRadius = 2f;

    public static void TryInteract()
    {
        if (DialogueSystem.IsDialogueActive && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnContinuePressed();
            return;
        }

        PlayerController player = null;
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.GetComponent<PlayerController>();

        if (player == null)
            return;

        Vector2 origin = player.transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, interactRadius);

        DialogueTrigger nearestDialogue = null;
        BossZoneNPC nearestBoss = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D c in colliders)
        {
            if (c == null) continue;

            DialogueTrigger dt = c.GetComponent<DialogueTrigger>();
            if (dt != null)
            {
                float d = ((Vector2)dt.transform.position - origin).sqrMagnitude;
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestDialogue = dt;
                }
            }

            BossZoneNPC bz = c.GetComponent<BossZoneNPC>();
            if (bz != null)
            {
                float d = ((Vector2)bz.transform.position - origin).sqrMagnitude;
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestBoss = bz;
                }
            }
        }

        if (nearestDialogue != null)
        {
            nearestDialogue.StartDialogue();
            return;
        }

        if (nearestBoss != null)
        {
            nearestBoss.StartDialogue();
        }
    }
}
