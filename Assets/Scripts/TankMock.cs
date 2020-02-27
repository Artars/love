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

    [Header("Skins")]
    public Material[] skins;
    public string tankName = "";
    public int tankSkin = 0;
    public bool showName = true;
    public List<MeshRenderer> meshRendereres = new List<MeshRenderer>();
    public List<TMPro.TextMeshPro> tankTexts =  new List<TMPro.TextMeshPro>();

    public void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void ApplyPosition(Vector3 mainPosition, Quaternion mainRotation, Quaternion turretRotation, Quaternion cannonRotation)
    {
        mainTransform.position = mainPosition;
        mainTransform.rotation = mainRotation;
        turretTransform.rotation = turretRotation;
        cannonTransform.localRotation = cannonRotation;
    }

    public void SetTankNameAndSkin(string name, bool showName, int skin)
    {
        tankName = name;
        tankSkin = skin;
        this.showName = showName;

        UpdateSkin(tankSkin);
        UpdateName(tankName);
    }


    protected void UpdateSkin(int newSkin)
    {
        //Update skin material
        foreach (var mesh in meshRendereres)
        {
            if(mesh != null)
            {
                Material[] newMaterials = new Material[mesh.materials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = skins[newSkin];
                }
                mesh.materials = newMaterials;
            }
        }
    }

    protected void UpdateName(string newName)
    {
        //Update text
        foreach (var text in tankTexts)
        {
            if(text != null)
            {
                if(showName)
                    text.text = newName;
                else
                    text.text = "";
            }
        }
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
