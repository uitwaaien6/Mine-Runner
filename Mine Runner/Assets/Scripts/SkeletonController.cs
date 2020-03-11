﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonController : MonoBehaviour
{
    Rigidbody rb;
    Animator animator;
    RockMovement rockMovement;
    int obstaclesLength;
    float speed = 4.8f;

    [SerializeField] Material flashMat;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rockMovement = FindObjectOfType<RockMovement>();
        animator = GetComponent<Animator>();
        obstaclesLength = GameObject.FindGameObjectsWithTag("Obstacle").Length;
    }

    void Update()
    {
        FreezeRotations();
        MovePosition();
        KeepTrackOfObjectsWithTag("Obstacle");
    }

    [Obsolete]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Obstacle" && collision.gameObject.tag != "Terrain")
        {
            animator.SetBool("isDigging", true);
            StartCoroutine(FlashObstacle(collision, flashMat, 0.06f));
            DestroyObstacleAfterAnimation(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Obstacle")
        {
            animator.SetBool("isDigging", false);
        }
    }

    [Obsolete]
    private void DestroyObstacleAfterAnimation(Collision collision)
    {
        float animDuration = rockMovement.DestroyObstacleAnimation(collision, 0.7f, 0.7f, 0.7f);
        Debug.Log(animDuration);
        float destroyDuration = animDuration - (animDuration / 1.20f);
        flashObstacleEdgeValue = destroyDuration;
        Destroy(collision.gameObject, destroyDuration); // destroy duration has to be greater than flashObstacle complition
    }

    float flashObstacleEdgeValue;
    public IEnumerator FlashObstacle(Collision collision, Material flashMat, float flashTime)
    {
        if (collision.gameObject.GetComponent<MeshRenderer>() != null)
        {
            int counter = 0;
            int switcher = 1;
            bool isFlashing = true;
            Material[] savedMaterials = collision.gameObject.GetComponent<MeshRenderer>().materials;
            while (isFlashing)
            {
                Material[] flashMaterialsToPlace = new Material[savedMaterials.Length];
                for (int i = 0; i < flashMaterialsToPlace.Length; i++)
                {
                    flashMaterialsToPlace[i] = flashMat;
                }
                switch (switcher)
                {
                    case 1:
                        if (collision.gameObject.tag != "Terrain")
                        {
                            collision.gameObject.GetComponent<MeshRenderer>().materials = savedMaterials;
                        }
                        break;
                    case -1:
                        if (collision.gameObject.tag != "Terrain")
                        {
                            collision.gameObject.GetComponent<MeshRenderer>().materials = flashMaterialsToPlace;
                        }
                        break;
                }
                counter++;
                switcher *= -1;
                if (counter > 9) { isFlashing = false; } /* this value has to be below destroyDuration otherwise it will try to change the non-existing meshRenderer */
                
                yield return new WaitForSeconds(flashTime);
            }
            Debug.Log((9 * flashTime) + " " + flashObstacleEdgeValue); // todo will make a system 
            StopAllCoroutines(); // warning, stops all the coroutines in script
        }
    }

    private void KeepTrackOfObjectsWithTag(string tagName)
    {
        int childrens = GameObject.FindGameObjectsWithTag(tagName).Length;
        if (childrens < obstaclesLength)
        {
            obstaclesLength = childrens;
            animator.SetBool("isDigging", false);
        }
    } 

    private void MovePosition()
    {
        rb.MovePosition(rb.position + Vector3.right * speed * Time.deltaTime);
        rb.AddForce(Vector3.forward * -270f);
    }

    private void FreezeRotations()
    {
        float xRotation = transform.rotation.x;
        xRotation = Mathf.Clamp(xRotation, xRotation - 1, xRotation + 2);
        transform.rotation = Quaternion.Euler(xRotation, 90f, 90f);
    }
}
