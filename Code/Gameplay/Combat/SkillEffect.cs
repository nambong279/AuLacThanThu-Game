using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Gameplay.Hero;

namespace AuLacThanThu.Gameplay.Combat
{
    /// <summary>
    /// Component gắn vào hiệu ứng kỹ năng của anh hùng
    /// </summary>
    public class SkillEffect : MonoBehaviour
    {
        #region Properties
        [Header("Effect Settings")]
        [SerializeField] private float effectDuration = 3f;
        [SerializeField] private float effectRadius = 2f;
        [SerializeField] private bool followOwner = false;
        [SerializeField] private bool rotateToTarget = false;
        [SerializeField] private GameObject hitEffectPrefab;
        
        [Header("Visual")]
        [SerializeField] private ParticleSystem mainParticle;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [Header("Audio")]
        [SerializeField] private AudioClip loopSound;
        [SerializeField] private AudioClip endSound;
        
        // References
        private HeroSkill skill;
        private HeroBase owner;
        private AudioSource audioSource;
        private SpriteRenderer spriteRenderer;
        
        // Timing
        private float elapsedTime = 0f;
        private bool isInitialized = false;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        private void Start()
        {
            // Set up audio
            if (audioSource != null && loopSound != null)
            {
                audioSource.clip = loopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Start effect
            if (mainParticle != null)
            {
                mainParticle.Play();
            }
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Update timing
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / effectDuration);
            
            // Update visuals
            UpdateVisuals(normalizedTime);
            
            // Follow owner if needed
            if (followOwner && owner != null)
            {
                transform.position = owner.transform.position;
            }
            
            // Check for effect end
            if (elapsedTime >= effectDuration)
            {
                EndEffect();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Handle collision with targets if needed
            HandleTargetCollision(collision.gameObject);
        }
        
        private void OnDestroy()
        {
            // Play end sound if specified
            if (endSound != null)
            {
                AudioSource.PlayClipAtPoint(endSound, transform.position);
            }
        }
        #endregion
        
        #region Initialization
        public void Initialize(HeroSkill skillReference, HeroBase ownerHero)
        {
            skill = skillReference;
            owner = ownerHero;
            
            // Set effect duration based on skill if provided
            if (skill != null)
            {
                // This would normally use skill.GetCurrentDuration() or similar
                // For now, just use the serialized duration
            }
            
            isInitialized = true;
        }
        #endregion
        
        #region Effect Control
        private void UpdateVisuals(float normalizedTime)
        {
            // Scale based on curve
            float scale = scaleCurve.Evaluate(normalizedTime);
            transform.localScale = new Vector3(scale, scale, scale);
            
            // Alpha based on curve if sprite renderer exists
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alphaCurve.Evaluate(normalizedTime);
                spriteRenderer.color = color;
            }
        }
        
        private void HandleTargetCollision(GameObject target)
        {
            if (skill == null || owner == null) return;
            
            // Spawn hit effect if applicable
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);
            }
            
            // Apply specific effects based on skill type
            // This would typically be handled by the skill itself
            // but we can add additional visual effects here
        }
        
        private void EndEffect()
        {
            // Stop particles
            if (mainParticle != null)
            {
                mainParticle.Stop();
            }
            
            // Stop audio
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            
            // Destroy after a small delay to allow effects to finish
            Destroy(gameObject, 0.5f);
        }
        #endregion
    }
}