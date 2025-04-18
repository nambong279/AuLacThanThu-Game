using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Combat;

namespace AuLacThanThu.Gameplay.Hero
{
    /// <summary>
    /// Quản lý kỹ năng của anh hùng
    /// </summary>
    [Serializable]
    public class HeroSkill
    {
        #region Properties
        [Header("Basic Info")]
        [SerializeField] private string skillId;
        [SerializeField] private string skillName;
        [SerializeField] private string description;
        [SerializeField] private Sprite skillIcon;
        [SerializeField] private SkillType skillType;
        [SerializeField] private ElementType elementType = ElementType.None;
        
        [Header("Requirements")]
        [SerializeField] private bool isUnlockedByDefault = false;
        [SerializeField] private int requiredHeroStars = 0;
        [SerializeField] private int requiredHeroLevel = 1;
        
        [Header("Active Skill Settings")]
        [SerializeField] private float baseCooldown = 10f;
        [SerializeField] private float baseDamage = 50f;
        [SerializeField] private float baseHeal = 0f;
        [SerializeField] private float baseRange = 5f;
        [SerializeField] private float baseDuration = 3f;
        [SerializeField] private TargetType targetType = TargetType.Enemy;
        [SerializeField] private EffectType effectType = EffectType.Damage;
        [SerializeField] private GameObject skillEffectPrefab;
        [SerializeField] private AudioClip skillSound;
        
        [Header("Passive Skill Settings")]
        [SerializeField] private List<StatBonus> passiveStatBonuses = new List<StatBonus>();
        
        // Runtime variables
        private bool isUnlocked = false;
        private float remainingCooldown = 0f;
        private int level = 1;
        
        // References
        private HeroBase ownerHero;
        #endregion
        
        #region Events
        public delegate void SkillEventHandler(HeroSkill skill);
        public event SkillEventHandler OnSkillActivated;
        public event SkillEventHandler OnSkillUnlocked;
        public event SkillEventHandler OnSkillUpgraded;
        public event SkillEventHandler OnCooldownComplete;
        #endregion
        
        #region Properties
        public string SkillId => skillId;
        public string SkillName => skillName;
        public string Description => description;
        public Sprite SkillIcon => skillIcon;
        public SkillType Type => skillType;
        public ElementType ElementType => elementType;
        public bool IsUnlocked => isUnlocked;
        public float RemainingCooldown => remainingCooldown;
        public int Level => level;
        public float CurrentCooldown => GetCurrentCooldown();
        #endregion
        
        #region Initialization
        public void Initialize(HeroBase owner)
        {
            ownerHero = owner;
            
            // Check if skill should be unlocked by default
            if (isUnlockedByDefault)
            {
                Unlock();
            }
            else
            {
                isUnlocked = false;
            }
            
            // Reset cooldown
            ResetCooldown();
        }
        #endregion
        
        #region Skill Management
        public void Unlock()
        {
            if (isUnlocked) return;
            
            isUnlocked = true;
            
            // Apply passive effects if it's a passive skill
            if (skillType == SkillType.Passive && ownerHero != null)
            {
                ApplyPassiveEffect();
            }
            
            // Trigger event
            OnSkillUnlocked?.Invoke(this);
        }
        
        public bool UpgradeLevel()
        {
            if (!isUnlocked) return false;
            
            level++;
            
            // Recalculate passive bonuses if applicable
            if (skillType == SkillType.Passive && ownerHero != null)
            {
                RemovePassiveEffect();
                ApplyPassiveEffect();
            }
            
            // Trigger event
            OnSkillUpgraded?.Invoke(this);
            
            return true;
        }
        
        public void ResetCooldown()
        {
            remainingCooldown = 0f;
        }
        
        public void UpdateCooldown(float deltaTime)
        {
            if (remainingCooldown > 0)
            {
                remainingCooldown -= deltaTime;
                
                if (remainingCooldown <= 0)
                {
                    remainingCooldown = 0;
                    
                    // Trigger cooldown complete event
                    OnCooldownComplete?.Invoke(this);
                }
            }
        }
        
        public bool IsReady()
        {
            return isUnlocked && remainingCooldown <= 0;
        }
        
