using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Core;
using AuLacThanThu.Gameplay.Combat;

namespace AuLacThanThu.Gameplay.Enemy
{
    /// <summary>
    /// Điều khiển boss với các giai đoạn và tập kỹ năng khác nhau
    /// </summary>
    public class BossController : EnemyBase
    {
        #region Properties
        [Header("Boss Settings")]
        [SerializeField] private string bossId;
        [SerializeField] private string bossName;
        [SerializeField] private int bossLevel = 1;
        
        [Header("Phase Settings")]
        [SerializeField] private int phaseCount = 1;
        [SerializeField] private int currentPhase = 1;
        [SerializeField] private float[] phaseTransitionThresholds = { 0.7f, 0.4f };
        [SerializeField] private bool immuneDuringTransition = true;
        [SerializeField] private float phaseTransitionDuration = 2f;
        
        [Header("Ability Settings")]
        [SerializeField] private List<BossAbility> abilities = new List<BossAbility>();
        [SerializeField] private List<BossAbility> phaseTransitionAbilities = new List<BossAbility>();
        [SerializeField] private float minTimeBetweenAbilities = 3f;
        [SerializeField] private float maxTimeBetweenAbilities = 6f;
        
        [Header("Enrage Settings")]
        [SerializeField] private bool hasEnrageTimer = false;
        [SerializeField] private float enrageTimerDuration = 180f;
        [SerializeField] private float enrageAttackMultiplier = 1.5f;
        [SerializeField] private float enrageSpeedMultiplier = 1.3f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject phaseTransitionEffect;
        [SerializeField] private GameObject enrageEffect;
        [SerializeField] private GameObject[] phaseVisuals;
        
        // Runtime state
        private float abilityTimer;
        private float enrageTimer;
        private bool isInPhaseTransition;
        private bool isEnraged;
        private BossAbility lastUsedAbility;
        
        // Animation parameters
        private static readonly int AnimPhaseParam = Animator.StringToHash("Phase");
        private static readonly int AnimEnragedParam = Animator.StringToHash("Enraged");
        private static readonly int AnimTransitionParam = Animator.StringToHash("PhaseTransition");
        
        // Active ability effects
        private List<GameObject> activeEffects = new List<GameObject>();
        #endregion
        
        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize ability timer
            abilityTimer = Random.Range(minTimeBetweenAbilities, maxTimeBetweenAbilities);
            
            // Initialize enrage timer if needed
            if (hasEnrageTimer)
            {
                enrageTimer = enrageTimerDuration;
            }
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Announce boss spawn
            AnnounceSpawn();
            
            // Initialize phase visuals
            UpdatePhaseVisuals();
        }
        
        protected override void Update()
        {
            // Skip update if transitioning between phases
            if (isInPhaseTransition)
                return;
                
            base.Update();
            
            // Update ability timer
            UpdateAbilityTimer();
            
            // Update enrage timer
            UpdateEnrageTimer();
            
            // Check for phase transitions
            CheckPhaseTransitions();
        }
        #endregion
        
        #region Ability Management
        private void UpdateAbilityTimer()
        {
            if (isDead) return;
            
            abilityTimer -= Time.deltaTime;
            
            if (abilityTimer <= 0f)
            {
                // Try to use an ability
                UseRandomAbility();
                
                // Reset timer
                abilityTimer = Random.Range(minTimeBetweenAbilities, maxTimeBetweenAbilities);
                
                // Reduce timer if enraged
                if (isEnraged)
                {
                    abilityTimer *= 0.7f;
                }
            }
        }
        
        private void UseRandomAbility()
        {
            // Get abilities for current phase
            List<BossAbility> availableAbilities = GetAvailableAbilities();
            
            if (availableAbilities.Count == 0)
                return;
                
            // Filter abilities that are off cooldown
            List<BossAbility> readyAbilities = availableAbilities.FindAll(a => a.IsReady());
            
            if (readyAbilities.Count == 0)
                return;
                
            // Pick random ability weighted by chance
            BossAbility selectedAbility = PickRandomAbility(readyAbilities);
            
            if (selectedAbility != null)
            {
                UseAbility(selectedAbility);
                lastUsedAbility = selectedAbility;
            }
        }
        
