using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Core;

namespace AuLacThanThu.Gameplay.Combat
{
    /// <summary>
    /// Hệ thống tính toán sát thương giữa các entity trong game
    /// </summary>
    public class DamageSystem : MonoBehaviour
    {
        #region Singleton
        private static DamageSystem _instance;
        public static DamageSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DamageSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("DamageSystem");
                        _instance = obj.AddComponent<DamageSystem>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Damage Calculation Settings")]
        [Tooltip("Hệ số phòng thủ trong công thức sát thương")]
        [SerializeField] private float defenseCoefficient = 100f;
        
        [Tooltip("Sát thương tối thiểu (phần trăm sát thương gốc)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float minDamagePercent = 0.1f;
        
        [Tooltip("Sát thương chí mạng cơ bản")]
        [SerializeField] private float baseCritDamage = 1.5f;
        
        [Header("Elemental Settings")]
        [Tooltip("Phần trăm tăng sát thương khi khắc chế")]
        [Range(0f, 1f)]
        [SerializeField] private float elementalAdvantagePercent = 0.2f;
        
        [Tooltip("Phần trăm giảm sát thương khi bị khắc chế")]
        [Range(0f, 1f)]
        [SerializeField] private float elementalDisadvantagePercent = 0.2f;
        
        [Header("Armor Calculation")]
        [Tooltip("Hệ số trong công thức tính hộ giáp")]
        [SerializeField] private float armorCoefficient = 200f;
        
        [Header("Hit Effects")]
        [SerializeField] private GameObject normalHitEffect;
        [SerializeField] private GameObject criticalHitEffect;
        [SerializeField] private GameObject fireHitEffect;
        [SerializeField] private GameObject waterHitEffect;
        [SerializeField] private GameObject earthHitEffect;
        [SerializeField] private GameObject lightningHitEffect;
        
        [Header("Floating Text")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = Color.red;
        [SerializeField] private Color healColor = Color.green;
        
        // Event System
        private EventSystem eventSystem;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            // Get references
            eventSystem = EventSystem.Instance;
        }
        #endregion
        
        #region Damage Calculation
        /// <summary>
        /// Tính toán sát thương sau khi áp dụng phòng thủ
        /// </summary>
        /// <param name="baseDamage">Sát thương cơ bản</param>
        /// <param name="attackerLevel">Cấp độ người tấn công</param>
        /// <param name="defenderDefense">Phòng thủ của người phòng thủ</param>
        /// <param name="defenderLevel">Cấp độ người phòng thủ</param>
        /// <returns>Sát thương sau khi tính toán</returns>
        public float CalculateDamageAfterDefense(float baseDamage, int attackerLevel, float defenderDefense, int defenderLevel)
        {
            // Công thức: Damage = BaseDamage * (1 - Defense / (Defense + DefenseCoefficient))
            // với Defense đã được điều chỉnh theo cấp độ
            
            // Điều chỉnh phòng thủ theo cấp độ
            float levelDifference = attackerLevel - defenderLevel;
            float defenseFactor = 1f + levelDifference * 0.05f; // +/- 5% hiệu quả phòng thủ mỗi cấp
            
            float adjustedDefense = defenderDefense * Mathf.Max(0.5f, defenseFactor);
            
            // Tính toán tỷ lệ giảm sát thương
            float damageReduction = adjustedDefense / (adjustedDefense + defenseCoefficient);
            
            // Đảm bảo sát thương tối thiểu
            damageReduction = Mathf.Min(damageReduction, 1f - minDamagePercent);
            
            // Áp dụng giảm sát thương
            float finalDamage = baseDamage * (1f - damageReduction);
            
            return Mathf.Max(1f, finalDamage);
        }
        
        /// <summary>
        /// Tính toán sát thương chí mạng
        /// </summary>
        /// <param name="damage">Sát thương cơ bản</param>
        /// <param name="critDamageMultiplier">Hệ số sát thương chí mạng</param>
        /// <param name="critReduction">Giảm sát thương chí mạng của đối phương</param>
        /// <returns>Sát thương chí mạng sau khi tính toán</returns>
        public float CalculateCriticalDamage(float damage, float critDamageMultiplier, float critReduction = 0f)
        {
            // Áp dụng giảm sát thương chí mạng
            float adjustedCritMultiplier = Mathf.Max(1f, critDamageMultiplier - critReduction);
            
            // Tính sát thương chí mạng
            return damage * adjustedCritMultiplier;
        }
        
        /// <summary>
        /// Tính toán sát thương sau khi áp dụng hộ giáp
        /// </summary>
        /// <param name="damage">Sát thương sau khi tính phòng thủ</param>
        /// <param name="armor">Hộ giáp của người phòng thủ</param>
        /// <param name="armorPenetration">Xuyên giáp của người tấn công</param>
        /// <returns>Sát thương sau khi áp dụng hộ giáp</returns>
        public float CalculateDamageAfterArmor(float damage, float armor, float armorPenetration = 0f)
        {
            // Giảm hộ giáp dựa trên xuyên giáp
            float effectiveArmor = Mathf.Max(0f, armor - armorPenetration);
            
            // Công thức giảm sát thương: Armor / (|Armor| + ArmorCoefficient)
            float damageReduction = effectiveArmor / (Mathf.Abs(effectiveArmor) + armorCoefficient);
            
            // Áp dụng giảm sát thương
            float finalDamage = damage * (1f - damageReduction);
            
            return Mathf.Max(1f, finalDamage);
        }
        
        /// <summary>
        /// Tính toán sát thương sau khi áp dụng tương khắc hệ
        /// </summary>
        /// <param name="damage">Sát thương cơ bản</param>
        /// <param name="attackerElement">Hệ nguyên tố người tấn công</param>
        /// <param name="defenderElement">Hệ nguyên tố người phòng thủ</param>
        /// <param name="elementalDamageBonus">Phần thưởng sát thương hệ bổ sung</param>
        /// <param name="elementalResistance">Kháng hệ của người phòng thủ</param>
        /// <returns>Sát thương sau khi áp dụng tương khắc hệ</returns>
        public float CalculateElementalDamage(float damage, ElementType attackerElement, ElementType defenderElement, 
            float elementalDamageBonus = 0f, float elementalResistance = 0f)
        {
            // Không có tương tác hệ nếu một trong hai không có hệ
            if (attackerElement == ElementType.None || defenderElement == ElementType.None)
            {
                return damage;
            }
            
            float damageMultiplier = 1f;
            
            // Áp dụng tương khắc hệ
            if (IsElementalAdvantage(attackerElement, defenderElement))
            {
                // Lợi thế hệ
                damageMultiplier += elementalAdvantagePercent;
            }
            else if (IsElementalAdvantage(defenderElement, attackerElement))
            {
                // Bất lợi hệ
                damageMultiplier -= elementalDisadvantagePercent;
            }
            
            // Áp dụng phần thưởng sát thương hệ
            damageMultiplier += elementalDamageBonus;
            
            // Áp dụng kháng hệ
            damageMultiplier -= elementalResistance;
            
            // Đảm bảo hệ số tối thiểu
            damageMultiplier = Mathf.Max(0.1f, damageMultiplier);
            
            return damage * damageMultiplier;
        }
        
        /// <summary>
        /// Kiểm tra xem hệ attackerElement có lợi thế so với defenderElement không
        /// </summary>
        public bool IsElementalAdvantage(ElementType attackerElement, ElementType defenderElement)
        {
            // Water > Fire > Earth > Lightning > Water
            return (attackerElement == ElementType.Water && defenderElement == ElementType.Fire) ||
                   (attackerElement == ElementType.Fire && defenderElement == ElementType.Earth) ||
                   (attackerElement == ElementType.Earth && defenderElement == ElementType.Lightning) ||
                   (attackerElement == ElementType.Lightning && defenderElement == ElementType.Water);
        }
        
        /// <summary>
        /// Xác định xem đòn tấn công có chí mạng không dựa trên tỷ lệ chí mạng và bỏ qua chí mạng
        /// </summary>
        /// <param name="critChance">Tỷ lệ chí mạng</param>
        /// <param name="critReductionChance">Tỷ lệ giảm chí mạng</param>
        /// <returns>True nếu đòn tấn công chí mạng</returns>
        public bool IsCriticalHit(float critChance, float critReductionChance = 0f)
        {
            // Áp dụng giảm tỷ lệ chí mạng
            float adjustedCritChance = Mathf.Max(0f, critChance - critReductionChance);
            
            // Xác định chí mạng
            return Random.value < adjustedCritChance;
        }
        
        /// <summary>
        /// Xác định xem đòn tấn công có bị né không dựa trên tỷ lệ né và bỏ qua né
        /// </summary>
        /// <param name="dodgeChance">Tỷ lệ né</param>
        /// <param name="dodgeIgnore">Tỷ lệ bỏ qua né</param>
        /// <returns>True nếu đòn tấn công bị né</returns>
        public bool IsDodged(float dodgeChance, float dodgeIgnore = 0f)
        {
            // Áp dụng bỏ qua né
            float adjustedDodgeChance = Mathf.Max(0f, dodgeChance - dodgeIgnore);
            
            // Xác định né
            return Random.value < adjustedDodgeChance;
        }
        
        /// <summary>
        /// Tính toán tổng sát thương sau khi áp dụng tất cả các yếu tố
        /// </summary>
        public DamageResult CalculateDamage(DamageInfo damageInfo)
        {
            DamageResult result = new DamageResult();
            
            // Kiểm tra né
            if (IsDodged(damageInfo.targetDodgeChance, damageInfo.attackerDodgeIgnore))
            {
                result.isDodged = true;
                return result;
            }
            
            // Sát thương cơ bản
            float damage = damageInfo.baseDamage;
            
            // Kiểm tra chí mạng
            result.isCritical = IsCriticalHit(damageInfo.attackerCritChance, damageInfo.targetCritReductionChance);
            
            // Áp dụng chí mạng nếu có
            if (result.isCritical)
            {
                damage = CalculateCriticalDamage(damage, damageInfo.attackerCritDamage, damageInfo.targetCritDamageReduction);
            }
            
            // Áp dụng phòng thủ
            damage = CalculateDamageAfterDefense(damage, damageInfo.attackerLevel, damageInfo.targetDefense, damageInfo.targetLevel);
            
            // Áp dụng hộ giáp
            damage = CalculateDamageAfterArmor(damage, damageInfo.targetArmor, damageInfo.attackerArmorPenetration);
            
            // Áp dụng tương khắc hệ
            result.elementType = damageInfo.elementType;
            damage = CalculateElementalDamage(damage, damageInfo.elementType, damageInfo.targetElementType, 
                damageInfo.attackerElementalBonus, damageInfo.targetElementalResistance);
            
            // Áp dụng giảm/tăng sát thương toàn thể
            damage *= (1f + damageInfo.attackerDamageIncrease - damageInfo.targetDamageReduction);
            
            // Đảm bảo sát thương tối thiểu là 1
            result.damage = Mathf.Max(1f, damage);
            
            // Áp dụng khắc chế hệ
            result.isElementalAdvantage = IsElementalAdvantage(damageInfo.elementType, damageInfo.targetElementType);
            result.isElementalDisadvantage = IsElementalAdvantage(damageInfo.targetElementType, damageInfo.elementType);
            
            return result;
        }
        #endregion
        
        #region Visual Effects
        /// <summary>
        /// Hiển thị hiệu ứng sát thương
        /// </summary>
        public void ShowDamageEffect(Vector3 position, DamageResult damageResult)
        {
            // Không hiển thị nếu né
            if (damageResult.isDodged)
            {
                ShowDodgeEffect(position);
                return;
            }
            
            // Hiển thị hiệu ứng chí mạng
            if (damageResult.isCritical && criticalHitEffect != null)
            {
                Instantiate(criticalHitEffect, position, Quaternion.identity);
            }
            // Hoặc hiệu ứng thường
            else if (normalHitEffect != null)
            {
                Instantiate(normalHitEffect, position, Quaternion.identity);
            }
            
            // Hiển thị hiệu ứng hệ
            ShowElementalHitEffect(position, damageResult.elementType);
            
            // Hiển thị số sát thương
            ShowDamageNumber(position, damageResult);
            
            // Lưu log sát thương
            LogDamage(damageResult);
        }
        
        /// <summary>
        /// Hiển thị hiệu ứng né
        /// </summary>
        public void ShowDodgeEffect(Vector3 position)
        {
            if (floatingTextPrefab != null)
            {
                GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
                FloatingText floatingText = textObj.GetComponent<FloatingText>();
                
                if (floatingText != null)
                {
                    floatingText.SetText("MISS", Color.gray);
                }
            }
        }
        
        /// <summary>
        /// Hiển thị hiệu ứng hồi máu
        /// </summary>
        public void ShowHealEffect(Vector3 position, float amount)
        {
            if (floatingTextPrefab != null)
            {
                GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
                FloatingText floatingText = textObj.GetComponent<FloatingText>();
                
                if (floatingText != null)
                {
                    floatingText.SetText("+" + Mathf.RoundToInt(amount).ToString(), healColor);
                }
            }
        }
        
        /// <summary>
        /// Hiển thị hiệu ứng hệ
        /// </summary>
        private void ShowElementalHitEffect(Vector3 position, ElementType elementType)
        {
            GameObject effectPrefab = null;
            
            switch (elementType)
            {
                case ElementType.Fire:
                    effectPrefab = fireHitEffect;
                    break;
                    
                case ElementType.Water:
                    effectPrefab = waterHitEffect;
                    break;
                    
                case ElementType.Earth:
                    effectPrefab = earthHitEffect;
                    break;
                    
                case ElementType.Lightning:
                    effectPrefab = lightningHitEffect;
                    break;
            }
            
            if (effectPrefab != null)
            {
                Instantiate(effectPrefab, position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Hiển thị số sát thương
        /// </summary>
        private void ShowDamageNumber(Vector3 position, DamageResult damageResult)
        {
            if (floatingTextPrefab == null)
                return;
                
            GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
            FloatingText floatingText = textObj.GetComponent<FloatingText>();
            
            if (floatingText != null)
            {
                string damageText = Mathf.RoundToInt(damageResult.damage).ToString();
                Color textColor = damageResult.isCritical ? criticalDamageColor : normalDamageColor;
                
                // Thêm hiệu ứng cho sát thương khắc chế hệ
                if (damageResult.isElementalAdvantage)
                {
                    damageText = "!" + damageText + "!";
                }
                
                floatingText.SetText(damageText, textColor);
                
                // Đặt kích thước lớn hơn cho chí mạng
                if (damageResult.isCritical)
                {
                    floatingText.SetSize(1.5f);
                }
            }
        }
        
        /// <summary>
        /// Ghi log sát thương
        /// </summary>
        private void LogDamage(DamageResult damageResult)
        {
            if (damageResult.isDodged)
            {
                Debug.Log("Attack missed (dodged)");
                return;
            }
            
            string criticalText = damageResult.isCritical ? "CRITICAL " : "";
            string elementText = damageResult.elementType != ElementType.None ? 
                $"[{damageResult.elementType}] " : "";
            string advantageText = damageResult.isElementalAdvantage ? "(Advantage) " : 
                damageResult.isElementalDisadvantage ? "(Disadvantage) " : "";
            
            Debug.Log($"{elementText}{criticalText}Damage: {damageResult.damage} {advantageText}");
        }
        #endregion
        
        #region Event Handling
        /// <summary>
        /// Xử lý sự kiện sát thương
        /// </summary>
        public void HandleDamageEvent(GameObject attacker, GameObject target, DamageInfo damageInfo)
        {
            // Tính toán sát thương
            DamageResult result = CalculateDamage(damageInfo);
            
            // Hiển thị hiệu ứng
            ShowDamageEffect(target.transform.position, result);
            
            // Gửi sự kiện đến EventSystem
            if (eventSystem != null)
            {
                // TODO: Gửi sự kiện sát thương
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Chứa thông tin đầu vào để tính toán sát thương
    /// </summary>
    public class DamageInfo
    {
        // Thông tin người tấn công
        public float baseDamage = 10f;
        public int attackerLevel = 1;
        public float attackerCritChance = 0.05f;
        public float attackerCritDamage = 1.5f;
        public float attackerDodgeIgnore = 0f;
        public float attackerArmorPenetration = 0f;
        public float attackerDamageIncrease = 0f;
        public float attackerElementalBonus = 0f;
        public ElementType elementType = ElementType.None;
        
        // Thông tin người phòng thủ
        public int targetLevel = 1;
        public float targetDefense = 5f;
        public float targetDodgeChance = 0.05f;
        public float targetCritReductionChance = 0f;
        public float targetCritDamageReduction = 0f;
        public float targetArmor = 0f;
        public float targetDamageReduction = 0f;
        public float targetElementalResistance = 0f;
        public ElementType targetElementType = ElementType.None;
    }
    
    /// <summary>
    /// Chứa kết quả tính toán sát thương
    /// </summary>
    public class DamageResult
    {
        public float damage = 0f;
        public bool isCritical = false;
        public bool isDodged = false;
        public bool isElementalAdvantage = false;
        public bool isElementalDisadvantage = false;
        public ElementType elementType = ElementType.None;
    }
    
    /// <summary>
    /// Component để hiển thị text nổi lên từ đối tượng
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fadeSpeed = 1f;
        
        private void Start()
        {
            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
            
            // Random horizontal offset
            float randomOffset = Random.Range(-0.5f, 0.5f);
            transform.position += new Vector3(randomOffset, 0, 0);
        }
        
        private void Update()
        {
            // Move upward
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            
            // Fade out
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a -= fadeSpeed * Time.deltaTime;
                textMesh.color = color;
            }
        }
        
        public void SetText(string text, Color color)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }
        }
        
        public void SetSize(float size)
        {
            if (textMesh != null)
            {
                textMesh.fontSize = Mathf.RoundToInt(textMesh.fontSize * size);
            }
            
            transform.localScale *= size;
        }
    }
}