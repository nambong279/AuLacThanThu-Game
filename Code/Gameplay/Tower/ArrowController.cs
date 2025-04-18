using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Combat;

namespace AuLacThanThu.Gameplay.Tower
{
    /// <summary>
    /// Điều khiển mũi tên bắn ra từ nỏ thần
    /// </summary>
    public class ArrowController : MonoBehaviour
    {
        #region Properties
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;
        
        // Arrow state
        private float damage;
        private Vector3 direction;
        private bool isCritical;
        private int pierceCount;
        private ElementType elementType = ElementType.None;
        private List<GameObject> hitEnemies = new List<GameObject>();
        
        // Element effects
        private float burnDamage = 0f;
        private float burnDuration = 0f;
        private float poisonPercentage = 0f;
        private float poisonDuration = 0f;
        private int chainCount = 0;
        private float chainDamage = 0f;
        private float chainRange = 3f;
        private float freezePercentage = 0f;
        private float freezeDuration = 0f;
        
        // Components
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        #endregion
        
        #region Initialization
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        private void Start()
        {
            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
            
            // Apply initial movement
            if (rb != null)
            {
                rb.velocity = direction * moveSpeed;
                
                // Rotate arrow to face direction
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            
            // Apply visual effects based on element
            ApplyElementalVisuals();
        }
        
        public void Initialize(float damage, Vector3 direction, bool isCritical, int pierceCount, ElementType elementType)
        {
            this.damage = damage;
            this.direction = direction.normalized;
            this.isCritical = isCritical;
            this.pierceCount = pierceCount;
            this.elementType = elementType;
            
            // Apply correct tag and layer
            gameObject.tag = "PlayerProjectile";
            gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
            
            // Apply visuals for crit
            if (isCritical && spriteRenderer != null)
            {
                // Make critical shots slightly larger
                transform.localScale *= 1.2f;
                
                // Add a glow effect or color change
                spriteRenderer.color = Color.yellow;
            }
        }
        
        private void ApplyElementalVisuals()
        {
            if (spriteRenderer == null) return;
            
            // Apply color and effects based on element
            switch (elementType)
            {
                case ElementType.Fire:
                    spriteRenderer.color = new Color(1f, 0.5f, 0f, 1f); // Orange
                    // TODO: Add fire particle effect
                    break;
                    
                case ElementType.Water:
                    spriteRenderer.color = new Color(0f, 0.7f, 1f, 1f); // Light blue
                    // TODO: Add ice particle effect
                    break;
                    
                case ElementType.Earth:
                    spriteRenderer.color = new Color(0.5f, 0.8f, 0.2f, 1f); // Green
                    // TODO: Add poison particle effect
                    break;
                    
                case ElementType.Lightning:
                    spriteRenderer.color = new Color(0.8f, 0.8f, 1f, 1f); // Light purple
                    // TODO: Add lightning particle effect
                    break;
                    
                default:
                    // Default arrow color
                    break;
            }
        }
        #endregion
        
        #region Collision Handling
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Check if hit an enemy
            if (collision.CompareTag("Enemy"))
            {
                GameObject enemy = collision.gameObject;
                
                // Skip if already hit this enemy (for piercing arrows)
                if (hitEnemies.Contains(enemy))
                    return;
                
                // Add to hit list
                hitEnemies.Add(enemy);
                
                // Apply damage to enemy
                ApplyDamageToEnemy(enemy);
                
                // Check if arrow should be destroyed
                if (pierceCount <= 0)
                {
                    CreateHitEffect(collision.transform.position);
                    Destroy(gameObject);
                }
                else
                {
                    // Reduce pierce count
                    pierceCount--;
                }
            }
            // Check if hit environment
            else if (collision.CompareTag("Environment"))
            {
                CreateHitEffect(collision.transform.position);
                Destroy(gameObject);
            }
        }
        
