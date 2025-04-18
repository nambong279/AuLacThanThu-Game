using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Combat;

namespace AuLacThanThu.Gameplay.Enemy
{
    /// <summary>
    /// Lớp cơ sở cho tất cả các loại kẻ địch trong game
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        #region Properties
        [Header("Basic Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected float baseDamage = 10f;
        [SerializeField] protected float attackRange = 1f;
        [SerializeField] protected float attackCooldown = 1f;
        [SerializeField] protected float defense = 5f;
        
        [Header("Advanced Stats")]
        [SerializeField] protected float critChance = 0.05f;
        [SerializeField] protected float critDamage = 1.5f;
        [SerializeField] protected float dodgeChance = 0.05f;
        [SerializeField] protected float armor = 0f;     // Damage reduction
        
        [Header("Element Properties")]
        [SerializeField] protected ElementType primaryElement = ElementType.None;
        [SerializeField] protected ElementType secondaryElement = ElementType.None;
        [SerializeField] protected float elementResistance = 0f;  // 0-1 scale, reduces element damage
        
        [Header("References")]
        [SerializeField] protected Transform target;       // Target to move towards (usually fortress)
        [SerializeField] protected GameObject deathEffect;
        [SerializeField] protected AudioClip hitSound;
        [SerializeField] protected AudioClip deathSound;
        
        // Status Effects
        protected bool isFrozen = false;
        protected bool isPoisoned = false;
        protected bool isBurning = false;
        protected float moveSpeedModifier = 1f;
        protected List<StatusEffect> activeStatusEffects = new List<StatusEffect>();
        
        // Components
        protected Rigidbody2D rb;
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Collider2D enemyCollider;
        
        // Internal state
        protected bool isDead = false;
        protected float lastAttackTime = 0f;
        
        // Visual indicators
        protected GameObject healthBar;
        #endregion
        
        #region Events
        // Events that other scripts can subscribe to
        public delegate void EnemyEventHandler(GameObject enemy);
        public event EnemyEventHandler OnEnemyDeath;
        public event EnemyEventHandler OnEnemyDamaged;
        #endregion
        
        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // Get references
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            enemyCollider = GetComponent<Collider2D>();
            
            // Initialize health
            currentHealth = maxHealth;
            
            // Find fortress as default target if none set
            if (target == null)
            {
                GameObject fortress = GameObject.FindGameObjectWithTag("Fortress");
                if (fortress != null)
                {
                    target = fortress.transform;
                }
            }
        }
        
        protected virtual void Start()
        {
            // Create health bar
            InitializeHealthBar();
        }
        
        protected virtual void Update()
        {
            if (isDead) return;
            
            // Update status effects
            UpdateStatusEffects();
            
            // Update health bar
            UpdateHealthBar();
            
            // Check attack cooldown
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // Check if in attack range of target
                if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
                {
                    Attack();
                }
            }
        }
        
        protected virtual void FixedUpdate()
        {
            if (isDead || isFrozen) return;
            
            // Move towards target
            MoveTowardsTarget();
        }
        #endregion
        
        #region Movement & Attack
        protected virtual void MoveTowardsTarget()
        {
            if (target == null || rb == null) return;
            
            // Calculate direction to target
            Vector2 direction = (target.position - transform.position).normalized;
            
            // Move with modified speed
            float finalSpeed = moveSpeed * moveSpeedModifier;
            rb.velocity = direction * finalSpeed;
            
            // Flip sprite based on movement direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
            
            // Play walking animation
            if (animator != null)
            {
                animator.SetFloat("Speed", finalSpeed);
                animator.SetBool("IsMoving", finalSpeed > 0.1f);
            }
        }
        
        protected virtual void Attack()
        {
            // Reset attack timer
            lastAttackTime = Time.time;
            
            // Play attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // Check for critical hit
            bool isCritical = Random.value < critChance;
            
            // Calculate damage
            float damage = baseDamage;
            if (isCritical)
            {
                damage *= critDamage;
            }
            
            // Apply damage to target
            if (target.CompareTag("Fortress"))
            {
                FortressController fortress = target.GetComponent<FortressController>();
                if (fortress != null)
                {
                    fortress.TakeDamage(damage, isCritical, primaryElement);
                }
            }
            else if (target.CompareTag("Hero"))
            {
                // TODO: Implement hero damage
            }
        }
        #endregion
        
