using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponManager : MonoBehaviour {

	public Player player {get; private set;}

	public GameObject[] weaponBag;		// [GameObject] means WeaponPrefab here, [BaseWeapon] must be attached to it.
	public int maxWeapons;
	private int currentLength = 0;

	private int weaponCurrent = -1;

	public bool AddWeapon(GameObject weaponPrefab) {
		if(currentLength >= maxWeapons) {
			return false;
		} else {
			weaponBag[currentLength++] = weaponPrefab;
			return true;
		}
	}

	public bool IsEmpty() {
		return currentLength == 0;
	}

	public bool RemoveWeapon(int index) {
		if(index >= 0 && index < currentLength) {
			for(int i=index; i<currentLength-1; i++) {
				weaponBag[i] = weaponBag[i+1];
			}
			weaponBag[--currentLength] = null;
			return true;
		}
		return false;
	}

	// Set player's weapon prefab and weapon data.
	public bool EquipWeapon(int index) {
		if(index >= 0 && index < currentLength) {
			GameObject wepaonPrefab = weaponBag[index];
			BaseWeapon baseWeapon = wepaonPrefab.GetComponent<BaseWeapon>();
			
			player.weaponPrefab = wepaonPrefab;
			player.weaponData = baseWeapon;
			
			baseWeapon.SetOwner(player);
			baseWeapon.ResizeCrossHair();
			// Init weapon's place.
			baseWeapon.transform.position = player.GetComponent<PlayerMotor>().globalDefaultPoint.position;
			// Handle deriviation relations in order to make animation system works properly.
			baseWeapon.transform.SetParent(player.anchor.transform);
			// Attach animator on baseweapon to the anchor point.
			// player.ChangeAnimatorOnAnchor(baseWeapon.animator);

			if(weaponCurrent != -1) {
				// Disable old weapon.
				weaponBag[weaponCurrent].SetActive(false);
			}
			weaponCurrent = index;
			weaponBag[weaponCurrent].SetActive(true);
			return true;
		}
		return false;
	}

	// Whether the weapon to equip exists.
	public bool CanEquip(int index) {
		return index < currentLength && weaponBag[index] != null;
	}

	// Get next available weapon slot.
	// asc = 1 | -1
	public int GetNextAvailable(int asc) {
		int nextpos = weaponCurrent + asc;
		if(nextpos >= currentLength) {
			nextpos = 0;
		} else if (nextpos < 0) {
			nextpos = currentLength - 1;
		}
		return nextpos;
	}

	// Reset current weapon number.
	// @Warning : Should only be executed once!
	public void Refresh() {
		int i;
		for(i=0; i<weaponBag.Length; i++) {
			if(weaponBag[i] == null) {
				break;
			}
			weaponBag[i] = Instantiate(weaponBag[i]);
			// Let the weapon move with cam
			weaponBag[i].transform.parent = player.cam.transform;
			weaponBag[i].SetActive(false);
		}
		this.currentLength = i;
	}

	void Awake() {
		player = gameObject.GetComponent<Player>();
	}

}