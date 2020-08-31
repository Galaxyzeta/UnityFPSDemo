using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour {
    public float hp;
    public float maxHp;

    public void Kill() {
        this.hp = 0;
    }

    public bool IsCritical() {
        return this.hp <= 0;
    }

    public void Heal(float healUnit) {
        hp += healUnit;
        hp = hp > maxHp? maxHp: hp;
    }

    public void Damage(float damagePoint) {
        hp -= damagePoint;
        hp = hp <= 0? 0: hp;
    }
}
