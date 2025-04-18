using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;
using AuLacThanThu.Utils;

namespace AuLacThanThu.Gameplay.Tower
{
    /// <summary>
    /// Điều khiển nỏ thần - vũ khí chính của người chơi
    /// </summary>
    public class CrossbowController : MonoBehaviour
    {
        #region Properties
        [Header("References")]
        [SerializeField] private Transform crossbowPivot;
        [SerializeField] private Transform firingPoint;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private LineRenderer aimLine;
        
        [Header("Settings")]
        [SerializeField] private float baseAttackSpeed = 1f;   // Attacks per second
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float critChance = 0.05f;     // 5% crit chance
        [SerializeField] private float critDamage = 1.5f;      // 150% damage on crit
        [SerializeField] private float rotationSpeed = 10f;    // How fast crossbow rotates
        [SerializeField] private bool autoAttack = false;      // Auto-attack nearest enemy
        [SerializeField] private float detectionRange = 10f;   // For auto-attack
        
        // Internal state
        private float attackCooldown = 0f;
        private Vector3 targetPosition;
        private bool isAiming = false;
        private ElementType currentElementType = ElementType.None;
        
        // References
        private Camera mainCamera;
        private List<GameObject> upgrades = new List<GameObject>();
        #endregion
        
        #region Events
        public delegate void ArrowFiredHandler(GameObject arrow, float damage, bool isCrit, ElementType elementType);
        public event ArrowFiredHandler OnArrowFired;
        #endregion
        
        #region Skill Properties
        // Skill levels
        private int multiShotLevel = 0;     // Additional arrows shot
        private int spreadShotLevel = 0;    // Spread angle of arrows
        private int piercingShotLevel = 0;  // Pierce through enemies
        
        // Element skills
        private int fireArrowLevel = 0;
        private int poisonArrowLevel = 0;
        private int lightningArrowLevel = 0;
        private int iceArrowLevel = 0;
        #endregion
        
        #region Stat Modifiers
        // These can be modified by skills and upgrades
        private float attackSpeedModifier = 1.0f;
        private float damageModifier = 1.0f;
        private float critChanceModifier = 1.0f;
        private float critDamageModifier = 1.0f;
        #endregion
        
        #region Initialization
        private void Start()
        {
            mainCamera = Camera.main;
            
            // Hide aim line by default
            if (aimLine != null)
            {
                aimLine.enabled = false;
            }
            
            // Set initial target point above
            targetPosition = transform.position + Vector3.up * 5f;
        }
        #endregion
        
        #region Update & Input Handling
        private void Update()
        {
            // Handle aiming and firing
            HandleInput();
            
            // Handle auto-attack
            if (autoAttack)
            {
                AutoAim();
            }
            
            // Rotate towards target
            RotateCrossbowTowardsTarget();
            
            // Update attack cooldown
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
        }
        
        private void HandleInput()
        {
            // Only process input when game is in Playing state
            if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return;
                
            // Touch/Mouse input for aiming
            if (Input.GetMouseButtonDown(0))
            {
                BeginAiming();
            }
            else if (Input.GetMouseButton(0) && isAiming)
            {
                ContinueAiming();
            }
            else if (Input.GetMouseButtonUp(0) && isAiming)
            {
                FireArrow();
                EndAiming();
            }
        }
        
        private void BeginAiming()
        {
            isAiming = true;
            
            // Show aim line
            if (aimLine != null)
            {
                aimLine.enabled = true;
            }
        }
        
        private void ContinueAiming()
        {
            // Get target position from mouse/touch
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // Distance from camera
            
            targetPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            
            // Update aim line
            UpdateAimLine();
        }
        
        private void EndAiming()
        {
            isAiming = false;
            
            // Hide aim line
            if (aimLine != null)
            {
                aimLine.enabled = false;
            }
        }
        
