using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public ScoreManager scoreManager;
    public ParticleSystem goalEffect;
    public AudioSource goalSound;
    public bool isRight;

    private void OnTriggerEnter2D(Collider2D other)
    {
        scoreManager.AddPoint(!isRight);
        goalEffect.Play();
        goalSound.pitch = Random.Range(0.9f, 1.1f);
        goalSound.Play();
    }


}