        #region Damage Handling
        public virtual void TakeDamage(float damage, bool isCritical, ElementType damageType)
        {
            // Check if enemy can dodge
            if (Random.value < dodgeChance)
            {
                // Dodged!
                ShowDodgeText();
                return;
            }
            
            // Apply defense reduction
            float finalDamage = Mathf.Max(1, damage - defense);
            
            // Apply armor reduction (using formula: armor/(|armor|+200))
            if (armor > 0)
            {
                float damageReduction = armor / (Mathf.Abs(armor) + 200f);
                finalDamage *= (1 - damageReduction);
            }
            
            // Apply element damage modifiers
            finalDamage = ApplyElementalInteractions(finalDamage, damageType);
            
            // Apply final damage
            currentHealth -= finalDamage;
            
            // Show damage number
            ShowDamageText(finalDamage, isCritical);
            
            // Play hit effect
            PlayHitEffects(damageType);
            
            // Trigger damaged event
            OnEnemyDamaged?.Invoke(gameObject);
            
            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        protected virtual float ApplyElementalInteractions(float damage, ElementType damageType)
        {
            float modifiedDamage = damage;
            
            // Check element effectiveness (20% bonus or penalty)
            if (IsElementEffectiveAgainst(damageType, primaryElement))
            {
                modifiedDamage *= 1.2f;
            }
            else if (IsElementEffectiveAgainst(primaryElement, damageType))
            {
                modifiedDamage *= 0.8f;
            }
            
            // Apply element resistance
            if (damageType != ElementType.None && 
                (damageType == primaryElement || damageType == secondaryElement))
            {
                modifiedDamage *= (1 - elementResistance);
            }
            
            return modifiedDamage;
        }
        
        protected bool IsElementEffectiveAgainst(ElementType attacker, ElementType defender)
        {
            // Water > Fire > Earth > Lightning > Water
            if (attacker == ElementType.Water && defender == ElementType.Fire) return true;
            if (attacker == ElementType.Fire && defender == ElementType.Earth) return true;
            if (attacker == ElementType.Earth && defender == ElementType.Lightning) return true;
            if (attacker == ElementType.Lightning && defender == ElementType.Water) return true;
            
            return false;
        }
        
        protected virtual void Die()
        {
            if (isDead) return;
            
            isDead = true;
            
            // Stop movement
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // Disable collider
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
            
            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            
            // Play death effect
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
            
            // Play death sound
            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position);
            }
            
            // Remove health bar
            if (healthBar != null)
            {
                Destroy(healthBar);
            }
            
            // Trigger death event before destroying
            OnEnemyDeath?.Invoke(gameObject);
            
            // Drop loot, add score, etc
            DropLoot();
            
