using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Core;
using AuLacThanThu.Gameplay.Combat;

namespace AuLacThanThu.Gameplay.Tower
{
    /// <summary>
    /// Quản lý thành trì - mục tiêu chính cần bảo vệ trong game
    /// </summary>
    public class FortressController : MonoBehaviour
    {
        #region Properties
        [Header("Basic Stats")]
        [SerializeField] private float baseMaxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float baseDefense = 10f;
        [SerializeField] private float baseHealthRegen = 0f;  // HP regenerated per second
        
        [Header("Advanced Stats")]
        [SerializeField] private float dodgeChance = 0.05f;
        [SerializeField] private float critReductionChance = 0f;   // Chance to reduce crit damage
        [SerializeField] private float critDamageReduction = 0f;   // Reduces crit damage by this percentage
        [SerializeField] private float armor = 0f;                 // Damage reduction formula: armor/(|armor|+200)
        [SerializeField] private float armorPenetrationResistance = 0f; // Reduces enemy armor penetration
        
        [Header("Element Resistances")]
        [SerializeField] private float fireResistance = 0f;        // 0-1, reduces fire damage
        [SerializeField] private float waterResistance = 0f;       // 0-1, reduces water damage
        [SerializeField] private float earthResistance = 0f;       // 0-1, reduces earth damage
        [SerializeField] private float lightningResistance = 0f;   // 0-1, reduces lightning damage
        
        [Header("Visual")]
        [SerializeField] private Transform healthBarTransform;
        [SerializeField] private SpriteRenderer healthBarRenderer;
        [SerializeField] private GameObject fortressDamagedEffect;
        [SerializeField] private GameObject fortressDestroyedEffect;
        [SerializeField] private List<Sprite> fortressDamageStates = new List<Sprite>();
        
        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destructionSound;
        [SerializeField] private AudioClip healSound;
        
        // References
        private SpriteRenderer fortressRenderer;
        private Animator fortressAnimator;
        
        // Additional stats from upgrades
        private float maxHealthModifier = 1.0f;
        private float defenseModifier = 1.0f;
        private float damageReductionModifier = 0f;
        
        // Coefficients for fortress stats
        private const float WALL_HP_COEF = 1.0f;        // Wall HP upgrade gives 1.0x HP
        private const float GATE_DEFENSE_COEF = 1.0f;   // Gate upgrade gives 1.0x defense
        
        // Status tracking
        private bool isDestroyed = false;
        
        // Upgrade levels
        private int wallLevel = 1;
        private int gateLevel = 1;
        private int crossbowLevel = 1;
        #endregion
        
        #region Events
        public delegate void FortressEventHandler();
        public event FortressEventHandler OnFortressDestroyed;
        public event FortressEventHandler OnFortressDamaged;
        public event FortressEventHandler OnFortressHealed;
        
        public delegate void FortressUpgradeHandler(int wallLevel, int gateLevel, int crossbowLevel);
        public event FortressUpgradeHandler OnFortressUpgraded;
        #endregion
        
        #region Initialization
        private void Awake()
        {
            fortressRenderer = GetComponent<SpriteRenderer>();
            fortressAnimator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            // Calculate actual max health based on wall level and modifiers
            float actualMaxHealth = CalculateMaxHealth();
            currentHealth = actualMaxHealth;
            
            // Set proper visual state
            UpdateVisuals();
        }
        
        private void Update()
        {
            // Health regeneration
            if (baseHealthRegen > 0 && currentHealth < CalculateMaxHealth())
            {
                Heal(baseHealthRegen * Time.deltaTime);
            }
        }
        #endregion
        
        #region Stat Calculations
        public float CalculateMaxHealth()
        {
            // Base health + wall level bonus + upgrades
            return baseMaxHealth * (1 + (wallLevel - 1) * WALL_HP_COEF) * maxHealthModifier;
        }
        
        public float CalculateDefense()
        {
            // Base defense + gate level bonus + upgrades
            return baseDefense * (1 + (gateLevel - 1) * GATE_DEFENSE_COEF) * defenseModifier;
        }
        #endregion
        
