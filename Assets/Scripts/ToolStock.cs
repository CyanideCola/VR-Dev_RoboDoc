using UnityEngine;

public class ToolStock : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string label = "Mallet";
        public ToolType tool = ToolType.Hammer;
        public GameObject prefab;
        public int price = 50;
        public bool starter = true;     // starter tools must be owned before the energy refill unlocks
        [HideInInspector] public bool owned;
        [HideInInspector] public GameObject live;
    }

    public Entry[] entries;
    public Transform dropPoint;
    public float floorY = -0.5f, lostDistance = 3f;
    public GameObject partPrefab;       // spare part, taken from GameProgress.parts

    public bool StartersOwned { get { foreach (var e in entries) if (e.starter && !e.owned) return false; return true; } }

    public void BuyByIndex(int i)
    {
        var e = entries[i];
        if (e.owned) { Notifications.Push("Already owned"); return; }
        if (!GameProgress.Instance.TrySpend(e.price)) { Notifications.Push("Need " + e.price + " gold"); return; }
        e.owned = true;
        Spawn(e);
        TutorialEvents.Raise("Bought:" + e.tool);
    }

    // Challenge: the starter tools are already bought, so the clock measures repairs, not shopping
    public void GrantStarters()
    {
        foreach (var e in entries)
            if (e.starter && !e.owned) { e.owned = true; Spawn(e); }
    }

    public void RespawnByIndex(int i)
    {
        var e = entries[i];
        if (!e.owned) return;
        if (e.live)
        {
            Vector3 p = e.live.transform.position;
            bool lost = p.y < floorY || Vector3.Distance(p, dropPoint.position) > lostDistance;
            if (!lost) { Notifications.Push(e.label + " is still around"); return; }
            Destroy(e.live);
        }
        Spawn(e);
    }

    public void TakePart()
    {
        var gp = GameProgress.Instance;
        if (!partPrefab || !gp) return;
        if (gp.parts <= 0) { Notifications.Push("No parts. Dismantle a robot to get some"); return; }
        gp.parts--;
        Instantiate(partPrefab, dropPoint.position, dropPoint.rotation);
    }

    void Spawn(Entry e) { e.live = Instantiate(e.prefab, dropPoint.position, dropPoint.rotation); }
}