        private List<BossAbility> GetAvailableAbilities()
        {
            return abilities.FindAll(a => a.MinPhase <= currentPhase && a.MaxPhase >= currentPhase);
        }
        
        private BossAbility PickRandomAbility(List<BossAbility> abilities)
        {
            if (abilities.Count == 0)
                return null;
                
            // Calculate total weight
            float totalWeight = 0f;
            foreach (var ability in abilities)
            {
                totalWeight += ability.UseWeight;
            }
            
            // Pick random ability
            float randomValue = Random.Range(0f, totalWeight);
            float accumulatedWeight = 0f;
            
            foreach (var ability in abilities)
            {
                accumulatedWeight += ability.UseWeight;
                
                if (randomValue <= accumulatedWeight)
                {
                    return ability;
                }
            }
            
            // Fallback to first ability
            return abilities[0];
        }
        
        private void UseAbility(BossAbility ability)
        {
            Debug.Log($"Boss {bossName} using ability: {ability.AbilityName}");
            
            // Trigger animation
            if (animator != null && !string.IsNullOrEmpty(ability.AnimationTrigger))
            {
                animator.SetTrigger(ability.AnimationTrigger);
            }
            
            // Create ability effect
            if (ability.EffectPrefab != null)
            {
                GameObject effect = Instantiate(ability.EffectPrefab, transform.position, Quaternion.identity);
                
                // Set up effect
                BossAbilityEffect abilityEffect = effect.GetComponent<BossAbilityEffect>();
                if (abilityEffect != null)
                {
                    abilityEffect.Initialize(this, ability);
                }
                
                // Add to active effects
                activeEffects.Add(effect);
                
                // Schedule cleanup
                StartCoroutine(CleanupEffectAfterDelay(effect, ability.Duration));
            }
            
            // Apply ability effects based on type
            ApplyAbilityEffects(ability);
            
            // Start cooldown
            ability.StartCooldown();
        }
        
