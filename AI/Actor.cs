using UnityEngine;

public class Actor : Damageable {
    public const int TEAM_RED = 0;
    public const int TEAM_BLUE = 1;

    [Header("Actor")]
    public int team = TEAM_RED;
}