        public bool Activate()
        {
            if (!IsReady()) return false;
            
            // Set cooldown
            remainingCooldown = GetCurrentCooldown();
            
            // Execute skill effect based on type
            ExecuteSkillEffect();
            
            // Trigger event
            OnSkillActivated?.Invoke(this);
            
            return true;
        }
        
        public void ApplyPassiveEffect()
        {
            if (skillType != SkillType.Passive || ownerHero == null) return;
            
            // Apply stat bonuses
            foreach (StatBonus bonus in passiveStatBonuses)
            {
                // Apply with level multiplier
                float bonusValue = bonus.Value * (1 + (level - 1) * 0.2f); // 20% increase per level
                
                // TODO: Apply bonus to hero stats
                Debug.Log($"Applied passive bonus {bonus.StatType}: {bonusValue}");
            }
        }
        
        public void RemovePassiveEffect()
        {
            if (skillType != SkillType.Passive || ownerHero == null) return;
            
            // Remove stat bonuses
            foreach (StatBonus bonus in passiveStatBonuses)
            {
                // TODO: Remove bonus from hero stats
                Debug.Log($"Removed passive bonus {bonus.StatType}");
            }
        }
        #endregion
        
        #region Skill Execution
        private void ExecuteSkillEffect()
        {
            // Spawn skill effect if available
            if (skillEffectPrefab != null)
            {
                GameObject effectObj = GameObject.Instantiate(
                    skillEffectPrefab, 
                    ownerHero.transform.position, 
                    Quaternion.identity
                );
                
                // Attach skill data to effect
                SkillEffect effect = effectObj.GetComponent<SkillEffect>();
                if (effect != null)
                {
                    effect.Initialize(this, ownerHero);
                }
                
                // Auto-destroy after duration
                GameObject.Destroy(effectObj, baseDuration * (1 + (level - 1) * 0.1f));
            }
            
            // Play skill sound
            if (skillSound != null && ownerHero != null)
            {
                AudioSource.PlayClipAtPoint(skillSound, ownerHero.transform.position);
            }
            
            // Execute effect based on type
            switch (effectType)
            {
                case EffectType.Damage:
                    ExecuteDamageEffect();
                    break;
                    
                case EffectType.Healing:
                    ExecuteHealingEffect();
                    break;
                    
                case EffectType.Buff:
                    ExecuteBuffEffect();
                    break;
                    
                case EffectType.Debuff:
                    ExecuteDebuffEffect();
                    break;
                    
                case EffectType.Special:
                    ExecuteSpecialEffect();
                    break;
            }
        }
        
