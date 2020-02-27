using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Cannon : NetworkBehaviour
{
    [Header("Targeting")]
    protected bool isFollowing = false;
    protected Transform transformOriginal;
    protected Transform transformToFollow;
    [SyncVar(hook = nameof(SetTankReference))]
    [HideInInspector]
    public NetworkIdentity tankIdentity;
    [HideInInspector]
    public Tank tankReference;

    [Header("Original references")]
    public ParticleSystem shootParticles;
    public AudioSource firingSoundSource;
    public Transform rotationPivot;
    public Transform nivelTransform;
    public Transform bulletSpawnPosition;
    public Transform cameraPositionGunner;

    [Header("Skin")]
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    public List<TMPro.TextMeshPro> texts = new List<TMPro.TextMeshPro>();

    public void Start()
    {
        SetTankReference(null,tankIdentity);
    }

    public void SetTankReference(NetworkIdentity oldNetworkID ,NetworkIdentity newNetworkID)
    {
        tankIdentity = newNetworkID;
        if(tankIdentity == null)
        {
            Debug.LogWarning("Missing tank reference");
            return;
        }

        //Set tank references
        tankReference = tankIdentity.GetComponent<Tank>();
        tankReference.cannonReference = this;
        tankReference.shootParticles = shootParticles;
        tankReference.firingSoundSource = firingSoundSource;
        tankReference.rotationPivot = rotationPivot;
        tankReference.nivelTransform = nivelTransform;
        tankReference.bulletSpawnPosition = bulletSpawnPosition;
        tankReference.cameraPositionGunner = cameraPositionGunner;

        //Set follow references
        transformOriginal = transform;
        transformToFollow = tankReference.cannonAttachmentPoint;
        isFollowing = true;

        //Set mesh renderers references
        for (int i = 0; i < meshRenderers.Count; i++)
        {
            if(!tankReference.meshRendereres.Contains(meshRenderers[i]))
                tankReference.meshRendereres.Add(meshRenderers[i]);
        }

        for (int i = 0; i < texts.Count; i++)
            if(!tankReference.tankTexts.Contains(texts[i]))
                tankReference.tankTexts.Add(texts[i]);
        
        tankReference.ForceSkinAndNameUpdate();
    }

    public void Update()
    {
        if(isFollowing)
        {
            transformOriginal.position = transformToFollow.position;
            transformOriginal.up = transformToFollow.up;
        }
    }

    public void ForceUpdate()
    {
        if(isFollowing)
        {
            Update();
        }
    }

}
