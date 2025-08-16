using System.Collections;
using UnityEngine;

namespace MDI.Patterns.Command.Examples
{
    /// <summary>
    /// Audio clip oynatma command'ı
    /// </summary>
    public class PlayAudioCommand : AsyncCommand<bool>
    {
        private readonly AudioSource _audioSource;
        private readonly AudioClip _audioClip;
        private readonly float _volume;
        private readonly bool _loop;
        private readonly float _pitch;
        private float _originalVolume;
        private bool _originalLoop;
        private float _originalPitch;
        private AudioClip _originalClip;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source</param>
        /// <param name="audioClip">Oynatılacak audio clip</param>
        /// <param name="volume">Ses seviyesi (0-1)</param>
        /// <param name="loop">Loop edilsin mi</param>
        /// <param name="pitch">Pitch değeri</param>
        public PlayAudioCommand(AudioSource audioSource, AudioClip audioClip, float volume = 1f, bool loop = false, float pitch = 1f)
            : base("Play Audio", $"Play {audioClip?.name}", priority: 2)
        {
            _audioSource = audioSource;
            _audioClip = audioClip;
            _volume = Mathf.Clamp01(volume);
            _loop = loop;
            _pitch = pitch;
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
                       _audioSource != null && 
                       _audioClip != null;
            }
        }

