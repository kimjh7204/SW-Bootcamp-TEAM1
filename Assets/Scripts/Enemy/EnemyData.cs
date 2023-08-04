using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Enemy Data", menuName = "Scriptable Object/Enemy Data", order = int.MaxValue)]
public class EnemyData : ScriptableObject
{
    [SerializeField] private EnemyType enemyType;
    public EnemyType EnemyType { get {  return enemyType; } }

    [SerializeField] private int enemyHp;
    public int EnemyHp { get {  return enemyHp; } }

    [SerializeField] private float enemyMoveSpeed;
    public float EnemyMoveSpeed { get { return enemyMoveSpeed; } }

    [SerializeField] private float enemyAttackRange;
    public float EnemyAttackRange { get {  return enemyAttackRange; } }

    [SerializeField] private float attackDuration;
    public float AttackDuration { get {  return attackDuration; } }

    [SerializeField] private float getHitDuration;
    public float GetHitDuration { get { return getHitDuration; } }
}
