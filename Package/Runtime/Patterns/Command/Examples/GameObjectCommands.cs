using System.Collections.Generic;
using UnityEngine;

namespace MDI.Patterns.Command.Examples
{
    /// <summary>
    /// GameObject aktif/pasif command'ı
    /// </summary>
    public class SetGameObjectActiveCommand : BaseCommand<bool>
    {
        private readonly GameObject _gameObject;
        private readonly bool _active;
        private bool _previousState;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="active">Aktif durumu</param>
        public SetGameObjectActiveCommand(GameObject gameObject, bool active)
            : base("Set GameObject Active", $"Set {gameObject?.name} active: {active}", priority: 1)
        {
            _gameObject = gameObject;
            _active = active;
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
                       _gameObject != null &&
                       _gameObject.activeSelf != _active;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override bool OnExecuteWithResult()
        {
            if (_gameObject == null)
                return false;

            _previousState = _gameObject.activeSelf;
            _gameObject.SetActive(_active);
            
            Debug.Log($"[SetGameObjectActiveCommand] {_gameObject.name} set to {_active}");
            return true;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_gameObject != null)
            {
                _gameObject.SetActive(_previousState);
                Debug.Log($"[SetGameObjectActiveCommand] {_gameObject.name} reverted to {_previousState}");
            }
        }
    }

