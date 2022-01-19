using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public float moveForce = 20;
    public Vector3 position;
    public bool debug;
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dir = position - transform.position;

        if (debug)
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0;

            dir = pos - transform.position;
        }

        body.velocity = dir * moveForce;
    }
}
