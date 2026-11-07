using UnityEngine;

[RequireComponent(typeof(TreadmillMover))]
public class ToiletGoal : MonoBehaviour
{
    private bool hasEntered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasEntered) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            hasEntered = true;
            Debug.Log("<color=cyan>[TOILET]</color> Berhasil masuk ke dalam toilet!");

            // Trigger Kemenangan di GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerWin();
            }
        }
    }
}