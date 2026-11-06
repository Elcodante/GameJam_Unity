using UnityEngine;

public class TreadmillMover : MonoBehaviour
{
    [Tooltip("Batas posisi Z di belakang player sebelum objek ini dihancurkan/direset")]
    public float destroyZPosition = -15f;

    private void Update()
    {
        float speed = 0f;
        if (PlayerController.Instance != null)
        {
            speed = PlayerController.Instance.currentRunSpeed;
        }

        // Gerakkan objek ke arah sumbu -Z (ke belakang)
        transform.position += Vector3.back * speed * Time.deltaTime;

        // Hancurkan atau recycle objek jika sudah melewati player
        if (transform.position.z <= destroyZPosition)
        {
            Destroy(gameObject);
        }
    }
}