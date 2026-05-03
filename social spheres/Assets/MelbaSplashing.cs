using UnityEngine;

public class MelbaSplashing : MonoBehaviour
{
    [Header("Credits: TheCoder")]

    [Header("Partical Prefab")]
    public GameObject ParticalPrefab;
    [Header("How long partical lasts")]
    public float spawnDuration = 2.0f;
    [Header("Splash Sound Effect")]
    public AudioSource HitSound;

    private float spawnTimer = 0.0f;
    private bool canSplash = true;

    private void OnTriggerEnter(Collider other)
    {
        if (canSplash && other.gameObject.CompareTag("HandTag"))
        {
            //Play Partical at hand
            GameObject effectInstance = Instantiate(ParticalPrefab, other.transform.position, Quaternion.identity);
            spawnTimer = 0.0f;

            Destroy(effectInstance, spawnDuration);
            HitSound.Play();

            canSplash = false;
            Invoke("ResetSplash", 2.0f);
        }
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
    }

    private void ResetSplash()
    {
        canSplash = true;
    }
}
