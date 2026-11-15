using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "RegresoALaLuz/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    public int levelIndex = 1;
    public string levelTitle = "NIVEL 1 - Ciudad apagada";
    public int requiredItems = 5;
    public int maxHints = 3;
    public int playerLives = 3;
    public float enemySpeedMultiplier = 1.0f;
    public float maintenanceAxeRange = 2.6f;
    public float plasmaAxeRange = 3.4f;
    public float stompBounceForce = 11f;
    public bool allowStompOnBoss = false;
    [TextArea] public string[] pedagogicalHints;
}
