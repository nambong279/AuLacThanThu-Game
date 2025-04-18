using System;
using System.Collections;
using UnityEngine;

namespace AuLacThanThu.Gameplay.Combat
{
    /// <summary>
    /// Quản lý hiệu ứng trạng thái như thiêu đốt, đóng băng, độc, v.v.
    /// </summary>
    public class StatusEffect
    {
        #region Properties
        // Basic information
        public string Name { get; private set; }
        public StatusEffectType Type { get; private set; }
        public float Duration { get; private set; }
        public float RemainingDuration { get; private set; }
        public bool IsExpired => RemainingDuration <= 0;
        
        // Timing
        private float tickInterval = 0f;
        private float timeSinceLastTick = 0f;
        
        // Effect callbacks
        private Action onStart;
        private Action onEnd;
        private Action onTick;
        #endregion
        
        #region Constructors
        public StatusEffect(string name, StatusEffectType type, float duration)
        {
            Name = name;
            Type = type;
            Duration = duration;
            RemainingDuration = duration;
        }
        #endregion
        
        #region Effect Setup
        public void SetStartEffect(Action action)
        {
            onStart = action;
        }
        
        public void SetEndEffect(Action action)
        {
            onEnd = action;
        }
        
        public void SetTickEffect(Action action, float interval)
        {
            onTick = action;
            tickInterval = interval;
            timeSinceLastTick = 0f;
        }
        #endregion
        
        #region Lifecycle Methods
        public void Start()
        {
            // Invoke start effect if set
            onStart?.Invoke();
        }
        
        public void Update()
        {
            // Update remaining duration
            RemainingDuration -= Time.deltaTime;
            
            // Process tick effects
            if (onTick != null && tickInterval > 0)
            {
                timeSinceLastTick += Time.deltaTime;
                
                if (timeSinceLastTick >= tickInterval)
                {
                    onTick.Invoke();
                    timeSinceLastTick = 0f;
                }
            }
            
            // Check for expiration
            if (RemainingDuration <= 0 && !IsExpired)
            {
                End();
            }
        }
        
        public void End()
        {
            // Invoke end effect if set
            onEnd?.Invoke();
        }
        
        public void Extend(float additionalDuration)
        {
            RemainingDuration += additionalDuration;
        }
        
        public void Refresh()
        {
            RemainingDuration = Duration;
        }
        #endregion
    }
    
    public enum StatusEffectType
    {
        None,
        Burn,
        Freeze,
        Poison,
        Stun,
        Bleed,
        Buff,
        Debuff
    }
}