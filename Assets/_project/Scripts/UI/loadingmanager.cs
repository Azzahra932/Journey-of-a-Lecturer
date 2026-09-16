using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider loadingSlider;

    [Header("Settings")]
    [SerializeField] private string targetSceneName = "MainGameplay";
    [SerializeField] private float minimumLoadingTime = 1.5f; // Durasi minimum loading (detik)

    private void Start()
    {
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }

        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        // Memulai proses loading scene tujuan di background
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            // AsyncOperation.progress bernilai 0 sampai 0.9 saat loading
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Menghitung progress gabungan berdasarkan waktu minimum dan loading asli
            float timeProgress = Mathf.Clamp01(timer / minimumLoadingTime);
            float currentProgress = Mathf.Min(targetProgress, timeProgress);

            if (loadingSlider != null)
            {
                loadingSlider.value = currentProgress;
            }

            // Jika loading sistem selesai DAN waktu minimum sudah tercapai
            if (operation.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                loadingSlider.value = 1f;
                operation.allowSceneActivation = true; // Pindah ke scene gameplay
            }

            yield return null;
        }
    }
}