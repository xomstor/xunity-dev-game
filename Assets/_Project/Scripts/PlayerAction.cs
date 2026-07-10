using UnityEngine;

public static class PlayerAction
{
    public static float interactRadius = 2f;

    public static bool HasDialogueNearby()
    {
        return HasInteractableNearby(t => t is DialogueTrigger || t is BossZoneNPC || t is QuestNPC);
    }

    public static bool HasItemNearby()
    {
        return HasInteractableNearby(t => t is PickupItem);
    }

    public static bool HasInteractableNearby()
    {
        return HasInteractableNearby(_ => true);
    }

    static bool HasInteractableNearby(System.Func<object, bool> filter)
    {
        PlayerController player = null;
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.GetComponent<PlayerController>();

        if (player == null)
            return false;

        Vector2 origin = player.transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, interactRadius);
        foreach (Collider2D c in colliders)
        {
            if (c == null) continue;
            object target = FindInteractable(c);
            if (target != null && filter(target))
                return true;
        }
        return false;
    }

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
        {
            Debug.Log("[PlayerAction] TryInteract: player not found");
            return;
        }

        Vector2 origin = player.transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, interactRadius);
        Debug.Log($"[PlayerAction] TryInteract: found {colliders.Length} colliders within radius {interactRadius}");

        object nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D c in colliders)
        {
            if (c == null) continue;

            object target = FindInteractable(c);
            if (target == null) continue;

            Transform t = null;
            if (target is PickupItem pi) t = pi.transform;
            else if (target is DialogueTrigger dt) t = dt.transform;
            else if (target is BossZoneNPC bz) t = bz.transform;
            else if (target is QuestNPC qn) t = qn.transform;

            if (t == null) continue;

            float d = ((Vector2)t.position - origin).sqrMagnitude;
            Debug.Log($"[PlayerAction]   {target.GetType().Name} on {t.name} at distance {Mathf.Sqrt(d):F2}");
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = target;
            }
        }

        if (nearest is PickupItem nearestPickup)
        {
            Debug.Log($"[PlayerAction] Picking up item {nearestPickup.name}");
            nearestPickup.TryPickUp();
            return;
        }

        if (nearest is DialogueTrigger nearestDialogue)
        {
            Debug.Log($"[PlayerAction] Starting dialogue with {nearestDialogue.name}");
            nearestDialogue.StartDialogue();
            return;
        }

        if (nearest is BossZoneNPC nearestBoss)
        {
            Debug.Log($"[PlayerAction] Starting boss-zone dialogue with {nearestBoss.name}");
            nearestBoss.StartDialogue();
            return;
        }

        if (nearest is QuestNPC nearestQuest)
        {
            Debug.Log($"[PlayerAction] Starting quest dialogue with {nearestQuest.name}");
            nearestQuest.TryStartQuestDialogue();
            return;
        }

        Debug.Log("[PlayerAction] No interactable found nearby");
    }

    static object FindInteractable(Collider2D c)
    {
        PickupItem pi = FindPickupItem(c);
        if (pi != null) return pi;

        DialogueTrigger dt = FindDialogueTrigger(c);
        if (dt != null) return dt;

        BossZoneNPC bz = FindBossZoneNPC(c);
        if (bz != null) return bz;

        QuestNPC qn = FindQuestNPC(c);
        if (qn != null) return qn;

        return null;
    }

    static PickupItem FindPickupItem(Collider2D c)
    {
        PickupItem pi = c.GetComponent<PickupItem>();
        if (pi == null && c.attachedRigidbody != null)
            pi = c.attachedRigidbody.GetComponent<PickupItem>();
        if (pi == null) pi = c.GetComponentInParent<PickupItem>();
        if (pi == null) pi = c.GetComponentInChildren<PickupItem>();
        return pi;
    }

    static QuestNPC FindQuestNPC(Collider2D c)
    {
        QuestNPC qn = c.GetComponent<QuestNPC>();
        if (qn == null && c.attachedRigidbody != null)
            qn = c.attachedRigidbody.GetComponent<QuestNPC>();
        if (qn == null) qn = c.GetComponentInParent<QuestNPC>();
        if (qn == null) qn = c.GetComponentInChildren<QuestNPC>();
        return qn;
    }

    static DialogueTrigger FindDialogueTrigger(Collider2D c)
    {
        DialogueTrigger dt = c.GetComponent<DialogueTrigger>();
        if (dt == null && c.attachedRigidbody != null)
            dt = c.attachedRigidbody.GetComponent<DialogueTrigger>();
        if (dt == null) dt = c.GetComponentInParent<DialogueTrigger>();
        if (dt == null) dt = c.GetComponentInChildren<DialogueTrigger>();
        return dt;
    }

    static BossZoneNPC FindBossZoneNPC(Collider2D c)
    {
        BossZoneNPC bz = c.GetComponent<BossZoneNPC>();
        if (bz == null && c.attachedRigidbody != null)
            bz = c.attachedRigidbody.GetComponent<BossZoneNPC>();
        if (bz == null) bz = c.GetComponentInParent<BossZoneNPC>();
        if (bz == null) bz = c.GetComponentInChildren<BossZoneNPC>();
        return bz;
    }
}
