using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour {
    public float hp;
    public float maxHp;
    public GameObject owner {get; set;}

    public void Kill() {
        this.hp = 0;
    }

    public bool IsCritical() {
        return this.hp <= 0;
    }

    public void Heal(float healUnit) {
        Heal(healUnit, null);
    }

    public void Heal(float healUnit, GameObject healSource) {
        hp += healUnit;
        hp = hp > maxHp? maxHp: hp;
        Debug.Log(healSource);
    }

    public void Damage(float damagePoint) {
        Damage(damagePoint, null);
    }

    public void Damage(float damagePoint, GameObject damageSource) {
        hp -= damagePoint;
        hp = hp <= 0? 0: hp;
        Debug.Log("Inflictor "+damageSource+" deals "+damagePoint+" to "+owner);
    }

    void Start() {
        owner = gameObject;
    }

}