        private void AutoAim()
        {
            // Skip if manually aiming or on cooldown
            if (isAiming || attackCooldown > 0)
                return;
                
            // Find nearest enemy
            GameObject nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy != null)
            {
                // Target the enemy
                targetPosition = nearestEnemy.transform.position;
                
                // Auto-fire if cooldown is ready
                if (attackCooldown <= 0)
                {
                    FireArrow();
                }
            }
        }
        
        private GameObject FindNearestEnemy()
        {
            // Find all enemies
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            
            GameObject nearest = null;
            float nearestDistance = detectionRange;
            
            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                
                if (distance < nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }
            
            return nearest;
        }
        #endregion
        
        #region Firing & Rotation
        private void RotateCrossbowTowardsTarget()
        {
            if (crossbowPivot == null)
                return;
                
            // Calculate direction to target
            Vector3 direction = targetPosition - crossbowPivot.position;
            
            // Calculate rotation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90); // -90 to adjust for sprite orientation
            
            // Smoothly rotate
            crossbowPivot.rotation = Quaternion.Slerp(
                crossbowPivot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        
        private void UpdateAimLine()
        {
            if (aimLine == null || firingPoint == null)
                return;
                
            aimLine.SetPosition(0, firingPoint.position);
            
            // Simple straight line for now
            Vector3 direction = (targetPosition - firingPoint.position).normalized;
            aimLine.SetPosition(1, firingPoint.position + direction * 10f);
        }
        
        private void FireArrow()
        {
            // Check if on cooldown
            if (attackCooldown > 0)
                return;
                
            // Calculate attack speed
            float attacksPerSecond = baseAttackSpeed * attackSpeedModifier;
            attackCooldown = 1f / attacksPerSecond;
            
            // Base arrow
            FireSingleArrow(0);
            
            // Multi-shot (additional arrows straight forward)
            for (int i = 1; i <= multiShotLevel; i++)
            {
                FireSingleArrow(0, i * 0.2f); // Delay additional arrows slightly
            }
            
            // Spread shot (arrows at angles)
            if (spreadShotLevel > 0)
            {
                float spreadAngle = 10f;
                
                // One arrow to the left
                FireSingleArrow(-spreadAngle);
                
                // One arrow to the right
                FireSingleArrow(spreadAngle);
                
                if (spreadShotLevel > 1)
                {
                    // Second level adds wider spread
                    FireSingleArrow(-spreadAngle * 2);
                    FireSingleArrow(spreadAngle * 2);
                }
            }
        }
        
        private void FireSingleArrow(float angleOffset, float delay = 0f)
        {
            if (delay > 0f)
            {
                StartCoroutine(FireArrowWithDelay(angleOffset, delay));
                return;
            }
            
            // Check if arrow prefab exists
            if (arrowPrefab == null || firingPoint == null)
            {
                Debug.LogError("Arrow prefab or firing point not assigned!");
                return;
            }
            
            // Calculate direction with offset
            Vector3 baseDirection = (targetPosition - firingPoint.position).normalized;
            Quaternion offsetRotation = Quaternion.Euler(0, 0, angleOffset);
            Vector3 direction = offsetRotation * baseDirection;
            
            // Create arrow
            GameObject arrow = Instantiate(arrowPrefab, firingPoint.position, Quaternion.identity);
            
            // Set arrow properties
            ArrowController arrowController = arrow.GetComponent<ArrowController>();
            
            if (arrowController != null)
            {
                // Calculate damage
                float damage = baseDamage * damageModifier;
                bool isCrit = Random.value < (critChance * critChanceModifier);
                
                if (isCrit)
                {
                    damage *= critDamage * critDamageModifier;
                }
                
                // Set arrow stats
                arrowController.Initialize(
                    damage,
                    direction,
                    isCrit, 
                    piercingShotLevel,
                    currentElementType
                );
                
                // Apply elemental effects
                ApplyElementalEffects(arrowController);
            }
            
            // Trigger event
            OnArrowFired?.Invoke(arrow, baseDamage * damageModifier, isCrit, currentElementType);
        }
        
        private IEnumerator FireArrowWithDelay(float angleOffset, float delay)
        {
            yield return new WaitForSeconds(delay);
            FireSingleArrow(angleOffset);
        }
        
        private void ApplyElementalEffects(ArrowController arrow)
        {
            // Apply elemental effects based on skill levels
            if (fireArrowLevel > 0)
            {
                arrow.SetElementType(ElementType.Fire);
                arrow.SetBurnDamage(baseDamage * 0.1f * fireArrowLevel, 3f);
            }
            else if (poisonArrowLevel > 0)
            {
                arrow.SetElementType(ElementType.Earth);
                arrow.SetPoisonDamage(0.02f * poisonArrowLevel, 3f);
            }
            else if (lightningArrowLevel > 0)
            {
                arrow.SetElementType(ElementType.Lightning);
                arrow.SetChainLightning(lightningArrowLevel, baseDamage * 0.5f, 3f);
            }
            else if (iceArrowLevel > 0)
            {
                arrow.SetElementType(ElementType.Water);
                arrow.SetFreezeEffect(0.2f * iceArrowLevel, 2f);
            }
        }
        #endregion
        
        #region Skill Upgrades
        public void UpgradeAttackSpeed(float amount)
        {
            attackSpeedModifier += amount;
            Debug.Log($"Attack speed increased to {baseAttackSpeed * attackSpeedModifier} attacks/sec");
        }
        
        public void UpgradeDamage(float amount)
        {
            damageModifier += amount;
            Debug.Log($"Damage increased to {baseDamage * damageModifier}");
        }
        
        public void UpgradeCritChance(float amount)
        {
            critChanceModifier += amount;
            Debug.Log($"Crit chance increased to {critChance * critChanceModifier * 100}%");
        }
        
        public void UpgradeCritDamage(float amount)
        {
            critDamageModifier += amount;
            Debug.Log($"Crit damage increased to {critDamage * critDamageModifier * 100}%");
        }
        
        public void UpgradeMultiShot()
        {
            multiShotLevel = Mathf.Min(multiShotLevel + 1, 2);
            Debug.Log($"Multi-shot upgraded to level {multiShotLevel}");
        }
        
        public void UpgradeSpreadShot()
        {
            spreadShotLevel = Mathf.Min(spreadShotLevel + 1, 2);
            Debug.Log($"Spread shot upgraded to level {spreadShotLevel}");
        }
        
        public void UpgradePiercingShot()
        {
            piercingShotLevel = Mathf.Min(piercingShotLevel + 1, 2);
            Debug.Log($"Piercing shot upgraded to level {piercingShotLevel}");
        }
        
        public void UpgradeFireArrows()
        {
            fireArrowLevel = Mathf.Min(fireArrowLevel + 1, 3);
            currentElementType = ElementType.Fire;
            Debug.Log($"Fire arrows upgraded to level {fireArrowLevel}");
        }
        
        public void UpgradePoisonArrows()
        {
            poisonArrowLevel = Mathf.Min(poisonArrowLevel + 1, 3);
            currentElementType = ElementType.Earth;
            Debug.Log($"Poison arrows upgraded to level {poisonArrowLevel}");
        }
        
        public void UpgradeLightningArrows()
        {
            lightningArrowLevel = Mathf.Min(lightningArrowLevel + 1, 3);
            currentElementType = ElementType.Lightning;
            Debug.Log($"Lightning arrows upgraded to level {lightningArrowLevel}");
        }
        
        public void UpgradeIceArrows()
        {
            iceArrowLevel = Mathf.Min(iceArrowLevel + 1, 3);
            currentElementType = ElementType.Water;
            Debug.Log($"Ice arrows upgraded to level {iceArrowLevel}");
        }
        
        public void ToggleAutoAttack(bool enabled)
        {
            autoAttack = enabled;
        }
        #endregion
    }
}