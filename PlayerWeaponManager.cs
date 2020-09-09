using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponManager : MonoBehaviour {

	public AbstractPlayer player {get; private set;}

	public List<GameObject> weaponBag;		// Weapon instance. Operations must be sync between weaponBag and prefabDefinition
	public int maxWeapons = 3;
	public GameObject pickablePrefab;

	public int weaponCurrent {get; set;} = -1;		// -1 means no weapon.

	public bool AddBlueprintWeapon(GameObject weaponPrefab) {
		if(weaponBag.Capacity == weaponBag.Count) {
			return false;
		}
		weaponBag.Add(InstantiateWeapon(weaponPrefab));
		return true;
	}

	public bool AddExistWeapon(GameObject weaponInstance) {
		if(weaponBag.Capacity == weaponBag.Count) {
			return false;
		}
		// IMPORTANT ! Otherwise, when player picks up weapon, the weapon object is stil at the root of pickable. 
		// Then it will be deleted by weaponPickable, leaving a null reference to an object, which causes a bunch of errors.
		AttachWeaponToPlayer(weaponInstance);	
		weaponBag.Add(weaponInstance);
		return true;
	}

	// Set weapon instance to player cam root. Do this after every instantiation.
	private void AttachWeaponToPlayer(GameObject weaponInstance) {
		// Let the weapon move with cam
		weaponInstance.transform.parent = player.mainCamera.transform;
		// Make child collider useless
		ToggleChildColliders(weaponInstance, false);
		// Deactive unequipped weapons
		weaponInstance.SetActive(false);
	}

	// Construct weapon instance by prefab definitions.
	public GameObject InstantiateWeapon(GameObject weaponPrefab) {
		GameObject obj = Instantiate<GameObject>(weaponPrefab);
		AttachWeaponToPlayer(obj);
		return obj;
	}

	public bool IsEmpty() {
		return weaponBag.Count == 0;
	}

	public void RemoveWeapon(int index) {
		// Destroy(weaponBag[index]);
		weaponBag.RemoveAt(index);
	}

	// Destroy all attached information, create a model
	public GameObject ThrowWeapon(int index) {
		GameObject weaponInstance = weaponBag[index];
		GameObject pickable = Instantiate<GameObject>(pickablePrefab);
		
		WeaponPickable weaponPickable = pickable.GetComponent<WeaponPickable>();
		weaponPickable.needInstantiation = false;
		weaponPickable.weaponPrefab = weaponInstance;
		
		BaseWeapon weaponData = weaponInstance.GetComponent<BaseWeapon>();
		weaponData.owner = null;

		weaponInstance.transform.SetParent(pickable.transform);

		RemoveWeapon(index);
		weaponCurrent = (int)Mathf.Clamp(weaponCurrent, 0, weaponBag.Count-1);
		PrintList();
		EquipWeapon(weaponCurrent);
		return pickable;
	}

	public bool EquipEmpty() {
		player.weaponPrefab = null;
		return true;
	}

	// Set player's weapon prefab and weapon data.
	public void EquipWeapon(int index) {
		if(index >= 0 && index < weaponBag.Count) {
			GameObject weaponInstance = weaponBag[index];
			BaseWeapon baseWeapon = weaponInstance.GetComponent<BaseWeapon>();
			
			player.weaponPrefab = weaponInstance;
			player.weaponData = baseWeapon;
			
			baseWeapon.owner = player;
			baseWeapon.ownerIsPlayer = true;
			baseWeapon.ResizeCrossHair();
			// Init weapon's place.
			baseWeapon.transform.position = player.anchor.transform.position;
			// Handle deriviation relations in order to make animation system works properly.
			baseWeapon.transform.SetParent(player.anchor.transform);

			if(weaponCurrent != -1) {
				// Disable old weapon.
				weaponBag[weaponCurrent].SetActive(false);
			}
			weaponCurrent = index;
			weaponBag[weaponCurrent].SetActive(true);
		} else {
			// @Warning: empty weapon will not be allowed
			EquipEmpty();
		}
	}

	private void ToggleChildColliders(GameObject weaponInstance, bool enabled) {
		Collider[] childColliders = weaponInstance.GetComponentsInChildren<Collider>();
		foreach(Collider collider in childColliders) {
			collider.enabled = enabled;
		}
	}

	// Whether the weapon to equip exists.
	public bool CanEquip(int index) {
		return index < weaponBag.Count && weaponBag[index] != null;
	}

	// Get next available weapon slot.
	// asc = 1 | -1
	public int GetNextAvailable(int asc) {
		int nextpos = weaponCurrent + asc;
		int currentLength = weaponBag.Count;
		if(nextpos >= currentLength) {
			nextpos = 0;
		} else if (nextpos < 0) {
			nextpos = currentLength - 1;
		}
		return nextpos;
	}

	// Reset current weapon number.
	// @Warning : Should only be executed once!
	public void Init(AbstractPlayer player) {
		this.player = player;
		weaponBag.Capacity = maxWeapons;
		for(int i=0; i<weaponBag.Count; i++) {
			weaponBag[i] = InstantiateWeapon(weaponBag[i]);
		}
	}

	public void PrintList() {
		string sb = "[LIST]";
		foreach(GameObject obj in weaponBag) {
			sb += obj.ToString();
		}
		Debug.Log(sb);
	}
}