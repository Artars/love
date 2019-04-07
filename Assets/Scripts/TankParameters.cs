using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Data", menuName = "Game Parameters/Tank parameters", order = 1)]
[System.Serializable]
public class TankParameters : ScriptableObject
{
    [Header("Driver")]
    
    public float forwardSpeed = 10;
    public float backwardSpeed = 5;
    public float turnSpeed = 10;


    [Header("Gunner")]
    public float turnCannonSpeed = 20;
    public float nivelCannonSpeed = 20;
    public float minCannonNivel = -30;
    public float maxCannonNivel = 30;
    public float shootCooldown = 1;
    public float bulletSpeed = 30;
    public float bulletDamage = 20;

    [Header("Health")]
    public float maxHeath = 100;



}
