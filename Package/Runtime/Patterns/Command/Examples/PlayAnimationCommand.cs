using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Command.Examples
{
    /// <summary>
    /// Animation oynatma command'ı örneği
    /// </summary>
    public class PlayAnimationCommand : AsyncCommand<bool>
    {
        private readonly Animator _animator;
        private readonly string _animationName;
        private readonly float _crossFadeTime;
        private readonly int _layer;
        
        private string _previousAnimationName;
        private AnimatorStateInfo _previousStateInfo;
        private bool _isPlaying;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animator">Animator component</param>
        /// <param name="animationName">Oynatılacak animasyon adı</param>
        /// <param name="crossFadeTime">CrossFade süresi</param>
        /// <param name="layer">Animator layer</param>
        public PlayAnimationCommand(Animator animator, string animationName, float crossFadeTime = 0.1f, int layer = 0)
            : base("Play Animation", $"Play animation: {animationName}", priority: 1, timeoutMs: 15000)
        {
            _animator = animator;
            _animationName = animationName;
            _crossFadeTime = crossFadeTime;
            _layer = layer;
            
            // Undo desteği aktif
            CanUndo = true;
        }

        /// <summary>
        /// Command çalıştırılabilir mi kontrolü
        /// </summary>
        public override bool CanExecute
        {
            get
            {
                return base.CanExecute && 
                       _animator != null && 
                       !string.IsNullOrEmpty(_animationName) &&
                       !_isPlaying &&
                       _animator.gameObject.activeInHierarchy;
            }
        }

        /// <summary>
        /// Async command core implementasyonu
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Animation başarılı mı</returns>
        protected override async Task<bool> OnExecuteAsyncCoreWithResult(CancellationToken cancellationToken)
        {
            if (_animator == null)
            {
                throw new System.InvalidOperationException("Animator is null");
            }

            if (string.IsNullOrEmpty(_animationName))
            {
                throw new System.InvalidOperationException("Animation name is null or empty");
            }

            // Mevcut animasyon bilgisini kaydet (undo için)
            _previousStateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
            _previousAnimationName = GetCurrentAnimationName();
            _isPlaying = true;

            try
            {
                // Animation'ı başlat
                if (_crossFadeTime > 0)
                {
                    _animator.CrossFade(_animationName, _crossFadeTime, _layer);
                }
                else
                {
                    _animator.Play(_animationName, _layer);
                }

                Debug.Log($"[PlayAnimationCommand] Started animation: {_animationName}");

                // Animation'ın başlamasını bekle
                await WaitForAnimationStart(cancellationToken);

                // Animation'ın bitmesini bekle (opsiyonel)
                if (!cancellationToken.IsCancellationRequested)
                {
                    await WaitForAnimationComplete(cancellationToken);
                }

                return true;
            }
            finally
            {
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Animation'ın başlamasını bekler
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task WaitForAnimationStart(CancellationToken cancellationToken)
        {
            var maxWaitTime = 2f; // 2 saniye max bekleme
            var waitTime = 0f;
            var frameTime = 0.016f; // ~60 FPS

            while (waitTime < maxWaitTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
                
                // Animation başladı mı kontrol et
                if (currentStateInfo.IsName(_animationName) || 
                    _animator.GetNextAnimatorStateInfo(_layer).IsName(_animationName))
                {
                    break;
                }

                await Task.Delay((int)(frameTime * 1000), cancellationToken);
                waitTime += frameTime;
            }
        }

        /// <summary>
        /// Animation'ın tamamlanmasını bekler
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task WaitForAnimationComplete(CancellationToken cancellationToken)
        {
            var frameTime = 0.016f; // ~60 FPS

            while (!cancellationToken.IsCancellationRequested)
            {
                var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
                
                // Animation tamamlandı mı kontrol et
                if (currentStateInfo.IsName(_animationName) && 
                    currentStateInfo.normalizedTime >= 1.0f && 
                    !_animator.IsInTransition(_layer))
                {
                    break;
                }

                await Task.Delay((int)(frameTime * 1000), cancellationToken);
            }

            Debug.Log($"[PlayAnimationCommand] Animation completed: {_animationName}");
        }

        /// <summary>
        /// Mevcut animation adını alır
        /// </summary>
        /// <returns>Animation adı</returns>
        private string GetCurrentAnimationName()
        {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
            var clipInfos = _animator.GetCurrentAnimatorClipInfo(_layer);
            
            if (clipInfos.Length > 0)
            {
                return clipInfos[0].clip.name;
            }
            
            return "Unknown";
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_animator != null && !string.IsNullOrEmpty(_previousAnimationName))
            {
                // Önceki animation'a geri dön
                _animator.Play(_previousAnimationName, _layer, _previousStateInfo.normalizedTime);
                Debug.Log($"[PlayAnimationCommand] Reverted to previous animation: {_previousAnimationName}");
            }
        }

        /// <summary>
        /// Command execution başarısız olduğunda
        /// </summary>
        /// <param name="exception">Oluşan exception</param>
        protected override void OnExecutionFailed(System.Exception exception)
        {
            _isPlaying = false;
            Debug.LogError($"[PlayAnimationCommand] Animation failed: {exception.Message}");
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"PlayAnimationCommand: {_animator?.name} -> {_animationName} (CrossFade: {_crossFadeTime}s)";
        }
    }

    /// <summary>
    /// Animation trigger command'ı
    /// </summary>
    public class TriggerAnimationCommand : BaseCommand<bool>
    {
        private readonly Animator _animator;
        private readonly string _triggerName;
        private readonly bool _resetAfterTrigger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animator">Animator component</param>
        /// <param name="triggerName">Trigger adı</param>
        /// <param name="resetAfterTrigger">Trigger'dan sonra reset edilsin mi</param>
        public TriggerAnimationCommand(Animator animator, string triggerName, bool resetAfterTrigger = false)
            : base("Trigger Animation", $"Trigger: {triggerName}", priority: 2)
        {
            _animator = animator;
            _triggerName = triggerName;
            _resetAfterTrigger = resetAfterTrigger;
            
            // Undo desteği (reset varsa)
            CanUndo = _resetAfterTrigger;
        }

        /// <summary>
        /// Command çalıştırılabilir mi kontrolü
        /// </summary>
        public override bool CanExecute
        {
            get
            {
                return base.CanExecute && 
                       _animator != null && 
                       !string.IsNullOrEmpty(_triggerName) &&
                       _animator.gameObject.activeInHierarchy;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        /// <returns>Trigger başarılı mı</returns>
        protected override bool OnExecuteWithResult()
        {
            if (_animator == null)
            {
                throw new System.InvalidOperationException("Animator is null");
            }

            if (string.IsNullOrEmpty(_triggerName))
            {
                throw new System.InvalidOperationException("Trigger name is null or empty");
            }

            // Trigger'ı aktif et
            _animator.SetTrigger(_triggerName);
            
            Debug.Log($"[TriggerAnimationCommand] Triggered: {_triggerName}");
            return true;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_animator != null && _resetAfterTrigger)
            {
                _animator.ResetTrigger(_triggerName);
                Debug.Log($"[TriggerAnimationCommand] Reset trigger: {_triggerName}");
            }
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"TriggerAnimationCommand: {_animator?.name} -> {_triggerName}";
        }
    }

    /// <summary>
    /// Animation parameter set command'ı
    /// </summary>
    public class SetAnimationParameterCommand : BaseCommand<bool>
    {
        private readonly Animator _animator;
        private readonly string _parameterName;
        private readonly object _value;
        private readonly System.Type _parameterType;
        private object _previousValue;

        /// <summary>
        /// Constructor for bool parameter
        /// </summary>
        public SetAnimationParameterCommand(Animator animator, string parameterName, bool value)
            : base("Set Animation Parameter", $"Set {parameterName} = {value}", priority: 3)
        {
            _animator = animator;
            _parameterName = parameterName;
            _value = value;
            _parameterType = typeof(bool);
            CanUndo = true;
        }

        /// <summary>
        /// Constructor for int parameter
        /// </summary>
        public SetAnimationParameterCommand(Animator animator, string parameterName, int value)
            : base("Set Animation Parameter", $"Set {parameterName} = {value}", priority: 3)
        {
            _animator = animator;
            _parameterName = parameterName;
            _value = value;
            _parameterType = typeof(int);
            CanUndo = true;
        }

        /// <summary>
        /// Constructor for float parameter
        /// </summary>
        public SetAnimationParameterCommand(Animator animator, string parameterName, float value)
            : base("Set Animation Parameter", $"Set {parameterName} = {value}", priority: 3)
        {
            _animator = animator;
            _parameterName = parameterName;
            _value = value;
            _parameterType = typeof(float);
            CanUndo = true;
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override bool OnExecuteWithResult()
        {
            if (_animator == null || string.IsNullOrEmpty(_parameterName))
                return false;

            // Önceki değeri kaydet
            SavePreviousValue();

            // Yeni değeri set et
            if (_parameterType == typeof(bool))
            {
                _animator.SetBool(_parameterName, (bool)_value);
            }
            else if (_parameterType == typeof(int))
            {
                _animator.SetInteger(_parameterName, (int)_value);
            }
            else if (_parameterType == typeof(float))
            {
                _animator.SetFloat(_parameterName, (float)_value);
            }

            Debug.Log($"[SetAnimationParameterCommand] Set {_parameterName} = {_value}");
            return true;
        }

        /// <summary>
        /// Önceki değeri kaydeder
        /// </summary>
        private void SavePreviousValue()
        {
            if (_parameterType == typeof(bool))
            {
                _previousValue = _animator.GetBool(_parameterName);
            }
            else if (_parameterType == typeof(int))
            {
                _previousValue = _animator.GetInteger(_parameterName);
            }
            else if (_parameterType == typeof(float))
            {
                _previousValue = _animator.GetFloat(_parameterName);
            }
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_animator == null || _previousValue == null)
                return;

            if (_parameterType == typeof(bool))
            {
                _animator.SetBool(_parameterName, (bool)_previousValue);
            }
            else if (_parameterType == typeof(int))
            {
                _animator.SetInteger(_parameterName, (int)_previousValue);
            }
            else if (_parameterType == typeof(float))
            {
                _animator.SetFloat(_parameterName, (float)_previousValue);
            }

            Debug.Log($"[SetAnimationParameterCommand] Reverted {_parameterName} = {_previousValue}");
        }
    }
}