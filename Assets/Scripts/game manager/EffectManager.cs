using System;
using System.Collections;
using UnityEngine;

public enum EffectType
{
    // Buffs
    SpeedBoost,        // Ngebut +40%
    IronSphincter,     // Imun rintangan (kebal tabrakan)
    ExtraTime,         // Instan tambah waktu kebelet

    // Debuffs
    StomachCramp,      // Kecepatan drop -35%
    InvertControls,    // Kiri & kanan terbalik
    TremorPanic        // Kamera getar heboh (distraksi)
}

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("Lucky Number Durations")]
    [Tooltip("Daftar angka hoki durasi efek yang bisa keluar")]
    public int[] luckyDurations = new int[] { 3, 5, 7 };

    // Event untuk mengabari UI ada efek baru (Nama Efek, Durasi, Apakah Positif)
    public event Action<EffectType, int, bool> OnEffectStarted;
    public event Action OnEffectEnded;

    private Coroutine activeEffectCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Dipanggil saat player menabrak kotak gacha di lintasan
    /// </summary>
    public void RollLuckyEffect()
    {
        // 1. ROLL JENIS EFEK (50% Buff, 50% Debuff)
        bool isPositive = UnityEngine.Random.value > 0.5f;
        EffectType chosenEffect;

        if (isPositive)
        {
            // Acak salah satu dari 3 Buff
            EffectType[] buffs = { EffectType.SpeedBoost, EffectType.IronSphincter, EffectType.ExtraTime };
            chosenEffect = buffs[UnityEngine.Random.Range(0, buffs.Length)];
        }
        else
        {
            // Acak salah satu dari 3 Debuff
            EffectType[] debuffs = { EffectType.StomachCramp, EffectType.InvertControls, EffectType.TremorPanic };
            chosenEffect = debuffs[UnityEngine.Random.Range(0, debuffs.Length)];
        }

        // 2. ROLL LUCKY NUMBER DURASI
        int duration = luckyDurations[UnityEngine.Random.Range(0, luckyDurations.Length)];

        // Terapkan Efek
        ApplyEffect(chosenEffect, duration, isPositive);
    }

    private void ApplyEffect(EffectType effect, int duration, bool isPositive)
    {
        // Hentikan efek sebelumnya jika ada yang masih jalan
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
            ResetPlayerState();
        }

        string effectName = "";

        switch (effect)
        {
            case EffectType.SpeedBoost:
                effectName = "NGEBUT KEBELET (+Speed)";
                PlayerController.Instance.SetSpeedMultiplier(1.4f);
                break;

            case EffectType.IronSphincter:
                effectName = "TAHAN BATIN (Invincible)";
                PlayerController.Instance.isInvincible = true;
                break;

            case EffectType.ExtraTime:
                effectName = "NAPAS LEGA (+5s Timer)";
                GameManager.Instance.AddUrgencyTime(5f);
                break;

            case EffectType.StomachCramp:
                effectName = "KRAM PERUT (-Speed)";
                PlayerController.Instance.SetSpeedMultiplier(0.65f);
                break;

            case EffectType.InvertControls:
                effectName = "PANIK! (Kontrol Terbalik)";
                PlayerController.Instance.invertControls = true;
                break;

            case EffectType.TremorPanic:
                effectName = "KERINGAT DINGIN (Tremor & Panic)";
                ProceduralCamera.Instance?.TriggerShake(duration, 0.4f);
                UrgencyVignette.Instance?.SetPanicState(true);
                break;
        }

        Debug.Log($"<color={(isPositive ? "green" : "red")}>[GACHA ROLL]</color> Dapat: <b>{effectName}</b> selama <b>{duration} Detik!</b>");
        OnEffectStarted?.Invoke(effect, duration, isPositive);

        activeEffectCoroutine = StartCoroutine(EffectTimerRoutine(duration));
    }

    private IEnumerator EffectTimerRoutine(int duration)
    {
        yield return new WaitForSeconds(duration);
        ResetPlayerState();
        OnEffectEnded?.Invoke();
        activeEffectCoroutine = null;
        Debug.Log("<color=gray>[Efek Selesai]</color> Kondisi player kembali normal.");
    }

    private void ResetPlayerState()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetSpeedMultiplier(1.0f);
            PlayerController.Instance.invertControls = false;
            PlayerController.Instance.isInvincible = false;
        }

        UrgencyVignette.Instance?.SetPanicState(false);
    }
}