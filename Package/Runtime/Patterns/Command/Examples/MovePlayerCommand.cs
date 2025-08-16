using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Command.Examples
{
    /// <summary>
    /// Player hareket command'ı örneği
    /// </summary>
    public class MovePlayerCommand : AsyncCommand<bool>
    {
        private readonly Transform _playerTransform;
        private readonly Vector3 _targetPosition;
        private readonly float _moveSpeed;
        private readonly float _tolerance;
        
        private Vector3 _originalPosition;
        private bool _isMoving;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="playerTransform">Player transform</param>
        /// <param name="targetPosition">Hedef pozisyon</param>
        /// <param name="moveSpeed">Hareket hızı</param>
        /// <param name="tolerance">Pozisyon toleransı</param>
        public MovePlayerCommand(Transform playerTransform, Vector3 targetPosition, float moveSpeed = 5f, float tolerance = 0.1f)
            : base("Move Player", $"Move player to {targetPosition}", priority: 1, timeoutMs: 10000)
        {
            _playerTransform = playerTransform;
            _targetPosition = targetPosition;
            _moveSpeed = moveSpeed;
            _tolerance = tolerance;
            
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
                       _playerTransform != null && 
                       !_isMoving &&
                       Vector3.Distance(_playerTransform.position, _targetPosition) > _tolerance;
            }
        }

        /// <summary>
        /// Async command core implementasyonu
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hareket başarılı mı</returns>
        protected override async Task<bool> OnExecuteAsyncCoreWithResult(CancellationToken cancellationToken)
        {
            if (_playerTransform == null)
            {
                throw new System.InvalidOperationException("Player transform is null");
            }

            // Orijinal pozisyonu kaydet (undo için)
            _originalPosition = _playerTransform.position;
            _isMoving = true;

            try
            {
                // Hareket animasyonu
                while (Vector3.Distance(_playerTransform.position, _targetPosition) > _tolerance)
                {
                    // Cancellation kontrolü
                    cancellationToken.ThrowIfCancellationRequested();

                    // Pozisyonu güncelle
                    _playerTransform.position = Vector3.MoveTowards(
                        _playerTransform.position, 
                        _targetPosition, 
                        _moveSpeed * Time.deltaTime
                    );

                    // Bir frame bekle
                    await Task.Yield();
                }

                // Tam pozisyona ayarla
                _playerTransform.position = _targetPosition;
                return true;
            }
            finally
            {
                _isMoving = false;
            }
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_playerTransform != null)
            {
                _playerTransform.position = _originalPosition;
                Debug.Log($"[MovePlayerCommand] Player moved back to original position: {_originalPosition}");
            }
        }

        /// <summary>
        /// Command execution başarısız olduğunda
        /// </summary>
        /// <param name="exception">Oluşan exception</param>
        protected override void OnExecutionFailed(System.Exception exception)
        {
            _isMoving = false;
            Debug.LogError($"[MovePlayerCommand] Move failed: {exception.Message}");
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"MovePlayerCommand: {_playerTransform?.name} -> {_targetPosition} (Speed: {_moveSpeed})";
        }
    }

    /// <summary>
    /// Instant player hareket command'ı
    /// </summary>
    public class InstantMovePlayerCommand : BaseCommand<bool>
    {
        private readonly Transform _playerTransform;
        private readonly Vector3 _targetPosition;
        private Vector3 _originalPosition;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="playerTransform">Player transform</param>
        /// <param name="targetPosition">Hedef pozisyon</param>
        public InstantMovePlayerCommand(Transform playerTransform, Vector3 targetPosition)
            : base("Instant Move Player", $"Instantly move player to {targetPosition}", priority: 2)
        {
            _playerTransform = playerTransform;
            _targetPosition = targetPosition;
            
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
                       _playerTransform != null &&
                       _playerTransform.position != _targetPosition;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        /// <returns>Hareket başarılı mı</returns>
        protected override bool OnExecuteWithResult()
        {
            if (_playerTransform == null)
            {
                throw new System.InvalidOperationException("Player transform is null");
            }

            // Orijinal pozisyonu kaydet
            _originalPosition = _playerTransform.position;
            
            // Instant hareket
            _playerTransform.position = _targetPosition;
            
            Debug.Log($"[InstantMovePlayerCommand] Player moved from {_originalPosition} to {_targetPosition}");
            return true;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_playerTransform != null)
            {
                _playerTransform.position = _originalPosition;
                Debug.Log($"[InstantMovePlayerCommand] Player moved back to original position: {_originalPosition}");
            }
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"InstantMovePlayerCommand: {_playerTransform?.name} -> {_targetPosition}";
        }
    }
}