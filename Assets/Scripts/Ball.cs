using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ball : MonoBehaviour
{
    public AudioClip[] hitSounds;
    public Light ballLight;

    public float lightIntensity = 2.5f;
    public float lightRange = 1.2f;

    private Rigidbody2D body;
    private AudioSource audioSource;
    private CircleCollider2D col;

    private FieldSide respawnSide = FieldSide.None;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<CircleCollider2D>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (body.velocity.magnitude > 1)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(hitSounds[Random.Range(0,hitSounds.Length)], body.velocity.magnitude/10f);
            if (other.gameObject.layer == 8)
            {
                StopCoroutine(nameof(PlayLightEffect));
                StartCoroutine(PlayLightEffect(body.velocity.magnitude / 20f + 1f));
            }
        }
    }

    IEnumerator PlayLightEffect(float intensity)
    {
        float timer = 0;
        while (timer < 0.25f)
        {
            float newFactor = Mathf.Lerp(intensity, 1, Easing.Quadratic.Out(timer / 0.25f));

            ballLight.intensity = lightIntensity * newFactor;
            ballLight.range = lightRange * newFactor;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        ballLight.intensity = lightIntensity;
        ballLight.range = lightRange;
    }

    public void Respawn(FieldSide side)
    {
        respawnSide = side;
        Invoke(nameof(PlaceBall),3.0f);
    }

    private void PlaceBall()
    {
        body.velocity = Vector2.zero;
        transform.position = respawnSide == FieldSide.Left ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
        if(Physics2D.OverlapCircle(transform.position,col.radius,1 << 8))
            body.isKinematic = true;
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (respawnSide != FieldSide.None)
        {
            respawnSide = FieldSide.None;
            body.isKinematic = false;
        }
    }
}