        #region Damage Handling
        public void TakeDamage(float damage, bool isCritical, ElementType damageType)
        {
            if (isDestroyed) return;
            
            // Check if attack is dodged
            if (Random.value < dodgeChance)
            {
                ShowDodgeText();
                return;
            }
            
            // If critical, check if crit is reduced
            if (isCritical && Random.value < critReductionChance)
            {
                damage *= (1 - critDamageReduction);
            }
            
            // Apply defense reduction
            float finalDamage = Mathf.Max(1, damage - CalculateDefense());
            
            // Apply armor reduction
            if (armor > 0)
            {
                float damageReduction = armor / (Mathf.Abs(armor) + 200f);
                finalDamage *= (1 - damageReduction);
            }
            
            // Apply elemental resistance
            finalDamage = ApplyElementalResistance(finalDamage, damageType);
            
            // Apply global damage reduction
            finalDamage *= (1 - damageReductionModifier);
            
            // Apply damage
            currentHealth -= finalDamage;
            
            // Show damage number
            ShowDamageText(finalDamage, isCritical);
            
            // Play hit sound and effect
            PlayHitEffects();
            
            // Trigger damaged event
            OnFortressDamaged?.Invoke();
            
            // Update visuals based on health percentage
            UpdateVisuals();
            
            // Check for destruction
            if (currentHealth <= 0)
            {
                DestroyFortress();
            }
        }
        
        private float ApplyElementalResistance(float damage, ElementType damageType)
        {
            switch (damageType)
            {
                case ElementType.Fire:
                    return damage * (1 - fireResistance);
                case ElementType.Water:
                    return damage * (1 - waterResistance);
                case ElementType.Earth:
                    return damage * (1 - earthResistance);
                case ElementType.Lightning:
                    return damage * (1 - lightningResistance);
                default:
                    return damage;
            }
        }
        
        public void Heal(float amount)
        {
            if (isDestroyed) return;
            
            float maxHealth = CalculateMaxHealth();
            
            // Calculate actual healing (don't exceed max health)
            float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
            
            if (actualHeal <= 0) return;
            
            // Apply healing
            currentHealth += actualHeal;
            
            // Play heal effect and sound
            if (healSound != null)
            {
                AudioSource.PlayClipAtPoint(healSound, transform.position, 0.5f);
            }
            
            // Show heal number
            ShowHealText(actualHeal);
            
            // Trigger event
            OnFortressHealed?.Invoke();
            
            // Update visuals
            UpdateVisuals();
        }
        
        private void DestroyFortress()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            currentHealth = 0;
            
            // Play destruction animation
            if (fortressAnimator != null)
            {
                fortressAnimator.SetTrigger("Destroyed");
            }
            
            // Play destruction effect
            if (fortressDestroyedEffect != null)
            {
                Instantiate(fortressDestroyedEffect, transform.position, Quaternion.identity);
            }
            
            // Play destruction sound
            if (destructionSound != null)
            {
                AudioSource.PlayClipAtPoint(destructionSound, transform.position, 1.0f);
            }
            
            // Trigger destroyed event
            OnFortressDestroyed?.Invoke();
        }
        #endregion
        
        #region Upgrade Methods
        public void UpgradeWall(int newLevel)
        {
            int previousLevel = wallLevel;
            wallLevel = newLevel;
            
            // Increase max health
            float previousMaxHealth = CalculateMaxHealth();
            float newMaxHealth = baseMaxHealth * (1 + (wallLevel - 1) * WALL_HP_COEF) * maxHealthModifier;
            
            // Scale current health proportionally
            if (currentHealth > 0)
            {
                float healthPercentage = currentHealth / previousMaxHealth;
                currentHealth = healthPercentage * newMaxHealth;
            }
            
            // Update visuals
            UpdateVisuals();
            
            // Trigger upgrade event
            OnFortressUpgraded?.Invoke(wallLevel, gateLevel, crossbowLevel);
            
            Debug.Log($"Wall upgraded from level {previousLevel} to {wallLevel}. New max health: {newMaxHealth}");
        }
        
        public void UpgradeGate(int newLevel)
        {
            int previousLevel = gateLevel;
            gateLevel = newLevel;
            
            // Update visuals
            UpdateVisuals();
            
            // Trigger upgrade event
            OnFortressUpgraded?.Invoke(wallLevel, gateLevel, crossbowLevel);
            
            Debug.Log($"Gate upgraded from level {previousLevel} to {gateLevel}. New defense: {CalculateDefense()}");
        }
        
        public void UpgradeCrossbow(int newLevel)
        {
            int previousLevel = crossbowLevel;
            crossbowLevel = newLevel;
            
            // Trigger upgrade event
            OnFortressUpgraded?.Invoke(wallLevel, gateLevel, crossbowLevel);
            
            Debug.Log($"Crossbow upgraded from level {previousLevel} to {crossbowLevel}");
        }
        
