using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankMock : MonoBehaviour
{
    public Transform mainTransform;
    public Transform turretTransform;
    public Transform cannonTransform;

    public ParticleSystem explosionParticles;

    [Range(0,1)]
    public float turretExplosionChance = 0.1f;
    public float turretExplosionVelocity = 2;

    public float lifetime = 2;

    public void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void ApplyPosition(Vector3 mainPosition, Quaternion mainRotation, Quaternion turretRotation, Quaternion cannonRotation)
    {
        mainTransform.position = mainPosition;
        mainTransform.rotation = mainRotation;
        turretTransform.localRotation = turretRotation;
        cannonTransform.localRotation = cannonRotation;
    }

    public void Explode()
    {
        if(explosionParticles != null)
        {
            explosionParticles.Play();
        }

        if(Random.Range(0f,1f) < turretExplosionChance)
        {
            Debug.Log("Exploded");
            // StartCoroutine(TurretFlyingAnimation());
            MakeTurretFly();
        }
    }

    protected void MakeTurretFly()
    {
        Destroy(turretTransform.gameObject, lifetime/2);
        turretTransform.SetParent(null);
        Rigidbody rigidbody = turretTransform.gameObject.AddComponent<Rigidbody>();
        rigidbody.AddForce(Vector3.up * turretExplosionVelocity, ForceMode.Impulse);
    }

    public IEnumerator TurretFlyingAnimation()
    {
        float counter = 0;
        float delta;

        while(counter < lifetime)
        {
            delta = Time.deltaTime;

            counter += delta;

            turretTransform.Translate(turretTransform.up * turretExplosionVelocity * delta);
            yield return null;
        }
    }

}
