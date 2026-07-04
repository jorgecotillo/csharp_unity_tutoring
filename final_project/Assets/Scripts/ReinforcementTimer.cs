using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ReinforcementEvent : UnityEvent<int> { }

/// <summary>
/// Simple, reusable reinforcement timer. Start the countdown via `StartTimer()`.
/// When the countdown reaches zero this component will invoke `OnReinforcementsArrive` with
/// the number of waves that should spawn (or 1 if you prefer to handle per-wave spawns externally).
/// It also exposes a per-tick callback `OnTick` (float remainingSeconds) so UI can update.
/// </summary>
public class ReinforcementTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Base delay from trigger to first reinforcement wave (seconds)")]
    public float initialDelay = 60f;

    [Tooltip("Time before arrival to start warnings (seconds)")]
    public float warningTime = 30f;

    [Tooltip("How many waves of reinforcements will arrive once timer completes")]
    public int waveCount = 3;

    [Tooltip("Spacing between subsequent waves (seconds)")]
    public float spawnInterval = 10f;

    [Tooltip("How many reinforcements per wave (informational only)")]
    public int reinforcementsPerWave = 6;

    [Header("Events")]
    // Invoked when the overall countdown reaches zero. Parameter = total waves to spawn.
    public ReinforcementEvent OnReinforcementsArrive;

    // Invoked every second while the timer is running. Parameter = remaining seconds.
    public UnityEventFloat OnTick;

    // Invoked when warnings should start (remaining <= warningTime)
    public UnityEvent OnWarningsStart;

    // Invoked if the timer is cancelled before completion
    public UnityEvent OnCancelled;

    private Coroutine _running;
    private float _remaining;
    private bool _isRunning = false;

    public float GetRemaining() => _isRunning ? _remaining : 0f;
    public bool IsRunning() => _isRunning;

    [ContextMenu("Start Timer")]
    public void StartTimer()
    {
        StartTimer(initialDelay);
    }

    public void StartTimer(float delay)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(RunTimer(delay));
    }

    public void CancelTimer()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _isRunning = false;
        OnCancelled?.Invoke();
    }

    private IEnumerator RunTimer(float delay)
    {
        _isRunning = true;
        _remaining = delay;

        // If the delay is <= 0 we immediately fire
        if (_remaining <= 0f)
        {
            Finish();
            yield break;
        }

        bool warningsFired = false;

        // Tick every 0.2s for smooth UI but only invoke OnTick once per second
        float tickAccumulator = 0f;
        while (_remaining > 0f)
        {
            _remaining -= 0.2f;
            tickAccumulator += 0.2f;

            if (!warningsFired && _remaining <= warningTime)
            {
                warningsFired = true;
                OnWarningsStart?.Invoke();
            }

            if (tickAccumulator >= 1f)
            {
                tickAccumulator = 0f;
                OnTick?.Invoke(Mathf.Ceil(_remaining));
            }

            yield return new WaitForSeconds(0.2f);
        }

        Finish();
    }

    private void Finish()
    {
        _isRunning = false;
        _running = null;

        // Fire the main event with the configured wave count
        OnReinforcementsArrive?.Invoke(waveCount);

        // Optionally spawn subsequent waves after intervals. We only signal the first event by default.
        // Spawner systems can either listen to OnReinforcementsArrive and spawn all waves, or
        // listen and schedule further waves using the `spawnInterval` value and `reinforcementsPerWave`.
    }
}

[Serializable]
public class UnityEventFloat : UnityEvent<float> { }
