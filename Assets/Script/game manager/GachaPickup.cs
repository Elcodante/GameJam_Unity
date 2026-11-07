using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(TreadmillMover))]
public class GachaPickup : MonoBehaviour
{
    [Header("Visual Rotation")]
    public float rotateSpeed = 100f;

    private void Start()
    {
        // Pastikan collider diset sebagai Trigger agar tidak menghalangi lari player
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        // Efek visual berputar di tempat
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            AudioManager.Instance?.PlayGacha();
            // Picu roll gacha
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RollLuckyEffect();
            }

            // Hancurkan objek pickup setelah diambil
            Destroy(gameObject);
        }
    }
}