        public void ApplyStatModifier(FortressStatType statType, float value)
        {
            switch (statType)
            {
                case FortressStatType.MaxHealth:
                    maxHealthModifier += value;
                    // Recalculate max health
                    float newMaxHealth = CalculateMaxHealth();
                    currentHealth = Mathf.Min(currentHealth, newMaxHealth);
                    break;
                    
                case FortressStatType.Defense:
                    defenseModifier += value;
                    break;
                    
                case FortressStatType.DamageReduction:
                    damageReductionModifier = Mathf.Clamp01(damageReductionModifier + value);
                    break;
                    
                case FortressStatType.HealthRegen:
                    baseHealthRegen += value;
                    break;
                    
                case FortressStatType.DodgeChance:
                    dodgeChance = Mathf.Clamp01(dodgeChance + value);
                    break;
                    
                case FortressStatType.Armor:
                    armor += value;
                    break;
                    
                case FortressStatType.CritReduction:
                    critDamageReduction = Mathf.Clamp01(critDamageReduction + value);
                    break;
                    
                case FortressStatType.CritReductionChance:
                    critReductionChance = Mathf.Clamp01(critReductionChance + value);
                    break;
                    
                default:
                    Debug.LogWarning($"Unknown fortress stat type: {statType}");
                    break;
            }
            
            // Update visuals
            UpdateVisuals();
            
            Debug.Log($"Applied {value} to {statType}");
        }
        
        public void ApplyElementResistance(ElementType elementType, float value)
        {
            switch (elementType)
            {
                case ElementType.Fire:
                    fireResistance = Mathf.Clamp01(fireResistance + value);
                    break;
                    
                case ElementType.Water:
                    waterResistance = Mathf.Clamp01(waterResistance + value);
                    break;
                    
                case ElementType.Earth:
                    earthResistance = Mathf.Clamp01(earthResistance + value);
                    break;
                    
                case ElementType.Lightning:
                    lightningResistance = Mathf.Clamp01(lightningResistance + value);
                    break;
                    
                default:
                    Debug.LogWarning($"Unknown element type for resistance: {elementType}");
                    break;
            }
            
            Debug.Log($"Applied {value} resistance to {elementType}");
        }
        #endregion
        
        #region Visual Effects
        private void UpdateVisuals()
        {
            // Update health bar
            if (healthBarTransform != null && healthBarRenderer != null)
            {
                float healthPercentage = Mathf.Clamp01(currentHealth / CalculateMaxHealth());
                
                // Scale health bar
                Vector3 scale = healthBarTransform.localScale;
                scale.x = healthPercentage;
                healthBarTransform.localScale = scale;
                
                // Change color based on health
                if (healthPercentage > 0.6f)
                {
                    healthBarRenderer.color = Color.green;
                }
                else if (healthPercentage > 0.3f)
                {
                    healthBarRenderer.color = Color.yellow;
                }
                else
                {
                    healthBarRenderer.color = Color.red;
                }
            }
            
            // Update fortress sprite based on damage state
            if (fortressRenderer != null && fortressDamageStates.Count > 0)
            {
                float healthPercentage = Mathf.Clamp01(currentHealth / CalculateMaxHealth());
                
                int stateIndex = Mathf.FloorToInt((1 - healthPercentage) * fortressDamageStates.Count);
                stateIndex = Mathf.Clamp(stateIndex, 0, fortressDamageStates.Count - 1);
                
                fortressRenderer.sprite = fortressDamageStates[stateIndex];
            }
        }
        
        private void PlayHitEffects()
        {
            // Play hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.5f);
            }
            
            // Play hit animation
            if (fortressAnimator != null)
            {
                fortressAnimator.SetTrigger("Hit");
            }
            
            // Show hit effect
            if (fortressDamagedEffect != null)
            {
                Instantiate(fortressDamagedEffect, transform.position, Quaternion.identity);
            }
        }
        
        private void ShowDamageText(float amount, bool isCritical)
        {
            // TODO: Implement damage text system
            Debug.Log($"Fortress takes {amount} damage. Critical: {isCritical}");
        }
        
        private void ShowHealText(float amount)
        {
            // TODO: Implement heal text system
            Debug.Log($"Fortress healed for {amount}");
        }
        
        private void ShowDodgeText()
        {
            // TODO: Implement dodge text system
            Debug.Log("Fortress dodged the attack");
        }
        #endregion
        
        #region Public Getters
        public float GetHealthPercentage()
        {
            return Mathf.Clamp01(currentHealth / CalculateMaxHealth());
        }
        
        public int GetWallLevel() => wallLevel;
        public int GetGateLevel() => gateLevel;
        public int GetCrossbowLevel() => crossbowLevel;
        
        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => CalculateMaxHealth();
        public float GetDefense() => CalculateDefense();
        #endregion
    }
    
    public enum FortressStatType
    {
        MaxHealth,
        Defense,
        DamageReduction,
        HealthRegen,
        DodgeChance,
        Armor,
        CritReduction,
        CritReductionChance
    }
}