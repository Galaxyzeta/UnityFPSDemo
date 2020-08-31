using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : BaseUI {
	
	public Image hpBar;
	public Text hpText;
	private Health health;

	void Start() {
		CommonUtil.IfNullLogError<Image>(hpBar);
		CommonUtil.IfNullLogError<Text>(hpText);
		health = player.health;
		CommonUtil.IfNullLogError<Health>(health);	
	}

	void Update() {
		hpBar.fillAmount = (float)health.hp / health.maxHp;
		hpText.text = health.hp+"/"+health.maxHp;
	}
}