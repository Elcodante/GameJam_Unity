using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SpeedLinesVFX : MonoBehaviour
{
    private ParticleSystem speedParticles;
    private ParticleSystem.EmissionModule emissionModule;

    private void Start()
    {
        CreateProceduralParticleSystem();
    }

    private void CreateProceduralParticleSystem()
    {
        // Buat GameObject partikel sebagai anak dari kamera
        GameObject particleObj = new GameObject("ProceduralSpeedLines");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = new Vector3(0, 0, 3f);
        particleObj.transform.localRotation = Quaternion.identity;

        speedParticles = particleObj.AddComponent<ParticleSystem>();
        
        // Setup Main Module
        var main = speedParticles.main;
        main.startLifetime = 0.35f;
        main.startSpeed = -25f; // Meluncur ke arah kamera
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 1f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Setup Emission
        emissionModule = speedParticles.emission;
        emissionModule.rateOverTime = 0f;

        // Setup Shape (Silinder di sekeliling sudut pandang kamera)
        var shape = speedParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 3.5f;

        // Setup Renderer (Bentuk garis panjang / stretched billboard)
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 4f;
        renderer.velocityScale = 0.1f;

        // Gunakan default particle material bawaan URP/Unity
        renderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void Update()
    {
        if (speedParticles == null || PlayerController.Instance == null) return;

        // Kecepatan normal = emisi angin sedikit. Saat Speed Boost = angin kencang!
        float speedRatio = PlayerController.Instance.currentRunSpeed / PlayerController.Instance.baseRunSpeed;

        if (speedRatio > 1.1f)
        {
            // Saat Speed Boost aktif
            emissionModule.rateOverTime = 80f;
        }
        else
        {
            // Saat lari biasa
            emissionModule.rateOverTime = 15f;
        }
    }
}