        private IEnumerator CleanupEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (effect != null)
            {
                activeEffects.Remove(effect);
                Destroy(effect);
            }
        }
        
        private void ApplyAbilityEffects(BossAbility ability)
        {
            switch (ability.AbilityType)
            {
                case BossAbilityType.Attack:
                    ApplyAttackAbility(ability);
                    break;
                    
                case BossAbilityType.AOE:
                    ApplyAOEAbility(ability);
                    break;
                    
                case BossAbilityType.Summon:
                    ApplySummonAbility(ability);
                    break;
                    
                case BossAbilityType.Buff:
                    ApplyBuffAbility(ability);
                    break;
                    
                case BossAbilityType.Movement:
                    ApplyMovementAbility(ability);
                    break;
                    
                case BossAbilityType.StatusEffect:
                    ApplyStatusEffectAbility(ability);
                    break;
            }
        }
        
        private void ApplyAttackAbility(BossAbility ability)
        {
            // Find target
            GameObject target = FindTarget();
            
            if (target == null)
                return;
                
            // Calculate damage
            float damage = baseDamage * ability.DamageMultiplier;
            
            // Apply enrage bonus
            if (isEnraged)
            {
                damage *= enrageAttackMultiplier;
            }
            
            // Apply damage
            if (target.CompareTag("Fortress"))
            {
                FortressController fortress = target.GetComponent<FortressController>();
                if (fortress != null)
                {
                    fortress.TakeDamage(damage, false, ability.ElementType);
                }
            }
            else if (target.CompareTag("Hero"))
            {
                // TODO: Apply damage to hero
                Debug.Log($"Apply {damage} damage to hero");
            }
        }
        
        private void ApplyAOEAbility(BossAbility ability)
        {
            // Find all targets in range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, ability.AreaRadius);
            
            foreach (Collider2D collider in colliders)
            {
                if (collider.CompareTag("Fortress") || collider.CompareTag("Hero"))
                {
                    // Calculate damage
                    float damage = baseDamage * ability.DamageMultiplier;
                    
                    // Apply enrage bonus
                    if (isEnraged)
                    {
                        damage *= enrageAttackMultiplier;
                    }
                    
                    // Apply damage
                    if (collider.CompareTag("Fortress"))
                    {
                        FortressController fortress = collider.GetComponent<FortressController>();
                        if (fortress != null)
                        {
                            fortress.TakeDamage(damage, false, ability.ElementType);
                        }
                    }
                    else if (collider.CompareTag("Hero"))
                    {
                        // TODO: Apply damage to hero
                        Debug.Log($"Apply {damage} damage to hero");
                    }
                }
            }
        }
        
        private void ApplySummonAbility(BossAbility ability)
        {
            // Determine number of summons
            int summonCount = ability.SummonCount;
            
            // Get position offsets
            List<Vector3> summonPositions = new List<Vector3>();
            
            for (int i = 0; i < summonCount; i++)
            {
                // Calculate position in circle around boss
                float angle = i * 360f / summonCount;
                float radian = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0) * 2f;
                
                summonPositions.Add(transform.position + offset);
            }
            
            // Summon enemies
            for (int i = 0; i < summonCount; i++)
            {
                if (string.IsNullOrEmpty(ability.SummonEnemyId))
                    continue;
                    
                // Create summon
                EnemyFactory.Instance?.CreateEnemy(ability.SummonEnemyId, summonPositions[i], Quaternion.identity);
            }
        }
        
        private void ApplyBuffAbility(BossAbility ability)
        {
            // Apply buff to self
            StartCoroutine(ApplyTempBuffs(ability));
        }
        
        private IEnumerator ApplyTempBuffs(BossAbility ability)
        {
            // Apply buffs
            moveSpeedModifier += ability.SpeedModifier;
            
            // Wait for duration
            yield return new WaitForSeconds(ability.Duration);
            
            // Remove buffs
            moveSpeedModifier -= ability.SpeedModifier;
        }
        
        private void ApplyMovementAbility(BossAbility ability)
        {
            // Teleport or dash
            if (ability.IsTeleport)
            {
                // Find random position not too close to fortress
                Vector3 targetPos = transform.position;
                
                // Try 5 times to find a valid position
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomDirection = Random.insideUnitCircle.normalized * ability.MovementDistance;
                    Vector3 potentialPos = transform.position + randomDirection;
                    
                    // Check if position is valid (would need additional logic in a real game)
                    targetPos = potentialPos;
                    break;
                }
                
                // Teleport with effect
                StartCoroutine(TeleportWithEffect(targetPos));
            }
            else
            {
                // Dash toward target
                GameObject target = FindTarget();
                
                if (target != null)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    Vector3 dashTarget = transform.position + direction * ability.MovementDistance;
                    
                    // Dash with effect
                    StartCoroutine(DashWithEffect(dashTarget, ability.Duration));
                }
            }
        }
        
        private void ApplyStatusEffectAbility(BossAbility ability)
        {
            // Find targets
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, ability.AreaRadius);
            
            foreach (Collider2D collider in colliders)
            {
                if (collider.CompareTag("Hero"))
                {
                    // TODO: Apply status effect to hero
                    Debug.Log($"Apply status effect to hero: {ability.StatusEffectType}");
                }
            }
        }
        
        private IEnumerator TeleportWithEffect(Vector3 targetPosition)
        {
            // Play disappear effect
            GameObject disappearEffect = Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            
            // Disable sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            
            // Wait a moment
            yield return new WaitForSeconds(0.5f);
            
            // Move to new position
            transform.position = targetPosition;
            
            // Play appear effect
            GameObject appearEffect = Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            
            // Enable sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            
            // Cleanup
            Destroy(disappearEffect, 1f);
            Destroy(appearEffect, 1f);
        }
        
        private IEnumerator DashWithEffect(Vector3 targetPosition, float duration)
        {
            float timer = 0f;
            Vector3 startPosition = transform.position;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                
                // Move towards target
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                
                yield return null;
            }
            
            // Ensure final position
            transform.position = targetPosition;
        }
        
        private GameObject FindTarget()
        {
            // Find fortress
            GameObject fortress = GameObject.FindGameObjectWithTag("Fortress");
            
            // TODO: Find heroes based on targeting priority
            
            return fortress;
        }
        #endregion
        
        #region Phase Management
        private void CheckPhaseTransitions()
        {
            if (isDead || isInPhaseTransition || currentPhase >= phaseCount)
                return;
                
            // Check health thresholds
            float healthPercent = currentHealth / maxHealth;
            
            // Check if we should transition to next phase
            if (currentPhase - 1 < phaseTransitionThresholds.Length && 
                healthPercent <= phaseTransitionThresholds[currentPhase - 1])
            {
                StartPhaseTransition();
            }
        }
        
        private void StartPhaseTransition()
        {
            isInPhaseTransition = true;
            
            // Notify transition
            Debug.Log($"Boss {bossName} transitioning to phase {currentPhase + 1}");
            
            // Trigger transition animation
            if (animator != null)
            {
                animator.SetTrigger(AnimTransitionParam);
            }
            
            // Play transition effect
            if (phaseTransitionEffect != null)
            {
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            }
            
            // Make immune during transition if needed
            if (immuneDuringTransition)
            {
                // Set temporary immunity
                isInvulnerable = true;
            }
            
            // Start transition process
            StartCoroutine(PhaseTransitionProcess());
        }
        
        private IEnumerator PhaseTransitionProcess()
        {
            // Wait for transition time
            yield return new WaitForSeconds(phaseTransitionDuration);
            
            // Increment phase
            currentPhase++;
            
            // Use transition ability if available
            if (currentPhase - 1 < phaseTransitionAbilities.Count)
            {
                BossAbility transitionAbility = phaseTransitionAbilities[currentPhase - 1];
                if (transitionAbility != null)
                {
                    UseAbility(transitionAbility);
                }
            }
            
            // Update animator
            if (animator != null)
            {
                animator.SetInteger(AnimPhaseParam, currentPhase);
            }
            
            // Update visuals
            UpdatePhaseVisuals();
            
            // End transition
            isInPhaseTransition = false;
            
            // Remove immunity
            if (immuneDuringTransition)
            {
                isInvulnerable = false;
            }
        }
        
        private void UpdatePhaseVisuals()
        {
            // Disable all phase visuals
            if (phaseVisuals != null)
            {
                for (int i = 0; i < phaseVisuals.Length; i++)
                {
                    if (phaseVisuals[i] != null)
                    {
                        phaseVisuals[i].SetActive(i == currentPhase - 1);
                    }
                }
            }
        }
        #endregion
        
        #region Enrage Management
        private void UpdateEnrageTimer()
        {
            if (!hasEnrageTimer || isEnraged)
                return;
                
            enrageTimer -= Time.deltaTime;
            
            if (enrageTimer <= 0f)
            {
                EnterEnrageMode();
            }
        }
        
        private void EnterEnrageMode()
        {
            if (isEnraged)
                return;
                
            isEnraged = true;
            
            // Log enrage
            Debug.Log($"Boss {bossName} has enraged!");
            
            // Update animator
            if (animator != null)
            {
                animator.SetBool(AnimEnragedParam, true);
            }
            
            // Play enrage effect
            if (enrageEffect != null)
            {
                Instantiate(enrageEffect, transform.position, Quaternion.identity);
            }
            
            // Apply enrage effects
            moveSpeedModifier *= enrageSpeedMultiplier;
            
            // TODO: Apply other enrage effects like increased attack speed, new abilities, etc.
        }
        #endregion
        
        #region Boss Specific Overrides
        protected override void Die()
        {
            // Clean up active effects
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            activeEffects.Clear();
            
            // Play special boss death effect
            // TODO: Add special boss death sequence
            
            // Call base implementation
            base.Die();
        }
        
        private void AnnounceSpawn()
        {
            // Show boss name
            // TODO: Implement boss announcement UI
            
            Debug.Log($"Boss appeared: {bossName}");
            
            // Play special spawn effect
            if (phaseTransitionEffect != null)
            {
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            }
        }
        
        public override void TakeDamage(float damage, bool isCritical, ElementType damageType)
        {
            // Skip if in transition and immune
            if (isInPhaseTransition && immuneDuringTransition)
            {
                // Show immune effect
                ShowDodgeText();
                return;
            }
            
            base.TakeDamage(damage, isCritical, damageType);
        }
        
        protected override float ApplyElementalInteractions(float damage, ElementType damageType)
        {
            // Apply special boss resistance
            float modifiedDamage = base.ApplyElementalInteractions(damage, damageType);
            
            // Apply phase-based resistance
            if (currentPhase > 1)
            {
                // Increase resistance in later phases
                modifiedDamage *= 1f - (0.1f * (currentPhase - 1));
            }
            
            return modifiedDamage;
        }
        #endregion
    }
    
    /// <summary>
    /// Định nghĩa kỹ năng của boss
    /// </summary>
    [System.Serializable]
    public class BossAbility
    {
        [Header("Basic Info")]
        [SerializeField] private string abilityName;
        [SerializeField] private BossAbilityType abilityType;
        [SerializeField] private ElementType elementType = ElementType.None;
        
        [Header("Usage Settings")]
        [SerializeField] private int minPhase = 1;
        [SerializeField] private int maxPhase = 99;
        [SerializeField] private float cooldown = 10f;
        [SerializeField] private float useWeight = 1f;
        [SerializeField] private float duration = 3f;
        
        [Header("Effect Settings")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float areaRadius = 3f;
        [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.None;
        [SerializeField] private float speedModifier = 0f;
        
        [Header("Summon Settings")]
        [SerializeField] private string summonEnemyId;
        [SerializeField] private int summonCount = 3;
        
        [Header("Movement Settings")]
        [SerializeField] private bool isTeleport = false;
        [SerializeField] private float movementDistance = 5f;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private string animationTrigger = "Attack";
        
        // Runtime state
        private float currentCooldown = 0f;
        
        // Properties
        public string AbilityName => abilityName;
        public BossAbilityType AbilityType => abilityType;
        public ElementType ElementType => elementType;
        public int MinPhase => minPhase;
        public int MaxPhase => maxPhase;
        public float UseWeight => useWeight;
        public float Duration => duration;
        public float DamageMultiplier => damageMultiplier;
        public float AreaRadius => areaRadius;
        public StatusEffectType StatusEffectType => statusEffectType;
        public string SummonEnemyId => summonEnemyId;
        public int SummonCount => summonCount;
        public bool IsTeleport => isTeleport;
        public float MovementDistance => movementDistance;
        public GameObject EffectPrefab => effectPrefab;
        public string AnimationTrigger => animationTrigger;
        public float SpeedModifier => speedModifier;
        
        /// <summary>
        /// Kiểm tra xem kỹ năng đã sẵn sàng sử dụng chưa
        /// </summary>
        public bool IsReady()
        {
            return currentCooldown <= 0f;
        }
        
        /// <summary>
        /// Bắt đầu cooldown cho kỹ năng
        /// </summary>
        public void StartCooldown()
        {
            currentCooldown = cooldown;
        }
        
        /// <summary>
        /// Cập nhật cooldown
        /// </summary>
        public void UpdateCooldown(float deltaTime)
        {
            if (currentCooldown > 0f)
            {
                currentCooldown -= deltaTime;
            }
        }
    }
    
    /// <summary>
    /// Component cho hiệu ứng kỹ năng của boss
    /// </summary>
    public class BossAbilityEffect : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private Animator effectAnimator;
        [SerializeField] private ParticleSystem particles;
        
        private BossController boss;
        private BossAbility ability;
        
        public void Initialize(BossController bossController, BossAbility bossAbility)
        {
            boss = bossController;
            ability = bossAbility;
            
            // Set lifetime
            lifeTime = ability.Duration;
            
            // Auto-destroy after lifetime
            Destroy(gameObject, lifeTime);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Handle collision with targets
            if (other.CompareTag("Fortress") || other.CompareTag("Hero"))
            {
                // This can be expanded based on ability type
                Debug.Log($"Boss ability effect hit {other.tag}");
            }
        }
    }
    
    /// <summary>
    /// Loại kỹ năng của boss
    /// </summary>
    public enum BossAbilityType
    {
        Attack,         // Tấn công đơn mục tiêu
        AOE,            // Tấn công diện rộng
        Summon,         // Triệu hồi kẻ địch phụ
        Buff,           // Tăng sức mạnh
        Movement,       // Di chuyển đặc biệt (teleport, dash)
        StatusEffect    // Gây hiệu ứng
    }
}