    /// <summary>
    /// GameObject instantiate command'ı
    /// </summary>
    public class InstantiateGameObjectCommand : BaseCommand<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Transform _parent;
        private GameObject _instantiatedObject;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefab">Instantiate edilecek prefab</param>
        /// <param name="position">Pozisyon</param>
        /// <param name="rotation">Rotasyon</param>
        /// <param name="parent">Parent transform</param>
        public InstantiateGameObjectCommand(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
            : base("Instantiate GameObject", $"Instantiate {prefab?.name}", priority: 2)
        {
            _prefab = prefab;
            _position = position;
            _rotation = rotation == default ? Quaternion.identity : rotation;
            _parent = parent;
            CanUndo = true;
        }

        /// <summary>
        /// Command çalıştırılabilir mi kontrolü
        /// </summary>
        public override bool CanExecute
        {
            get
            {
                return base.CanExecute && _prefab != null;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override GameObject OnExecuteWithResult()
        {
            if (_prefab == null)
                return null;

            _instantiatedObject = Object.Instantiate(_prefab, _position, _rotation, _parent);
            
            Debug.Log($"[InstantiateGameObjectCommand] Instantiated {_prefab.name} as {_instantiatedObject.name}");
            return _instantiatedObject;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_instantiatedObject != null)
            {
                var objectName = _instantiatedObject.name;
                Object.DestroyImmediate(_instantiatedObject);
                _instantiatedObject = null;
                Debug.Log($"[InstantiateGameObjectCommand] Destroyed {objectName}");
            }
        }
    }

    /// <summary>
    /// GameObject destroy command'ı
    /// </summary>
    public class DestroyGameObjectCommand : BaseCommand<bool>
    {
        private readonly GameObject _gameObject;
        private readonly bool _immediate;
        private GameObject _backupPrefab;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Transform _originalParent;
        private bool _originalActiveState;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameObject">Destroy edilecek GameObject</param>
        /// <param name="immediate">Hemen destroy edilsin mi</param>
        public DestroyGameObjectCommand(GameObject gameObject, bool immediate = false)
            : base("Destroy GameObject", $"Destroy {gameObject?.name}", priority: 3)
        {
            _gameObject = gameObject;
            _immediate = immediate;
            // Not: Gerçek undo için prefab referansı gerekli, bu basit bir örnek
            CanUndo = false; // Güvenlik için undo kapalı
        }

        /// <summary>
        /// Command çalıştırılabilir mi kontrolü
        /// </summary>
        public override bool CanExecute
        {
            get
            {
                return base.CanExecute && _gameObject != null;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override bool OnExecuteWithResult()
        {
            if (_gameObject == null)
                return false;

            // Backup bilgileri kaydet (undo için)
            SaveBackupInfo();

            var objectName = _gameObject.name;
            
            if (_immediate)
            {
                Object.DestroyImmediate(_gameObject);
            }
            else
            {
                Object.Destroy(_gameObject);
            }
            
            Debug.Log($"[DestroyGameObjectCommand] Destroyed {objectName} (Immediate: {_immediate})");
            return true;
        }

        /// <summary>
        /// Backup bilgilerini kaydeder
        /// </summary>
        private void SaveBackupInfo()
        {
            if (_gameObject != null)
            {
                _originalPosition = _gameObject.transform.position;
                _originalRotation = _gameObject.transform.rotation;
                _originalParent = _gameObject.transform.parent;
                _originalActiveState = _gameObject.activeSelf;
                
                // Not: Gerçek bir undo için GameObject'in tüm component'lerini ve child'larını
                // serialize etmek gerekir. Bu basit bir örnek.
            }
        }
    }

    /// <summary>
    /// Component ekleme command'ı
    /// </summary>
    public class AddComponentCommand<T> : BaseCommand<T> where T : Component
    {
        private readonly GameObject _gameObject;
        private T _addedComponent;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameObject">Component eklenecek GameObject</param>
        public AddComponentCommand(GameObject gameObject)
            : base("Add Component", $"Add {typeof(T).Name} to {gameObject?.name}", priority: 2)
        {
            _gameObject = gameObject;
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
                       _gameObject != null &&
                       _gameObject.GetComponent<T>() == null;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override T OnExecuteWithResult()
        {
            if (_gameObject == null)
                return null;

            _addedComponent = _gameObject.AddComponent<T>();
            
            Debug.Log($"[AddComponentCommand] Added {typeof(T).Name} to {_gameObject.name}");
            return _addedComponent;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            if (_addedComponent != null)
            {
                var componentName = _addedComponent.GetType().Name;
                var gameObjectName = _addedComponent.gameObject.name;
                
                Object.DestroyImmediate(_addedComponent);
                _addedComponent = null;
                
                Debug.Log($"[AddComponentCommand] Removed {componentName} from {gameObjectName}");
            }
        }
    }

    /// <summary>
    /// Multiple GameObject işlemleri için batch command
    /// </summary>
    public class BatchGameObjectCommand : BaseCommand<List<CommandResult>>
    {
        private readonly List<IBaseCommand> _commands;
        private readonly ICommandExecutor _executor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <param name="executor">Command executor (opsiyonel)</param>
        public BatchGameObjectCommand(List<IBaseCommand> commands, ICommandExecutor executor = null)
            : base("Batch GameObject Operations", $"Execute {commands?.Count} commands", priority: 1)
        {
            _commands = commands ?? new List<IBaseCommand>();
            _executor = executor ?? new CommandExecutor();
            CanUndo = true;
        }

        /// <summary>
        /// Command çalıştırılabilir mi kontrolü
        /// </summary>
        public override bool CanExecute
        {
            get
            {
                return base.CanExecute && _commands.Count > 0;
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected override List<CommandResult> OnExecuteWithResult()
        {
            var results = new List<CommandResult>();
            
            foreach (var command in _commands)
            {
                var result = _executor.Execute(command);
                results.Add(result);
                
                // Eğer bir command başarısız olursa durabilir (opsiyonel)
                // if (!result.IsSuccess) break;
            }
            
            Debug.Log($"[BatchGameObjectCommand] Executed {results.Count} commands");
            return results;
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected override void OnUndo()
        {
            // Command'ları ters sırada undo et
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                var command = _commands[i];
                if (command.CanUndo)
                {
                    _executor.Undo(command);
                }
            }
            
            Debug.Log($"[BatchGameObjectCommand] Undone {_commands.Count} commands");
        }
    }
}