        private void ApplyDamageToEnemy(GameObject enemy)
        {
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            
            if (enemyBase != null)
            {
                // Apply base damage
                enemyBase.TakeDamage(damage, isCritical, elementType);
                
                // Apply elemental effects
                ApplyElementalEffects(enemyBase);
                
                // Handle chain lightning if applicable
                if (elementType == ElementType.Lightning && chainCount > 0)
                {
                    StartCoroutine(ChainLightningEffect(enemy.transform.position));
                }
            }
        }
        
        private void ApplyElementalEffects(EnemyBase enemy)
        {
            switch (elementType)
            {
                case ElementType.Fire:
                    if (burnDamage > 0)
                    {
                        enemy.ApplyBurnEffect(burnDamage, burnDuration);
                    }
                    break;
                    
                case ElementType.Water:
                    if (freezePercentage > 0)
                    {
                        enemy.ApplyFreezeEffect(freezePercentage, freezeDuration);
                    }
                    break;
                    
                case ElementType.Earth:
                    if (poisonPercentage > 0)
                    {
                        enemy.ApplyPoisonEffect(poisonPercentage, poisonDuration);
                    }
                    break;
                    
                case ElementType.Lightning:
                    // Handled by ChainLightningEffect
                    break;
            }
        }
        
        private IEnumerator ChainLightningEffect(Vector3 sourcePosition)
        {
            if (chainCount <= 0 || chainDamage <= 0)
                yield break;
                
            // Find nearby enemies
            Collider2D[] colliders = Physics2D.OverlapCircleAll(sourcePosition, chainRange);
            List<EnemyBase> validTargets = new List<EnemyBase>();
            
            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag("Enemy") && !hitEnemies.Contains(col.gameObject))
                {
                    EnemyBase enemy = col.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        validTargets.Add(enemy);
                    }
                }
            }
            
            // If no valid targets, exit
            if (validTargets.Count == 0)
                yield break;
                
            // Chain to nearest enemy
            validTargets.Sort((a, b) => 
                Vector3.Distance(a.transform.position, sourcePosition)
                .CompareTo(Vector3.Distance(b.transform.position, sourcePosition))
            );
            
            // Get the closest enemy
            EnemyBase target = validTargets[0];
            hitEnemies.Add(target.gameObject);
            
            // Visual effect for lightning chain
            if (target != null)
            {
                // TODO: Create lightning effect between points
                LineRenderer line = new GameObject("ChainLightning").AddComponent<LineRenderer>();
                line.SetPosition(0, sourcePosition);
                line.SetPosition(1, target.transform.position);
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.cyan;
                line.endColor = Color.blue;
                
                // Apply damage
                target.TakeDamage(chainDamage, false, ElementType.Lightning);
                
                // Clean up
                Destroy(line.gameObject, 0.2f);
                
                // Reduce chain count and continue chain
                chainCount--;
                if (chainCount > 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    StartCoroutine(ChainLightningEffect(target.transform.position));
                }
            }
        }
        
        private void CreateHitEffect(Vector3 position)
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // Play hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, position, 0.5f);
            }
        }
        #endregion
        
        #region Element Setup Methods
        public void SetElementType(ElementType type)
        {
            this.elementType = type;
            ApplyElementalVisuals();
        }
        
        public void SetBurnDamage(float damage, float duration)
        {
            this.burnDamage = damage;
            this.burnDuration = duration;
        }
        
        public void SetPoisonDamage(float percentagePerTick, float duration)
        {
            this.poisonPercentage = percentagePerTick;
            this.poisonDuration = duration;
        }
        
        public void SetChainLightning(int chainCount, float damage, float range)
        {
            this.chainCount = chainCount;
            this.chainDamage = damage;
            this.chainRange = range;
        }
        
        public void SetFreezeEffect(float slowPercentage, float duration)
        {
            this.freezePercentage = slowPercentage;
            this.freezeDuration = duration;
        }
        #endregion
    }
}