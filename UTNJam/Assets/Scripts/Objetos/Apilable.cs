using NUnit.Framework;
using UnityEngine;

public class Apilable : MonoBehaviour
{
    [SerializeField] float timerMax;
    float timer;
    [SerializeField] bool isGrounded = false;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isGrounded)
        {
            timer += Time.deltaTime;
            if (timer >= timerMax)
            {
                isGrounded = false;
                rb.isKinematic = true;
            }
        }
    }

    public bool GetGrounded()
    {
        return rb.isKinematic;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isGrounded)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                Debug.Log("hit ground");
            }

            if (collision.gameObject.CompareTag("Apilable"))
            {
                Debug.Log("cai sobre un objeto" + collision.gameObject.GetComponent<Apilable>().GetGrounded());
                if (collision.gameObject.GetComponent<Apilable>().GetGrounded())
                {
                    isGrounded = true;
                    Debug.Log("hit object");
                }
            }
        }
    
    }
}