            // Destroy after delay (to allow death animation)
            Destroy(gameObject, 1f);
        }
        #endregion
        
        #region Status Effects
        public virtual void ApplyBurnEffect(float damagePerSecond, float duration)
        {
            // Create new burn status effect
            StatusEffect burnEffect = new StatusEffect(
                "Burn",
                StatusEffectType.Burn,
                duration
            );
            
            burnEffect.SetTickEffect(() =>
            {
                // Apply burn damage every 0.5 seconds
                float tickDamage = damagePerSecond * 0.5f;
                currentHealth -= tickDamage;
                
                // Show small damage number
                ShowDamageText(tickDamage, false, Color.red);
                
                // Check for death
                if (currentHealth <= 0)
                {
                    Die();
                }
            }, 0.5f);
            
            // Replace existing burn or add new
            ReplaceOrAddStatusEffect(burnEffect);
            
            isBurning = true;
            
            // Apply visual effect
            ApplyBurnVisuals(true);
        }
        
        public virtual void ApplyFreezeEffect(float slowPercentage, float duration)
        {
            // Apply slow effect
            moveSpeedModifier = Mathf.Max(0.1f, 1 - slowPercentage);
            
            // Create freeze status effect
            StatusEffect freezeEffect = new StatusEffect(
                "Freeze",
                StatusEffectType.Freeze,
                duration
            );
            
            // When effect starts
            freezeEffect.SetStartEffect(() =>
            {
                moveSpeedModifier = Mathf.Max(0.1f, 1 - slowPercentage);
                isFrozen = slowPercentage >= 0.9f; // Fully frozen if slowed by 90%+
                
                // Apply visual effect
                ApplyFreezeVisuals(true);
            });
            
            // When effect ends
            freezeEffect.SetEndEffect(() =>
            {
                moveSpeedModifier = 1.0f;
                isFrozen = false;
                
                // Remove visual effect
                ApplyFreezeVisuals(false);
            });
            
            // Replace existing freeze or add new
            ReplaceOrAddStatusEffect(freezeEffect);
        }
        
        public virtual void ApplyPoisonEffect(float percentHealthDamage, float duration)
        {
            // Create new poison status effect
            StatusEffect poisonEffect = new StatusEffect(
                "Poison",
                StatusEffectType.Poison,
                duration
            );
            
            poisonEffect.SetTickEffect(() =>
            {
                // Damage based on percentage of max health
                float tickDamage = maxHealth * percentHealthDamage;
                currentHealth -= tickDamage;
                
                // Show damage number
                ShowDamageText(tickDamage, false, Color.green);
                
                // Check for death
                if (currentHealth <= 0)
                {
                    Die();
                }
            }, 1f);
            
            // Replace existing poison or add new
            ReplaceOrAddStatusEffect(poisonEffect);
            
            isPoisoned = true;
            
            // Apply visual effect
            ApplyPoisonVisuals(true);
        }
        
        protected void UpdateStatusEffects()
        {
            // Create a copy of the list to allow removal during iteration
            List<StatusEffect> effects = new List<StatusEffect>(activeStatusEffects);
            
            foreach (StatusEffect effect in effects)
            {
                effect.Update();
                
                // Remove expired effects
                if (effect.IsExpired)
                {
                    activeStatusEffects.Remove(effect);
                    
                    // Update flags based on effect type
                    if (effect.Type == StatusEffectType.Burn) 
                    {
                        isBurning = false;
                        ApplyBurnVisuals(false);
                    }
                    else if (effect.Type == StatusEffectType.Poison)
                    {
                        isPoisoned = false;
                        ApplyPoisonVisuals(false);
                    }
                    else if (effect.Type == StatusEffectType.Freeze)
                    {
                        isFrozen = false;
                        moveSpeedModifier = 1.0f;
                        ApplyFreezeVisuals(false);
                    }
                }
            }
        }
        
        private void ReplaceOrAddStatusEffect(StatusEffect newEffect)
        {
            // Find existing effect of same type
            StatusEffect existingEffect = activeStatusEffects.Find(e => e.Type == newEffect.Type);
            
            if (existingEffect != null)
            {
                // Replace if new effect has longer duration
                if (newEffect.RemainingDuration > existingEffect.RemainingDuration)
                {
                    activeStatusEffects.Remove(existingEffect);
                    activeStatusEffects.Add(newEffect);
                    newEffect.Start();
                }
            }
            else
            {
                // Add new effect
                activeStatusEffects.Add(newEffect);
                newEffect.Start();
            }
        }
        #endregion
        
        #region Visual Effects
        protected virtual void ApplyBurnVisuals(bool isActive)
        {
            // Override in specific enemy types for custom effects
            
            // Apply a red tint when burning
            if (spriteRenderer != null)
            {
                if (isActive)
                {
                    spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 1f);
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
            }
            
            // TODO: Add particle effect for burning
        }
        
        protected virtual void ApplyFreezeVisuals(bool isActive)
        {
            // Override in specific enemy types for custom effects
            
            // Apply a blue tint when frozen
            if (spriteRenderer != null)
            {
                if (isActive)
                {
                    spriteRenderer.color = new Color(0.7f, 0.7f, 1f, 1f);
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
            }
            
            // TODO: Add particle effect for freezing
        }
        
        protected virtual void ApplyPoisonVisuals(bool isActive)
        {
            // Override in specific enemy types for custom effects
            
            // Apply a green tint when poisoned
            if (spriteRenderer != null)
            {
                if (isActive)
                {
                    spriteRenderer.color = new Color(0.7f, 1f, 0.7f, 1f);
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
            }
            
            // TODO: Add particle effect for poison
        }
        
        protected virtual void PlayHitEffects(ElementType damageType)
        {
            // Play hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.5f);
            }
            
            // Play hit animation
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
            
            // Add different visual effects based on damage type
            switch (damageType)
            {
                case ElementType.Fire:
                    // Fire hit effect
                    break;
                    
                case ElementType.Water:
                    // Water/Ice hit effect
                    break;
                    
                case ElementType.Earth:
                    // Poison hit effect
                    break;
                    
                case ElementType.Lightning:
                    // Lightning hit effect
                    break;
                    
                default:
                    // Default hit effect
                    break;
            }
        }
        
        protected virtual void ShowDamageText(float amount, bool isCritical, Color? color = null)
        {
            // Create floating text object
            // Override in derived classes or implement a damage text system
            Debug.Log($"Enemy takes {amount} damage. Critical: {isCritical}");
        }
        
        protected virtual void ShowDodgeText()
        {
            // Create "Dodge" floating text
            Debug.Log("Enemy dodged the attack");
        }
        #endregion
        
        #region Health Bar
        protected virtual void InitializeHealthBar()
        {
            // Create simple health bar
            // Override in derived classes or implement a health bar system
        }
        
        protected virtual void UpdateHealthBar()
        {
            // Update health bar position and fill amount
            // Override in derived classes
        }
        #endregion
        
        #region Loot
        protected virtual void DropLoot()
        {
            // Override in derived classes to implement loot dropping
            // For now, just add score or resources
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // TODO: Add resources, experience, etc.
            }
        }
        #endregion
    }
}