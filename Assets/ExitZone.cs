using UnityEngine;

public class ExitZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponentInParent<Character>();
        if (character != null && character.manager != null)
            character.manager.PlayerExited(character);
    }
}