        private void ExecuteDamageEffect()
        {
            // Find targets based on target type
            List<GameObject> targets = FindTargets();
            
            // Calculate damage
            float damage = GetCurrentDamage();
            
            // Apply damage to each target
            foreach (GameObject target in targets)
            {
                if (targetType == TargetType.Enemy)
                {
                    EnemyBase enemy = target.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage, false, elementType);
                    }
                }
                else if (targetType == TargetType.Fortress)
                {
                    // Heal fortress instead of damage
                    FortressController fortress = target.GetComponent<FortressController>();
                    if (fortress != null)
                    {
                        fortress.Heal(damage);
                    }
                }
            }
        }
        
        private void ExecuteHealingEffect()
        {
            // Find targets based on target type
            List<GameObject> targets = FindTargets();
            
            // Calculate healing
            float healing = GetCurrentHealing();
            
            // Apply healing to each target
            foreach (GameObject target in targets)
            {
                if (targetType == TargetType.Hero)
                {
                    HeroBase hero = target.GetComponent<HeroBase>();
                    if (hero != null)
                    {
                        // TODO: Add healing method to hero
                        Debug.Log($"Healed hero for {healing}");
                    }
                }
                else if (targetType == TargetType.Fortress)
                {
                    FortressController fortress = target.GetComponent<FortressController>();
                    if (fortress != null)
                    {
                        fortress.Heal(healing);
                    }
                }
            }
        }
        
        private void ExecuteBuffEffect()
        {
            // Find targets based on target type
            List<GameObject> targets = FindTargets();
            
            // Calculate buff duration
            float duration = GetCurrentDuration();
            
            // Apply buff to each target
            foreach (GameObject target in targets)
            {
                if (targetType == TargetType.Hero)
                {
                    HeroBase hero = target.GetComponent<HeroBase>();
                    if (hero != null)
                    {
                        // TODO: Apply buff
                        Debug.Log($"Applied buff to hero for {duration}s");
                    }
                }
            }
        }
        
        private void ExecuteDebuffEffect()
        {
            // Find targets based on target type
            List<GameObject> targets = FindTargets();
            
            // Calculate debuff duration
            float duration = GetCurrentDuration();
            
            // Apply debuff to each target
            foreach (GameObject target in targets)
            {
                if (targetType == TargetType.Enemy)
                {
                    EnemyBase enemy = target.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        // Apply elemental debuff based on element type
                        switch (elementType)
                        {
                            case ElementType.Fire:
                                enemy.ApplyBurnEffect(GetCurrentDamage() * 0.1f, duration);
                                break;
                                
                            case ElementType.Water:
                                enemy.ApplyFreezeEffect(0.5f, duration);
                                break;
                                
                            case ElementType.Earth:
                                enemy.ApplyPoisonEffect(0.05f, duration);
                                break;
                                
                            case ElementType.Lightning:
                                // Stun or chain lightning effect
                                // TODO: Implement stun effect
                                break;
                                
                            default:
                                // Default debuff
                                break;
                        }
                    }
                }
            }
        }
        
        private void ExecuteSpecialEffect()
        {
            // Special effects are unique to each hero skill
            // Override in derived classes or implement through SkillEffect component
            Debug.Log($"Executed special effect for skill {skillName}");
        }
        
        private List<GameObject> FindTargets()
        {
            List<GameObject> targets = new List<GameObject>();
            
            if (ownerHero == null) return targets;
            
            float range = GetCurrentRange();
            
            // Find targets based on type
            switch (targetType)
            {
                case TargetType.Enemy:
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                    foreach (GameObject enemy in enemies)
                    {
                        if (Vector3.Distance(ownerHero.transform.position, enemy.transform.position) <= range)
                        {
                            targets.Add(enemy);
                        }
                    }
                    break;
                    
                case TargetType.Hero:
                    GameObject[] heroes = GameObject.FindGameObjectsWithTag("Hero");
                    foreach (GameObject hero in heroes)
                    {
                        if (Vector3.Distance(ownerHero.transform.position, hero.transform.position) <= range)
                        {
                            targets.Add(hero);
                        }
                    }
                    break;
                    
                case TargetType.Fortress:
                    GameObject fortress = GameObject.FindGameObjectWithTag("Fortress");
                    if (fortress != null)
                    {
                        targets.Add(fortress);
                    }
                    break;
                    
                case TargetType.Self:
                    targets.Add(ownerHero.gameObject);
                    break;
            }
            
            return targets;
        }
        #endregion
        
        #region Stat Calculations
        private float GetCurrentCooldown()
        {
            // Cooldown decreases by 5% per level
            return baseCooldown * (1 - (level - 1) * 0.05f);
        }
        
        private float GetCurrentDamage()
        {
            // Damage increases by 20% per level
            return baseDamage * (1 + (level - 1) * 0.2f);
        }
        
        private float GetCurrentHealing()
        {
            // Healing increases by 20% per level
            return baseHeal * (1 + (level - 1) * 0.2f);
        }
        
        private float GetCurrentRange()
        {
            // Range increases by 10% per level
            return baseRange * (1 + (level - 1) * 0.1f);
        }
        
        private float GetCurrentDuration()
        {
            // Duration increases by 10% per level
            return baseDuration * (1 + (level - 1) * 0.1f);
        }
        #endregion
    }
    
    public enum SkillType
    {
        Active,
        Passive
    }
    
    public enum TargetType
    {
        Enemy,
        Hero,
        Fortress,
        Self
    }
    
    public enum EffectType
    {
        Damage,
        Healing,
        Buff,
        Debuff,
        Special
    }
}