        /// <summary>
        /// Asenkron command execution implementasyonu
        /// </summary>
        protected override async System.Threading.Tasks.Task<bool> OnExecuteAsyncCoreWithResult(System.Threading.CancellationToken cancellationToken)
        {
            if (_audioSource == null || _audioClip == null)
                return false;

            // Orijinal değerleri kaydet
            SaveOriginalValues();

            // Audio ayarlarını uygula
            _audioSource.clip = _audioClip;
            _audioSource.volume = _volume;
            _audioSource.loop = _loop;
            _audioSource.pitch = _pitch;
            
            // Audio'yu oynat
            _audioSource.Play();
            
            Debug.Log($"[PlayAudioCommand] Playing {_audioClip.name} (Volume: {_volume}, Loop: {_loop}, Pitch: {_pitch})");

            // Eğer loop değilse, audio bitene kadar bekle
            if (!_loop)
            {
                float duration = _audioClip.length / Mathf.Abs(_pitch);
                float elapsed = 0f;
                
                while (elapsed < duration && !cancellationToken.IsCancellationRequested)
                {
                    await System.Threading.Tasks.Task.Delay(100, cancellationToken);
                    elapsed += 0.1f;
                    
                    // Audio source durmuşsa çık
                    if (!_audioSource.isPlaying)
                        break;
                }
            }

            return !cancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Orijinal değerleri kaydeder
        /// </summary>
        private void SaveOriginalValues()
        {
            if (_audioSource != null)
            {
                _originalClip = _audioSource.clip;
                _originalVolume = _audioSource.volume;
                _originalLoop = _audioSource.loop;
                _originalPitch = _audioSource.pitch;
            }
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = _originalClip;
                _audioSource.volume = _originalVolume;
                _audioSource.loop = _originalLoop;
                _audioSource.pitch = _originalPitch;
                
                Debug.Log($"[PlayAudioCommand] Stopped and reverted audio settings");
            }
        }
    }

    /// <summary>
    /// Audio durdurma command'ı
    /// </summary>
    public class StopAudioCommand : BaseCommand<bool>
    {
        private readonly AudioSource _audioSource;
        private readonly bool _fadeOut;
        private readonly float _fadeDuration;
        private bool _wasPlaying;
        private float _originalVolume;
        private float _playbackTime;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source</param>
        /// <param name="fadeOut">Fade out yapılsın mı</param>
        /// <param name="fadeDuration">Fade out süresi</param>
        public StopAudioCommand(AudioSource audioSource, bool fadeOut = false, float fadeDuration = 1f)
            : base("Stop Audio", $"Stop audio on {audioSource?.name}", priority: 1)
        {
            _audioSource = audioSource;
            _fadeOut = fadeOut;
            _fadeDuration = fadeDuration;
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
                       _audioSource != null && 
                       _audioSource.isPlaying;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override bool OnExecuteWithResult()
        {
            if (_audioSource == null || !_audioSource.isPlaying)
                return false;

            // Backup bilgileri kaydet
            _wasPlaying = _audioSource.isPlaying;
            _originalVolume = _audioSource.volume;
            _playbackTime = _audioSource.time;

            if (_fadeOut && _fadeDuration > 0)
            {
                // Fade out için coroutine başlat (Unity context'inde)
                if (Application.isPlaying)
                {
                    var behaviour = _audioSource.GetComponent<MonoBehaviour>();
                    if (behaviour != null)
                    {
                        behaviour.StartCoroutine(FadeOutCoroutine());
                    }
                    else
                    {
                        // Fallback: Direkt durdur
                        _audioSource.Stop();
                    }
                }
                else
                {
                    _audioSource.Stop();
                }
            }
            else
            {
                _audioSource.Stop();
            }
            
            Debug.Log($"[StopAudioCommand] Stopped audio (Fade out: {_fadeOut})");
            return true;
        }

        /// <summary>
        /// Fade out coroutine
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            float startVolume = _audioSource.volume;
            float elapsed = 0f;
            
            while (elapsed < _fadeDuration && _audioSource.isPlaying)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / _fadeDuration;
                _audioSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
                yield return null;
            }
            
            _audioSource.Stop();
            _audioSource.volume = startVolume; // Volume'u geri yükle
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_audioSource != null && _wasPlaying)
            {
                _audioSource.volume = _originalVolume;
                _audioSource.time = _playbackTime;
                _audioSource.Play();
                
                Debug.Log($"[StopAudioCommand] Resumed audio playback");
            }
        }
    }

    /// <summary>
    /// Audio volume ayarlama command'ı
    /// </summary>
    public class SetAudioVolumeCommand : BaseCommand<float>
    {
        private readonly AudioSource _audioSource;
        private readonly float _targetVolume;
        private readonly bool _smooth;
        private readonly float _duration;
        private float _originalVolume;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source</param>
        /// <param name="targetVolume">Hedef volume (0-1)</param>
        /// <param name="smooth">Smooth geçiş yapılsın mı</param>
        /// <param name="duration">Geçiş süresi</param>
        public SetAudioVolumeCommand(AudioSource audioSource, float targetVolume, bool smooth = false, float duration = 1f)
            : base("Set Audio Volume", $"Set volume to {targetVolume:F2}", priority: 1)
        {
            _audioSource = audioSource;
            _targetVolume = Mathf.Clamp01(targetVolume);
            _smooth = smooth;
            _duration = duration;
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
                       _audioSource != null &&
                       !Mathf.Approximately(_audioSource.volume, _targetVolume);
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override float OnExecuteWithResult()
        {
            if (_audioSource == null)
                return 0f;

            _originalVolume = _audioSource.volume;

            if (_smooth && _duration > 0 && Application.isPlaying)
            {
                // Smooth volume change için coroutine başlat
                var behaviour = _audioSource.GetComponent<MonoBehaviour>();
                if (behaviour != null)
                {
                    behaviour.StartCoroutine(SmoothVolumeCoroutine());
                }
                else
                {
                    // Fallback: Direkt ayarla
                    _audioSource.volume = _targetVolume;
                }
            }
            else
            {
                _audioSource.volume = _targetVolume;
            }
            
            Debug.Log($"[SetAudioVolumeCommand] Volume changed from {_originalVolume:F2} to {_targetVolume:F2} (Smooth: {_smooth})");
            return _targetVolume;
        }

        /// <summary>
        /// Smooth volume change coroutine
        /// </summary>
        private IEnumerator SmoothVolumeCoroutine()
        {
            float startVolume = _audioSource.volume;
            float elapsed = 0f;
            
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / _duration;
                _audioSource.volume = Mathf.Lerp(startVolume, _targetVolume, normalizedTime);
                yield return null;
            }
            
            _audioSource.volume = _targetVolume;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_audioSource != null)
            {
                _audioSource.volume = _originalVolume;
                Debug.Log($"[SetAudioVolumeCommand] Volume reverted to {_originalVolume:F2}");
            }
        }
    }

    /// <summary>
    /// Audio pitch ayarlama command'ı
    /// </summary>
    public class SetAudioPitchCommand : BaseCommand<float>
    {
        private readonly AudioSource _audioSource;
        private readonly float _targetPitch;
        private float _originalPitch;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source</param>
        /// <param name="targetPitch">Hedef pitch</param>
        public SetAudioPitchCommand(AudioSource audioSource, float targetPitch)
            : base("Set Audio Pitch", $"Set pitch to {targetPitch:F2}", priority: 1)
        {
            _audioSource = audioSource;
            _targetPitch = targetPitch;
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
                       _audioSource != null &&
                       !Mathf.Approximately(_audioSource.pitch, _targetPitch);
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override float OnExecuteWithResult()
        {
            if (_audioSource == null)
                return 0f;

            _originalPitch = _audioSource.pitch;
            _audioSource.pitch = _targetPitch;
            
            Debug.Log($"[SetAudioPitchCommand] Pitch changed from {_originalPitch:F2} to {_targetPitch:F2}");
            return _targetPitch;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_audioSource != null)
            {
                _audioSource.pitch = _originalPitch;
                Debug.Log($"[SetAudioPitchCommand] Pitch reverted to {_originalPitch:F2}");
            }
        }
    }

    /// <summary>
    /// Audio mute/unmute command'ı
    /// </summary>
    public class MuteAudioCommand : BaseCommand<bool>
    {
        private readonly AudioSource _audioSource;
        private readonly bool _mute;
        private bool _originalMuteState;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source</param>
        /// <param name="mute">Mute edilsin mi</param>
        public MuteAudioCommand(AudioSource audioSource, bool mute)
            : base("Mute Audio", $"{(mute ? "Mute" : "Unmute")} audio", priority: 1)
        {
            _audioSource = audioSource;
            _mute = mute;
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
                       _audioSource != null &&
                       _audioSource.mute != _mute;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override bool OnExecuteWithResult()
        {
            if (_audioSource == null)
                return false;

            _originalMuteState = _audioSource.mute;
            _audioSource.mute = _mute;
            
            Debug.Log($"[MuteAudioCommand] Audio {(_mute ? "muted" : "unmuted")}");
            return true;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_audioSource != null)
            {
                _audioSource.mute = _originalMuteState;
                Debug.Log($"[MuteAudioCommand] Mute state reverted to {_originalMuteState}");
            }
        }
    }
}