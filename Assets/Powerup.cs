using UnityEngine;

public class Powerup : MonoBehaviour
{
    public enum PowerupType { Phase, TrueRadar }

    public PowerupType Type = PowerupType.Phase;
    public bool IsCollected { get; private set; }

    private void Awake() => EnsureTrigger();
    private void Reset() => EnsureTrigger();

    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponentInParent<Character>();
        Collect(character);
    }

    public void Collect(Character character)
    {
        if (character == null || IsCollected) return;
        IsCollected = true;
        character.CollectPowerup(Type);
        character.manager?.ShowPowerupCollected(character, Type);
        Destroy(gameObject);
    }

    private void EnsureTrigger()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
    }
}
